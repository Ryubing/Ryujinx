using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Ryujinx.Graphics.Vulkan
{
    internal class BarrierBatch : IDisposable
    {
        private const int MaxBarriersPerCall = 16;

        private const AccessFlags BaseAccess = AccessFlags.ShaderReadBit | AccessFlags.ShaderWriteBit;
        private const AccessFlags BufferAccess = AccessFlags.IndexReadBit | AccessFlags.VertexAttributeReadBit | AccessFlags.UniformReadBit;
        private const AccessFlags CommandBufferAccess = AccessFlags.IndirectCommandReadBit;

        private readonly VulkanRenderer _gd;

        private readonly NativeArray<MemoryBarrier> _memoryBarrierBatch = new(MaxBarriersPerCall);
        private readonly NativeArray<BufferMemoryBarrier> _bufferBarrierBatch = new(MaxBarriersPerCall);
        private readonly NativeArray<ImageMemoryBarrier> _imageBarrierBatch = new(MaxBarriersPerCall);

        private readonly List<BarrierWithStageFlags<MemoryBarrier, int>> _memoryBarriers = new();
        private readonly List<BarrierWithStageFlags<BufferMemoryBarrier, int>> _bufferBarriers = new();
        private readonly List<BarrierWithStageFlags<ImageMemoryBarrier, TextureStorage>> _imageBarriers = new();
        private int _queuedBarrierCount;

        private enum IncoherentBarrierType
        {
            None,
            Texture,
            All,
            CommandBuffer
        }

        private PipelineStageFlags _incoherentBufferWriteStages;
        private PipelineStageFlags _incoherentTextureWriteStages;
        private PipelineStageFlags _extraStages;
        private IncoherentBarrierType _queuedIncoherentBarrier;

        public BarrierBatch(VulkanRenderer gd)
        {
            _gd = gd;
        }

        public static (AccessFlags Access, PipelineStageFlags Stages) GetSubpassAccessSuperset(VulkanRenderer gd)
        {
            AccessFlags access = BufferAccess;
            PipelineStageFlags stages = PipelineStageFlags.AllGraphicsBit;

            if (gd.TransformFeedbackApi != null)
            {
                access |= AccessFlags.TransformFeedbackWriteBitExt;
                stages |= PipelineStageFlags.TransformFeedbackBitExt;
            }

            if (!gd.IsTBDR)
            {
                // Desktop GPUs can transform image barriers into memory barriers.

                access |= AccessFlags.DepthStencilAttachmentWriteBit | AccessFlags.ColorAttachmentWriteBit;
                access |= AccessFlags.DepthStencilAttachmentReadBit | AccessFlags.ColorAttachmentReadBit;

                stages |= PipelineStageFlags.EarlyFragmentTestsBit | PipelineStageFlags.LateFragmentTestsBit;
                stages |= PipelineStageFlags.ColorAttachmentOutputBit;
            }

            return (access, stages);
        }

        private readonly record struct StageFlags : IEquatable<StageFlags>
        {
            public readonly PipelineStageFlags Source;
            public readonly PipelineStageFlags Dest;

            public StageFlags(PipelineStageFlags source, PipelineStageFlags dest)
            {
                Source = source;
                Dest = dest;
            }
        }

        private readonly struct BarrierWithStageFlags<T, T2> where T : unmanaged
        {
            public readonly StageFlags Flags;
            public readonly T Barrier;
            public readonly T2 Resource;

            public BarrierWithStageFlags(StageFlags flags, T barrier)
            {
                Flags = flags;
                Barrier = barrier;
                Resource = default;
            }

            public BarrierWithStageFlags(PipelineStageFlags srcStageFlags, PipelineStageFlags dstStageFlags, T barrier, T2 resource)
            {
                Flags = new StageFlags(srcStageFlags, dstStageFlags);
                Barrier = barrier;
                Resource = resource;
            }
        }

        private void QueueBarrier<T, T2>(List<BarrierWithStageFlags<T, T2>> list, T barrier, T2 resource, PipelineStageFlags srcStageFlags, PipelineStageFlags dstStageFlags) where T : unmanaged
        {
            list.Add(new BarrierWithStageFlags<T, T2>(srcStageFlags, dstStageFlags, barrier, resource));
            _queuedBarrierCount++;
        }

        public void QueueBarrier(MemoryBarrier barrier, PipelineStageFlags srcStageFlags, PipelineStageFlags dstStageFlags)
        {
            QueueBarrier(_memoryBarriers, barrier, default, srcStageFlags, dstStageFlags);
        }

        public void QueueBarrier(BufferMemoryBarrier barrier, PipelineStageFlags srcStageFlags, PipelineStageFlags dstStageFlags)
        {
            QueueBarrier(_bufferBarriers, barrier, default, srcStageFlags, dstStageFlags);
        }

        public void QueueBarrier(ImageMemoryBarrier barrier, TextureStorage resource, PipelineStageFlags srcStageFlags, PipelineStageFlags dstStageFlags)
        {
            QueueBarrier(_imageBarriers, barrier, resource, srcStageFlags, dstStageFlags);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void FlushMemoryBarrier(ShaderCollection program, bool inRenderPass)
        {
            if (_queuedIncoherentBarrier > IncoherentBarrierType.None)
            {
                // We should emit a memory barrier if there's a write access in the program (current program, or program since last barrier)
                bool hasTextureWrite = _incoherentTextureWriteStages != PipelineStageFlags.None;
                bool hasBufferWrite = _incoherentBufferWriteStages != PipelineStageFlags.None;
                bool hasBufferBarrier = _queuedIncoherentBarrier > IncoherentBarrierType.Texture;

                if (hasTextureWrite || (hasBufferBarrier && hasBufferWrite))
                {
                    AccessFlags access = BaseAccess;

                    PipelineStageFlags stages = inRenderPass ? PipelineStageFlags.AllGraphicsBit : PipelineStageFlags.AllCommandsBit;

                    if (hasBufferBarrier && hasBufferWrite)
                    {
                        access |= BufferAccess;

                        if (_gd.TransformFeedbackApi != null)
                        {
                            access |= AccessFlags.TransformFeedbackWriteBitExt;
                            stages |= PipelineStageFlags.TransformFeedbackBitExt;
                        }
                    }

                    if (_queuedIncoherentBarrier == IncoherentBarrierType.CommandBuffer)
                    {
                        access |= CommandBufferAccess;
                        stages |= PipelineStageFlags.DrawIndirectBit;
                    }

                    MemoryBarrier barrier = new MemoryBarrier()
                    {
                        SType = StructureType.MemoryBarrier,
                        SrcAccessMask = access,
                        DstAccessMask = access
                    };

                    QueueBarrier(barrier, stages, stages);

                    _incoherentTextureWriteStages = program?.IncoherentTextureWriteStages ?? PipelineStageFlags.None;

                    if (_queuedIncoherentBarrier > IncoherentBarrierType.Texture)
                    {
                        if (program != null)
                        {
                            _incoherentBufferWriteStages = program.IncoherentBufferWriteStages | _extraStages;
                        }
                        else
                        {
                            _incoherentBufferWriteStages = PipelineStageFlags.None;
                        }
                    }

                    _queuedIncoherentBarrier = IncoherentBarrierType.None;
                }
            }
        }

        public unsafe void Flush(CommandBufferScoped cbs, bool inRenderPass, RenderPassHolder rpHolder, Action endRenderPass)
        {
            Flush(cbs, null, inRenderPass, rpHolder, endRenderPass);
        }

        public unsafe void Flush(CommandBufferScoped cbs, ShaderCollection program, bool inRenderPass, RenderPassHolder rpHolder, Action endRenderPass)
        {
            if (program != null)
            {
                _incoherentBufferWriteStages |= program.IncoherentBufferWriteStages | _extraStages;
                _incoherentTextureWriteStages |= program.IncoherentTextureWriteStages;
            }

            FlushMemoryBarrier(program, inRenderPass);

            if (!inRenderPass && rpHolder != null)
            {
                // Render pass is about to begin. Queue any fences that normally interrupt the pass.
                rpHolder.InsertForcedFences(cbs);
            }

            while (_queuedBarrierCount > 0)
            {
                int memoryCount = 0;
                int bufferCount = 0;
                int imageCount = 0;

                bool hasBarrier = false;
                StageFlags flags = default;

                static void AddBarriers<T, T2>(
                    Span<T> target,
                    ref int queuedBarrierCount,
                    ref bool hasBarrier,
                    ref StageFlags flags,
                    ref int count,
                    List<BarrierWithStageFlags<T, T2>> list) where T : unmanaged
                {
                    int firstMatch = -1;
                    int end = list.Count;

                    for (int i = 0; i < list.Count; i++)
                    {
                        BarrierWithStageFlags<T, T2> barrier = list[i];

                        if (!hasBarrier)
                        {
                            flags = barrier.Flags;
                            hasBarrier = true;

                            target[count++] = barrier.Barrier;
                            queuedBarrierCount--;
                            firstMatch = i;

                            if (count >= target.Length)
                            {
                                end = i + 1;
                                break;
                            }
                        }
                        else
                        {
                            if (flags.Equals(barrier.Flags))
                            {
                                target[count++] = barrier.Barrier;
                                queuedBarrierCount--;

                                if (firstMatch == -1)
                                {
                                    firstMatch = i;
                                }

                                if (count >= target.Length)
                                {
                                    end = i + 1;
                                    break;
                                }
                            }
                            else
                            {
                                // Delete consumed barriers from the first match to the current non-match.
                                if (firstMatch != -1)
                                {
                                    int deleteCount = i - firstMatch;
                                    list.RemoveRange(firstMatch, deleteCount);
                                    i -= deleteCount;

                                    firstMatch = -1;
                                    end = list.Count;
                                }
                            }
                        }
                    }

                    if (firstMatch == 0 && end == list.Count)
                    {
                        list.Clear();
                    }
                    else if (firstMatch != -1)
                    {
                        int deleteCount = end - firstMatch;

                        list.RemoveRange(firstMatch, deleteCount);
                    }
                }

                if (inRenderPass && _imageBarriers.Count > 0)
                {
                    // Image barriers queued in the batch are meant to be globally scoped,
                    // but inside a render pass they're scoped to just the range of the render pass.

                    // On MoltenVK, we just break the rules and always use image barrier.
                    // On desktop GPUs, all barriers are globally scoped, so we just replace it with a generic memory barrier.
                    // Generally, we want to avoid this from happening in the future, so flag the texture to immediately
                    // emit a barrier whenever the current render pass is bound again.

                    bool anyIsNonAttachment = false;

                    foreach (BarrierWithStageFlags<ImageMemoryBarrier, TextureStorage> barrier in _imageBarriers)
                    {
                        // If the binding is an attachment, don't add it as a forced fence.
                        bool isAttachment = rpHolder.ContainsAttachment(barrier.Resource);

                        if (!isAttachment)
                        {
                            rpHolder.AddForcedFence(barrier.Resource, barrier.Flags.Dest);
                            anyIsNonAttachment = true;
                        }
                    }

                    if (_gd.IsTBDR)
                    {
                        if (!_gd.IsMoltenVk)
                        {
                            if (!anyIsNonAttachment)
                            {
                                // This case is a feedback loop. To prevent this from causing an absolute performance disaster,
                                // remove the barriers entirely.
                                // If this is not here, there will be a lot of single draw render passes.
                                // TODO: explicit handling for feedback loops, likely outside this class.

                                _queuedBarrierCount -= _imageBarriers.Count;
                                _imageBarriers.Clear();
                            }
                            else
                            {
                                // TBDR GPUs are sensitive to barriers, so we need to end the pass to ensure the data is available.
                                // Metal already has hazard tracking so MVK doesn't need this.
                                endRenderPass();
                                inRenderPass = false;
                            }
                        }
                    }
                    else
                    {
                        // Generic pipeline memory barriers will work for desktop GPUs.
                        // They do require a few more access flags on the subpass dependency, though.
                        foreach (var barrier in _imageBarriers)
                        {
                            _memoryBarriers.Add(new BarrierWithStageFlags<MemoryBarrier, int>(
                                barrier.Flags,
                                new MemoryBarrier()
                                {
                                    SType = StructureType.MemoryBarrier,
                                    SrcAccessMask = barrier.Barrier.SrcAccessMask,
                                    DstAccessMask = barrier.Barrier.DstAccessMask
                                }));
                        }

                        _imageBarriers.Clear();
                    }
                }

                if (inRenderPass && _memoryBarriers.Count > 0)
                {
                    PipelineStageFlags allFlags = PipelineStageFlags.None;

                    foreach (var barrier in _memoryBarriers)
                    {
                        allFlags |= barrier.Flags.Dest;
                    }

                    if (allFlags.HasFlag(PipelineStageFlags.DrawIndirectBit) || !_gd.SupportsRenderPassBarrier(allFlags))
                    {
                        endRenderPass();
                        inRenderPass = false;
                    }
                }

                AddBarriers(_memoryBarrierBatch.AsSpan(), ref _queuedBarrierCount, ref hasBarrier, ref flags, ref memoryCount, _memoryBarriers);
                AddBarriers(_bufferBarrierBatch.AsSpan(), ref _queuedBarrierCount, ref hasBarrier, ref flags, ref bufferCount, _bufferBarriers);
                AddBarriers(_imageBarrierBatch.AsSpan(), ref _queuedBarrierCount, ref hasBarrier, ref flags, ref imageCount, _imageBarriers);

                if (hasBarrier)
                {
                    PipelineStageFlags srcStageFlags = flags.Source;

                    if (inRenderPass)
                    {
                        // Inside a render pass, barrier stages can only be from rasterization.
                        srcStageFlags &= ~PipelineStageFlags.ComputeShaderBit;
                    }

                    _gd.Api.CmdPipelineBarrier(
                        cbs.CommandBuffer,
                        srcStageFlags,
                        flags.Dest,
                        0,
                        (uint)memoryCount,
                        _memoryBarrierBatch.Pointer,
                        (uint)bufferCount,
                        _bufferBarrierBatch.Pointer,
                        (uint)imageCount,
                        _imageBarrierBatch.Pointer);
                }
            }
        }

        private void QueueIncoherentBarrier(IncoherentBarrierType type)
        {
            if (type > _queuedIncoherentBarrier)
            {
                _queuedIncoherentBarrier = type;
            }
        }

        public void QueueTextureBarrier()
        {
            QueueIncoherentBarrier(IncoherentBarrierType.Texture);
        }

        public void QueueMemoryBarrier()
        {
            QueueIncoherentBarrier(IncoherentBarrierType.All);
        }

        public void QueueCommandBufferBarrier()
        {
            QueueIncoherentBarrier(IncoherentBarrierType.CommandBuffer);
        }

        public void EnableTfbBarriers(bool enable)
        {
            if (enable)
            {
                _extraStages |= PipelineStageFlags.TransformFeedbackBitExt;
            }
            else
            {
                _extraStages &= ~PipelineStageFlags.TransformFeedbackBitExt;
            }
        }

        public void Dispose()
        {
            _memoryBarrierBatch.Dispose();
            _bufferBarrierBatch.Dispose();
            _imageBarrierBatch.Dispose();
        }
    }
}

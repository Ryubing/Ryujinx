using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Graphics.Vulkan
{
    internal class RenderPassHolder
    {
        private readonly struct FramebufferCacheKey : IRefEquatable<FramebufferCacheKey>
        {
            private readonly uint _width;
            private readonly uint _height;
            private readonly uint _layers;

            public FramebufferCacheKey(uint width, uint height, uint layers)
            {
                _width = width;
                _height = height;
                _layers = layers;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(_width, _height, _layers);
            }

            public bool Equals(ref FramebufferCacheKey other)
            {
                return other._width == _width && other._height == _height && other._layers == _layers;
            }
        }

        private readonly record struct ForcedFence(TextureStorage Texture, PipelineStageFlags StageFlags);

        private readonly TextureView[] _textures;
        private readonly Auto<DisposableRenderPass> _renderPass;
        private readonly HashTableSlim<FramebufferCacheKey, Auto<DisposableFramebuffer>> _framebuffers;
        private readonly RenderPassCacheKey _key;
        private readonly List<ForcedFence> _forcedFences;

        public unsafe RenderPassHolder(VulkanRenderer gd, Device device, RenderPassCacheKey key, FramebufferParams fb)
        {
            // Create render pass using framebuffer params.

            const int MaxAttachments = Constants.MaxRenderTargets + 1;

            AttachmentDescription[] attachmentDescs = null;

            SubpassDescription subpass = new()
            {
                PipelineBindPoint = PipelineBindPoint.Graphics,
            };

            AttachmentReference* attachmentReferences = stackalloc AttachmentReference[MaxAttachments];

            bool hasFramebuffer = fb != null;

            if (hasFramebuffer && fb.AttachmentsCount != 0)
            {
                attachmentDescs = new AttachmentDescription[fb.AttachmentsCount];

                for (int i = 0; i < fb.AttachmentsCount; i++)
                {
                    attachmentDescs[i] = new AttachmentDescription(
                        0,
                        fb.AttachmentFormats[i],
                        TextureStorage.ConvertToSampleCountFlags(gd.Capabilities.SupportedSampleCounts, fb.AttachmentSamples[i]),
                        AttachmentLoadOp.Load,
                        AttachmentStoreOp.Store,
                        AttachmentLoadOp.Load,
                        AttachmentStoreOp.Store,
                        ImageLayout.General,
                        ImageLayout.General);
                }

                int colorAttachmentsCount = fb.ColorAttachmentsCount;

                if (colorAttachmentsCount > MaxAttachments - 1)
                {
                    colorAttachmentsCount = MaxAttachments - 1;
                }

                if (colorAttachmentsCount != 0)
                {
                    int maxAttachmentIndex = fb.MaxColorAttachmentIndex;
                    subpass.ColorAttachmentCount = (uint)maxAttachmentIndex + 1;
                    subpass.PColorAttachments = &attachmentReferences[0];

                    // Fill with VK_ATTACHMENT_UNUSED to cover any gaps.
                    for (int i = 0; i <= maxAttachmentIndex; i++)
                    {
                        subpass.PColorAttachments[i] = new AttachmentReference(Vk.AttachmentUnused, ImageLayout.Undefined);
                    }

                    for (int i = 0; i < colorAttachmentsCount; i++)
                    {
                        int bindIndex = fb.AttachmentIndices[i];

                        subpass.PColorAttachments[bindIndex] = new AttachmentReference((uint)i, ImageLayout.General);
                    }
                }

                if (fb.HasDepthStencil)
                {
                    uint dsIndex = (uint)fb.AttachmentsCount - 1;

                    subpass.PDepthStencilAttachment = &attachmentReferences[MaxAttachments - 1];
                    *subpass.PDepthStencilAttachment = new AttachmentReference(dsIndex, ImageLayout.General);
                }
            }

            SubpassDependency subpassDependency = PipelineConverter.CreateSubpassDependency(gd);

            fixed (AttachmentDescription* pAttachmentDescs = attachmentDescs)
            {
                RenderPassCreateInfo renderPassCreateInfo = new()
                {
                    SType = StructureType.RenderPassCreateInfo,
                    PAttachments = pAttachmentDescs,
                    AttachmentCount = attachmentDescs != null ? (uint)attachmentDescs.Length : 0,
                    PSubpasses = &subpass,
                    SubpassCount = 1,
                    PDependencies = &subpassDependency,
                    DependencyCount = 1,
                };

                gd.Api.CreateRenderPass(device, in renderPassCreateInfo, null, out RenderPass renderPass).ThrowOnError();

                _renderPass = new Auto<DisposableRenderPass>(new DisposableRenderPass(gd.Api, device, renderPass));
            }

            _framebuffers = new HashTableSlim<FramebufferCacheKey, Auto<DisposableFramebuffer>>();

            // Register this render pass with all render target views.

            TextureView[] textures = fb.GetAttachmentViews();

            foreach (TextureView texture in textures)
            {
                texture.AddRenderPass(key, this);
            }

            _textures = textures;
            _key = key;

            _forcedFences = [];
        }

        public Auto<DisposableFramebuffer> GetFramebuffer(VulkanRenderer gd, CommandBufferScoped cbs, FramebufferParams fb)
        {
            FramebufferCacheKey key = new(fb.Width, fb.Height, fb.Layers);

            if (!_framebuffers.TryGetValue(ref key, out Auto<DisposableFramebuffer> result))
            {
                result = fb.Create(gd.Api, cbs, _renderPass);

                _framebuffers.Add(ref key, result);
            }

            return result;
        }

        public Auto<DisposableRenderPass> GetRenderPass()
        {
            return _renderPass;
        }

        public void AddForcedFence(TextureStorage storage, PipelineStageFlags stageFlags)
        {
            if (!_forcedFences.Any(fence => fence.Texture == storage))
            {
                _forcedFences.Add(new ForcedFence(storage, stageFlags));
            }
        }

        public void InsertForcedFences(CommandBufferScoped cbs)
        {
            if (_forcedFences.Count > 0)
            {
                _forcedFences.RemoveAll((entry) =>
                {
                    if (entry.Texture.Disposed)
                    {
                        return true;
                    }

                    entry.Texture.QueueWriteToReadBarrier(cbs, AccessFlags.ShaderReadBit, entry.StageFlags);

                    return false;
                });
            }
        }

        public bool ContainsAttachment(TextureStorage storage)
        {
            return _textures.Any(view => view.Storage == storage);
        }

        public void Dispose()
        {
            // Dispose all framebuffers.

            foreach (Auto<DisposableFramebuffer> fb in _framebuffers.Values)
            {
                fb.Dispose();
            }

            // Notify all texture views that this render pass has been disposed.

            foreach (TextureView texture in _textures)
            {
                texture.RemoveRenderPass(_key);
            }

            // Dispose render pass.

            _renderPass.Dispose();
        }
    }
}

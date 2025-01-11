using Ryujinx.Common.Memory;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using System;
using System.Numerics;

namespace Ryujinx.Graphics.Vulkan
{
    struct PipelineDynamicState
    {
        private float _depthBiasSlopeFactor;
        private float _depthBiasConstantFactor;
        private float _depthBiasClamp;
        private bool _depthBiasEnable;

        public int ScissorsCount;
        private Array16<Rect2D> _scissors;

        private uint _backCompareMask;
        private uint _backWriteMask;
        private uint _backReference;
        private uint _frontCompareMask;
        private uint _frontWriteMask;
        private uint _frontReference;

        private StencilOp _backFailOp;
        private StencilOp _backPassOp;
        private StencilOp _backDepthFailOp;
        private CompareOp _backCompareOp;
        private StencilOp _frontFailOp;
        private StencilOp _frontPassOp;
        private StencilOp _frontDepthFailOp;
        private CompareOp _frontCompareOp;

        private float _lineWidth;

        public bool StencilTestEnable;

        public bool DepthTestEnable;
        public bool DepthWriteEnable;
        private CompareOp _depthCompareOp;

        private Array4<float> _blendConstants;

        private FeedbackLoopAspects _feedbackLoopAspects;

        public uint ViewportsCount;
        public Array16<Viewport> Viewports;

        public CullModeFlags CullMode;
        private FrontFace _frontFace;

        private bool _discard;

        private LogicOp _logicOp;

        private uint _patchControlPoints;

        public PrimitiveTopology Topology;

        private bool _primitiveRestartEnable;

        [Flags]
        private enum DirtyFlags
        {
            None = 0,
            Blend = 1 << 0,
            DepthBias = 1 << 1,
            Scissor = 1 << 2,
            Stencil = 1 << 3,
            Viewport = 1 << 4,
            FeedbackLoop = 1 << 5,
            CullMode = 1 << 6,
            FrontFace = 1 << 7,
            DepthTestBool = 1 << 8,
            DepthTestCompareOp = 1 << 9,
            StencilTestEnableAndStencilOp = 1 << 10,
            LineWidth = 1 << 11,
            RasterDiscard = 1 << 12,
            LogicOp = 1 << 13,
            PatchControlPoints = 1 << 14,
            PrimitiveRestart = 1 << 15,
            PrimitiveTopology = 1 << 16,
            DepthBiasEnable = 1 << 17,
            Standard = Blend | DepthBias | Scissor | Stencil | Viewport,
            Extended = CullMode | FrontFace | DepthTestBool | DepthTestCompareOp | StencilTestEnableAndStencilOp | PrimitiveTopology,
            Extended2 = RasterDiscard | PrimitiveRestart | DepthBiasEnable,
        }

        private DirtyFlags _dirty;

        public void SetBlendConstants(float r, float g, float b, float a)
        {
            _blendConstants[0] = r;
            _blendConstants[1] = g;
            _blendConstants[2] = b;
            _blendConstants[3] = a;
            _dirty |= DirtyFlags.Blend;
        }

        public void SetDepthBias(float slopeFactor, float constantFactor, float clamp)
        {
            _depthBiasSlopeFactor = slopeFactor;
            _depthBiasConstantFactor = constantFactor;
            _depthBiasClamp = clamp;

            _dirty |= DirtyFlags.DepthBias;
        }

        public void SetDepthBiasEnable(bool enable)
        {
            _depthBiasEnable = enable;
            _dirty |= DirtyFlags.DepthBiasEnable;
        }

        public void SetScissor(int index, Rect2D scissor)
        {
            _scissors[index] = scissor;
            _dirty |= DirtyFlags.Scissor;
        }

        public void SetDepthTestBool(bool testEnable, bool writeEnable)
        {
            DepthTestEnable = testEnable;
            DepthWriteEnable = writeEnable;
            _dirty |= DirtyFlags.DepthTestBool;
        }

        public void SetDepthTestCompareOp(CompareOp depthTestOp)
        {
            _depthCompareOp = depthTestOp;
            _dirty |= DirtyFlags.DepthTestCompareOp;
        }

        public void SetStencilTestandOp(
            StencilOp backFailOp,
            StencilOp backPassOp,
            StencilOp backDepthFailOp,
            CompareOp backCompareOp,
            StencilOp frontFailOp,
            StencilOp frontPassOp,
            StencilOp frontDepthFailOp,
            CompareOp frontCompareOp,
            bool stencilTestEnable)
        {
            _backFailOp = backFailOp;
            _backPassOp = backPassOp;
            _backDepthFailOp = backDepthFailOp;
            _backCompareOp = backCompareOp;
            _frontFailOp = frontFailOp;
            _frontPassOp = frontPassOp;
            _frontDepthFailOp = frontDepthFailOp;
            _frontCompareOp = frontCompareOp;

            StencilTestEnable = stencilTestEnable;

            _dirty |= DirtyFlags.StencilTestEnableAndStencilOp;
        }

        public void SetStencilTest(bool stencilTestEnable)
        {
            StencilTestEnable = stencilTestEnable;

            _dirty |= DirtyFlags.StencilTestEnableAndStencilOp;
        }

        public void SetStencilMask(uint backCompareMask,
            uint backWriteMask,
            uint backReference,
            uint frontCompareMask,
            uint frontWriteMask,
            uint frontReference)
        {
            _backCompareMask = backCompareMask;
            _backWriteMask = backWriteMask;
            _backReference = backReference;
            _frontCompareMask = frontCompareMask;
            _frontWriteMask = frontWriteMask;
            _frontReference = frontReference;
            _dirty |= DirtyFlags.Stencil;
        }

        public void SetViewport(int index, Viewport viewport)
        {
            Viewports[index] = viewport;
            _dirty |= DirtyFlags.Viewport;
        }

        public void SetViewports(ref Array16<Viewport> viewports, uint viewportsCount)
        {
            if (!Viewports.Equals(viewports) || ViewportsCount != viewportsCount)
            {
                Viewports = viewports;
                ViewportsCount = viewportsCount;
                if (ViewportsCount != 0)
                {
                    _dirty |= DirtyFlags.Viewport;
                }
            }
        }

        public void SetCullMode(CullModeFlags cullMode)
        {
            CullMode = cullMode;
            _dirty |= DirtyFlags.CullMode;
        }

        public void SetFrontFace(FrontFace frontFace)
        {
            _frontFace = frontFace;
            _dirty |= DirtyFlags.FrontFace;
        }

        public void SetLineWidth(float width)
        {
            _lineWidth = width;
            _dirty |= DirtyFlags.LineWidth;
        }

        public void SetFeedbackLoop(FeedbackLoopAspects aspects)
        {
            _feedbackLoopAspects = aspects;

            _dirty |= DirtyFlags.FeedbackLoop;
        }

        public void SetRasterizerDiscard(bool discard)
        {
            _discard = discard;
            _dirty |= DirtyFlags.RasterDiscard;
        }

        public void SetPrimitiveRestartEnable(bool primitiveRestart)
        {
            _primitiveRestartEnable = primitiveRestart;
            _dirty |= DirtyFlags.PrimitiveRestart;
        }

        public void SetPrimitiveTopology(PrimitiveTopology primitiveTopology)
        {
            Topology = primitiveTopology;
            _dirty |= DirtyFlags.PrimitiveTopology;
        }

        public void SetLogicOp(LogicOp op)
        {
            _logicOp = op;
            _dirty |= DirtyFlags.LogicOp;
        }

        public void SetPatchControlPoints(uint points)
        {
            _patchControlPoints = points;
            _dirty |= DirtyFlags.PatchControlPoints;
        }

        public void ForceAllDirty(VulkanRenderer gd)
        {
            _dirty = DirtyFlags.Standard;

            if (gd.Capabilities.SupportsExtendedDynamicState)
            {
                _dirty |= DirtyFlags.Extended;
            }

            if (gd.Capabilities.SupportsExtendedDynamicState2.ExtendedDynamicState2)
            {
                _dirty |= DirtyFlags.Extended2;

                if (gd.Capabilities.SupportsExtendedDynamicState2.ExtendedDynamicState2LogicOp)
                {
                    _dirty |= DirtyFlags.LogicOp;
                }

                if (gd.Capabilities.SupportsExtendedDynamicState2.ExtendedDynamicState2PatchControlPoints)
                {
                    _dirty |= DirtyFlags.PatchControlPoints;
                }
            }

            if (!gd.IsMoltenVk)
            {
                _dirty |= DirtyFlags.LineWidth;
            }

            if (gd.Capabilities.SupportsDynamicAttachmentFeedbackLoop)
            {
                _dirty |= DirtyFlags.FeedbackLoop;
            }
        }

        public void ReplayIfDirty(VulkanRenderer gd, CommandBuffer commandBuffer)
        {
            if (_dirty == DirtyFlags.None)
            {
                return;
            }

            var api = gd.Api;
            var extendedStateApi = gd.ExtendedDynamicStateApi;
            var extendedState2Api = gd.ExtendedDynamicState2Api;
            var dynamicFeedbackLoopApi = gd.DynamicFeedbackLoopApi;

            DirtyFlags dirtyFlags = _dirty;

            while (dirtyFlags != DirtyFlags.None)
            {
                int bitIndex = BitOperations.TrailingZeroCount((uint)dirtyFlags);
                DirtyFlags currentFlag = (DirtyFlags)(1 << bitIndex);

                switch (currentFlag)
                {
                    case DirtyFlags.Blend:
                        RecordBlend(api, commandBuffer);
                        break;
                    case DirtyFlags.DepthBias:
                        RecordDepthBias(api, commandBuffer);
                        break;
                    case DirtyFlags.Scissor:
                        RecordScissor(gd, commandBuffer);
                        break;
                    case DirtyFlags.Stencil:
                        RecordStencil(api, commandBuffer);
                        break;
                    case DirtyFlags.Viewport:
                        RecordViewport(gd, commandBuffer);
                        break;
                    case DirtyFlags.FeedbackLoop:
                        RecordFeedbackLoop(dynamicFeedbackLoopApi, commandBuffer);
                        break;
                    case DirtyFlags.CullMode:
                        RecordCullMode(extendedStateApi, commandBuffer);
                        break;
                    case DirtyFlags.FrontFace:
                        RecordFrontFace(extendedStateApi, commandBuffer);
                        break;
                    case DirtyFlags.DepthTestBool:
                        RecordDepthTestBool(extendedStateApi, commandBuffer);
                        break;
                    case DirtyFlags.DepthTestCompareOp:
                        RecordDepthTestCompareOp(extendedStateApi, commandBuffer);
                        break;
                    case DirtyFlags.StencilTestEnableAndStencilOp:
                        RecordStencilTestAndOp(extendedStateApi, commandBuffer);
                        break;
                    case DirtyFlags.LineWidth:
                        RecordLineWidth(api, commandBuffer);
                        break;
                    case DirtyFlags.RasterDiscard:
                        RecordRasterizationDiscard(extendedState2Api, commandBuffer);
                        break;
                    case DirtyFlags.LogicOp:
                        RecordLogicOp(extendedState2Api, commandBuffer);
                        break;
                    case DirtyFlags.PatchControlPoints:
                        RecordPatchControlPoints(extendedState2Api, commandBuffer);
                        break;
                    case DirtyFlags.PrimitiveRestart:
                        RecordPrimitiveRestartEnable(gd, commandBuffer);
                        break;
                    case DirtyFlags.PrimitiveTopology:
                        RecordPrimitiveTopology(extendedStateApi, commandBuffer);
                        break;
                    case DirtyFlags.DepthBiasEnable:
                        RecordDepthBiasEnable(extendedState2Api, commandBuffer);
                        break;
                }

                dirtyFlags &= ~currentFlag;
            }

            _dirty = DirtyFlags.None;
        }

        private void RecordBlend(Vk api, CommandBuffer commandBuffer)
        {
            api.CmdSetBlendConstants(commandBuffer, _blendConstants.AsSpan());
        }

        private readonly void RecordDepthBias(Vk api, CommandBuffer commandBuffer)
        {
            api.CmdSetDepthBias(commandBuffer, _depthBiasConstantFactor, _depthBiasClamp, _depthBiasSlopeFactor);
        }

        private readonly void RecordDepthBiasEnable(ExtExtendedDynamicState2 gd, CommandBuffer commandBuffer)
        {
            gd.CmdSetDepthBiasEnable(commandBuffer, _depthBiasEnable);
        }

        private void RecordScissor(VulkanRenderer gd, CommandBuffer commandBuffer)
        {
            if (ScissorsCount != 0)
            {
                if (gd.Capabilities.SupportsExtendedDynamicState)
                {
                    gd.ExtendedDynamicStateApi.CmdSetScissorWithCount(commandBuffer, (uint)ScissorsCount, _scissors.AsSpan());
                }
                else
                {
                    gd.Api.CmdSetScissor(commandBuffer, 0, (uint)ScissorsCount, _scissors.AsSpan());
                }
            }
        }

        private readonly void RecordStencil(Vk api, CommandBuffer commandBuffer)
        {
            api.CmdSetStencilCompareMask(commandBuffer, StencilFaceFlags.FaceBackBit, _backCompareMask);
            api.CmdSetStencilWriteMask(commandBuffer, StencilFaceFlags.FaceBackBit, _backWriteMask);
            api.CmdSetStencilReference(commandBuffer, StencilFaceFlags.FaceBackBit, _backReference);
            api.CmdSetStencilCompareMask(commandBuffer, StencilFaceFlags.FaceFrontBit, _frontCompareMask);
            api.CmdSetStencilWriteMask(commandBuffer, StencilFaceFlags.FaceFrontBit, _frontWriteMask);
            api.CmdSetStencilReference(commandBuffer, StencilFaceFlags.FaceFrontBit, _frontReference);
        }

        private readonly void RecordStencilTestAndOp(ExtExtendedDynamicState api, CommandBuffer commandBuffer)
        {
            api.CmdSetStencilTestEnable(commandBuffer, StencilTestEnable);

            api.CmdSetStencilOp(commandBuffer, StencilFaceFlags.FaceBackBit, _backFailOp, _backPassOp, _backDepthFailOp, _backCompareOp);
            api.CmdSetStencilOp(commandBuffer, StencilFaceFlags.FaceFrontBit, _frontFailOp, _frontPassOp, _frontDepthFailOp, _frontCompareOp);
        }

        private void RecordViewport(VulkanRenderer gd, CommandBuffer commandBuffer)
        {
            if (ViewportsCount == 0)
            {
                return;
            }

            if (gd.Capabilities.SupportsExtendedDynamicState)
            {
                gd.ExtendedDynamicStateApi.CmdSetViewportWithCount(commandBuffer, ViewportsCount, Viewports.AsSpan());
            }
            else
            {
                gd.Api.CmdSetViewport(commandBuffer, 0, ViewportsCount, Viewports.AsSpan());
            }
        }

        private readonly void RecordCullMode(ExtExtendedDynamicState api, CommandBuffer commandBuffer)
        {
            api.CmdSetCullMode(commandBuffer, CullMode);
        }

        private readonly void RecordFrontFace(ExtExtendedDynamicState api, CommandBuffer commandBuffer)
        {
            api.CmdSetFrontFace(commandBuffer, _frontFace);
        }

        private readonly void RecordDepthTestBool(ExtExtendedDynamicState api, CommandBuffer commandBuffer)
        {
            api.CmdSetDepthTestEnable(commandBuffer, DepthTestEnable);

            api.CmdSetDepthWriteEnable(commandBuffer, DepthWriteEnable);
        }

        private readonly void RecordDepthTestCompareOp(ExtExtendedDynamicState api, CommandBuffer commandBuffer)
        {
            api.CmdSetDepthCompareOp(commandBuffer, _depthCompareOp);
        }

        private readonly void RecordRasterizationDiscard(ExtExtendedDynamicState2 extendedDynamicState2Api, CommandBuffer commandBuffer)
        {
            extendedDynamicState2Api.CmdSetRasterizerDiscardEnable(commandBuffer, _discard);
        }

        private readonly void RecordPrimitiveRestartEnable(VulkanRenderer gd, CommandBuffer commandBuffer)
        {
            bool primitiveRestartEnable = _primitiveRestartEnable;

            bool topologySupportsRestart;

            if (gd.Capabilities.SupportsPrimitiveTopologyListRestart)
            {
                topologySupportsRestart = gd.Capabilities.SupportsPrimitiveTopologyPatchListRestart ||
                                          Topology != PrimitiveTopology.PatchList;
            }
            else
            {
                topologySupportsRestart = Topology == PrimitiveTopology.LineStrip ||
                                          Topology == PrimitiveTopology.TriangleStrip ||
                                          Topology == PrimitiveTopology.TriangleFan ||
                                          Topology == PrimitiveTopology.LineStripWithAdjacency ||
                                          Topology == PrimitiveTopology.TriangleStripWithAdjacency;
            }

            primitiveRestartEnable &= topologySupportsRestart;

            // Cannot disable primitiveRestartEnable for these Topologies on MacOS.
            if (gd.IsMoltenVk)
            {
                primitiveRestartEnable = true;
            }

            gd.ExtendedDynamicState2Api.CmdSetPrimitiveRestartEnable(commandBuffer, primitiveRestartEnable);
        }

        private readonly void RecordPrimitiveTopology(ExtExtendedDynamicState extendedDynamicStateApi, CommandBuffer commandBuffer)
        {
            extendedDynamicStateApi.CmdSetPrimitiveTopology(commandBuffer, Topology);
        }

        private readonly void RecordLogicOp(ExtExtendedDynamicState2 extendedDynamicState2Api, CommandBuffer commandBuffer)
        {
            extendedDynamicState2Api.CmdSetLogicOp(commandBuffer, _logicOp);
        }

        private readonly void RecordPatchControlPoints(ExtExtendedDynamicState2 extendedDynamicState2Api, CommandBuffer commandBuffer)
        {
            extendedDynamicState2Api.CmdSetPatchControlPoints(commandBuffer, _patchControlPoints);
        }

        private readonly void RecordLineWidth(Vk api, CommandBuffer commandBuffer)
        {
            api.CmdSetLineWidth(commandBuffer, _lineWidth);
        }

        private readonly void RecordFeedbackLoop(ExtAttachmentFeedbackLoopDynamicState api, CommandBuffer commandBuffer)
        {
            ImageAspectFlags aspects = (_feedbackLoopAspects & FeedbackLoopAspects.Color) != 0 ? ImageAspectFlags.ColorBit : 0;

            if ((_feedbackLoopAspects & FeedbackLoopAspects.Depth) != 0)
            {
                aspects |= ImageAspectFlags.DepthBit | ImageAspectFlags.StencilBit;
            }

            api.CmdSetAttachmentFeedbackLoopEnable(commandBuffer, aspects);
        }
    }
}

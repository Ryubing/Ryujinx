using Ryujinx.Common;
using Ryujinx.Graphics.GAL;
using Silk.NET.Vulkan;
using System;
using System.Numerics;

namespace Ryujinx.Graphics.Vulkan
{
    static class TextureCopy
    {
        public static void Blit(
            Vk api,
            CommandBuffer commandBuffer,
            Image srcImage,
            Image dstImage,
            TextureCreateInfo srcInfo,
            TextureCreateInfo dstInfo,
            Extents2D srcRegion,
            Extents2D dstRegion,
            int srcLayer,
            int dstLayer,
            int srcLevel,
            int dstLevel,
            int layers,
            int levels,
            bool linearFilter,
            ImageAspectFlags srcAspectFlags = 0,
            ImageAspectFlags dstAspectFlags = 0)
        {
            static (Offset3D, Offset3D) ExtentsToOffset3D(Extents2D extents, int width, int height, int level)
            {
                static int Clamp(int value, int max)
                {
                    return Math.Clamp(value, 0, max);
                }

                Offset3D xy1 = new(Clamp(extents.X1, width) >> level, Clamp(extents.Y1, height) >> level, 0);
                Offset3D xy2 = new(Clamp(extents.X2, width) >> level, Clamp(extents.Y2, height) >> level, 1);

                return (xy1, xy2);
            }

            if (srcAspectFlags == 0)
            {
                srcAspectFlags = srcInfo.Format.ConvertAspectFlags();
            }

            if (dstAspectFlags == 0)
            {
                dstAspectFlags = dstInfo.Format.ConvertAspectFlags();
            }

            ImageBlit.SrcOffsetsBuffer srcOffsets = new();
            ImageBlit.DstOffsetsBuffer dstOffsets = new();

            Filter filter = linearFilter && !dstInfo.Format.IsDepthOrStencil() ? Filter.Linear : Filter.Nearest;

            TextureView.InsertImageBarrier(
                api,
                commandBuffer,
                srcImage,
                TextureStorage.DefaultAccessMask,
                AccessFlags.TransferReadBit,
                PipelineStageFlags.AllCommandsBit,
                PipelineStageFlags.TransferBit,
                srcAspectFlags,
                srcLayer,
                srcLevel,
                layers,
                levels);

            uint copySrcLevel = (uint)srcLevel;
            uint copyDstLevel = (uint)dstLevel;

            for (int level = 0; level < levels; level++)
            {
                ImageSubresourceLayers srcSl = new(srcAspectFlags, copySrcLevel, (uint)srcLayer, (uint)layers);
                ImageSubresourceLayers dstSl = new(dstAspectFlags, copyDstLevel, (uint)dstLayer, (uint)layers);

                (srcOffsets.Element0, srcOffsets.Element1) = ExtentsToOffset3D(srcRegion, srcInfo.Width, srcInfo.Height, level);
                (dstOffsets.Element0, dstOffsets.Element1) = ExtentsToOffset3D(dstRegion, dstInfo.Width, dstInfo.Height, level);

                ImageBlit region = new()
                {
                    SrcSubresource = srcSl,
                    SrcOffsets = srcOffsets,
                    DstSubresource = dstSl,
                    DstOffsets = dstOffsets,
                };

                api.CmdBlitImage(commandBuffer, srcImage, ImageLayout.General, dstImage, ImageLayout.General, 1, in region, filter);

                copySrcLevel++;
                copyDstLevel++;

                if (srcInfo.Target == Target.Texture3D || dstInfo.Target == Target.Texture3D)
                {
                    layers = Math.Max(1, layers >> 1);
                }
            }

            TextureView.InsertImageBarrier(
                api,
                commandBuffer,
                dstImage,
                AccessFlags.TransferWriteBit,
                TextureStorage.DefaultAccessMask,
                PipelineStageFlags.TransferBit,
                PipelineStageFlags.AllCommandsBit,
                dstAspectFlags,
                dstLayer,
                dstLevel,
                layers,
                levels);
        }

        public static void Copy(
            Vk api,
            CommandBuffer commandBuffer,
            Image srcImage,
            Image dstImage,
            TextureCreateInfo srcInfo,
            TextureCreateInfo dstInfo,
            int srcViewLayer,
            int dstViewLayer,
            int srcViewLevel,
            int dstViewLevel,
            int srcLayer,
            int dstLayer,
            int srcLevel,
            int dstLevel)
        {
            int srcDepth = srcInfo.GetDepthOrLayers();
            int srcLevels = srcInfo.Levels;

            int dstDepth = dstInfo.GetDepthOrLayers();
            int dstLevels = dstInfo.Levels;

            if (dstInfo.Target == Target.Texture3D)
            {
                dstDepth = Math.Max(1, dstDepth >> dstLevel);
            }

            int depth = Math.Min(srcDepth, dstDepth);
            int levels = Math.Min(srcLevels, dstLevels);

            Copy(
                api,
                commandBuffer,
                srcImage,
                dstImage,
                srcInfo,
                dstInfo,
                srcViewLayer,
                dstViewLayer,
                srcViewLevel,
                dstViewLevel,
                srcLayer,
                dstLayer,
                srcLevel,
                dstLevel,
                depth,
                levels);
        }

        private static int ClampLevels(TextureCreateInfo info, int levels)
        {
            int width = info.Width;
            int height = info.Height;
            int depth = info.Target == Target.Texture3D ? info.Depth : 1;

            int maxLevels = 1 + BitOperations.Log2((uint)Math.Max(Math.Max(width, height), depth));

            if (levels > maxLevels)
            {
                levels = maxLevels;
            }

            return levels;
        }

        public static void Copy(
            Vk api,
            CommandBuffer commandBuffer,
            Image srcImage,
            Image dstImage,
            TextureCreateInfo srcInfo,
            TextureCreateInfo dstInfo,
            int srcViewLayer,
            int dstViewLayer,
            int srcViewLevel,
            int dstViewLevel,
            int srcDepthOrLayer,
            int dstDepthOrLayer,
            int srcLevel,
            int dstLevel,
            int depthOrLayers,
            int levels)
        {
            int srcZ;
            int srcLayer;
            int srcDepth;
            int srcLayers;

            if (srcInfo.Target == Target.Texture3D)
            {
                srcZ = srcDepthOrLayer;
                srcLayer = 0;
                srcDepth = depthOrLayers;
                srcLayers = 1;
            }
            else
            {
                srcZ = 0;
                srcLayer = srcDepthOrLayer;
                srcDepth = 1;
                srcLayers = depthOrLayers;
            }

            int dstZ;
            int dstLayer;
            int dstLayers;

            if (dstInfo.Target == Target.Texture3D)
            {
                dstZ = dstDepthOrLayer;
                dstLayer = 0;
                dstLayers = 1;
            }
            else
            {
                dstZ = 0;
                dstLayer = dstDepthOrLayer;
                dstLayers = depthOrLayers;
            }

            int srcWidth = srcInfo.Width;
            int srcHeight = srcInfo.Height;

            int dstWidth = dstInfo.Width;
            int dstHeight = dstInfo.Height;

            srcWidth = Math.Max(1, srcWidth >> srcLevel);
            srcHeight = Math.Max(1, srcHeight >> srcLevel);

            dstWidth = Math.Max(1, dstWidth >> dstLevel);
            dstHeight = Math.Max(1, dstHeight >> dstLevel);

            int blockWidth = 1;
            int blockHeight = 1;
            bool sizeInBlocks = false;

            // When copying from a compressed to a non-compressed format,
            // the non-compressed texture will have the size of the texture
            // in blocks (not in texels), so we must adjust that size to
            // match the size in texels of the compressed texture.
            if (!srcInfo.IsCompressed && dstInfo.IsCompressed)
            {
                srcWidth *= dstInfo.BlockWidth;
                srcHeight *= dstInfo.BlockHeight;
                blockWidth = dstInfo.BlockWidth;
                blockHeight = dstInfo.BlockHeight;

                sizeInBlocks = true;
            }
            else if (srcInfo.IsCompressed && !dstInfo.IsCompressed)
            {
                dstWidth *= srcInfo.BlockWidth;
                dstHeight *= srcInfo.BlockHeight;
                blockWidth = srcInfo.BlockWidth;
                blockHeight = srcInfo.BlockHeight;
            }

            int width = Math.Min(srcWidth, dstWidth);
            int height = Math.Min(srcHeight, dstHeight);

            ImageAspectFlags srcAspect = srcInfo.Format.ConvertAspectFlags();
            ImageAspectFlags dstAspect = dstInfo.Format.ConvertAspectFlags();

            TextureView.InsertImageBarrier(
                api,
                commandBuffer,
                srcImage,
                TextureStorage.DefaultAccessMask,
                AccessFlags.TransferReadBit,
                PipelineStageFlags.AllCommandsBit,
                PipelineStageFlags.TransferBit,
                srcAspect,
                srcViewLayer + srcLayer,
                srcViewLevel + srcLevel,
                srcLayers,
                levels);

            for (int level = 0; level < levels; level++)
            {
                // Stop copy if we are already out of the levels range.
                if (level >= srcInfo.Levels || dstLevel + level >= dstInfo.Levels)
                {
                    break;
                }

                ImageSubresourceLayers srcSl = new(
                    srcAspect,
                    (uint)(srcViewLevel + srcLevel + level),
                    (uint)(srcViewLayer + srcLayer),
                    (uint)srcLayers);

                ImageSubresourceLayers dstSl = new(
                    dstAspect,
                    (uint)(dstViewLevel + dstLevel + level),
                    (uint)(dstViewLayer + dstLayer),
                    (uint)dstLayers);

                int copyWidth = sizeInBlocks ? BitUtils.DivRoundUp(width, blockWidth) : width;
                int copyHeight = sizeInBlocks ? BitUtils.DivRoundUp(height, blockHeight) : height;

                Extent3D extent = new((uint)copyWidth, (uint)copyHeight, (uint)srcDepth);

                if (srcInfo.Samples > 1 && srcInfo.Samples != dstInfo.Samples)
                {
                    ImageResolve region = new(srcSl, new Offset3D(0, 0, srcZ), dstSl, new Offset3D(0, 0, dstZ), extent);

                    api.CmdResolveImage(commandBuffer, srcImage, ImageLayout.General, dstImage, ImageLayout.General, 1, in region);
                }
                else
                {
                    ImageCopy region = new(srcSl, new Offset3D(0, 0, srcZ), dstSl, new Offset3D(0, 0, dstZ), extent);

                    api.CmdCopyImage(commandBuffer, srcImage, ImageLayout.General, dstImage, ImageLayout.General, 1, in region);
                }

                width = Math.Max(1, width >> 1);
                height = Math.Max(1, height >> 1);

                if (srcInfo.Target == Target.Texture3D)
                {
                    srcDepth = Math.Max(1, srcDepth >> 1);
                }
            }

            TextureView.InsertImageBarrier(
                api,
                commandBuffer,
                dstImage,
                AccessFlags.TransferWriteBit,
                TextureStorage.DefaultAccessMask,
                PipelineStageFlags.TransferBit,
                PipelineStageFlags.AllCommandsBit,
                dstAspect,
                dstViewLayer + dstLayer,
                dstViewLevel + dstLevel,
                dstLayers,
                levels);
        }

        public unsafe static void ResolveDepthStencil(
            VulkanRenderer gd,
            Device device,
            CommandBufferScoped cbs,
            TextureView src,
            TextureView dst)
        {
            AttachmentReference2 dsAttachmentReference = new(StructureType.AttachmentReference2, null, 0, ImageLayout.General);
            AttachmentReference2 dsResolveAttachmentReference = new(StructureType.AttachmentReference2, null, 1, ImageLayout.General);

            SubpassDescriptionDepthStencilResolve subpassDsResolve = new()
            {
                SType = StructureType.SubpassDescriptionDepthStencilResolve,
                PDepthStencilResolveAttachment = &dsResolveAttachmentReference,
                DepthResolveMode = ResolveModeFlags.SampleZeroBit,
                StencilResolveMode = ResolveModeFlags.SampleZeroBit,
            };

            SubpassDescription2 subpass = new()
            {
                SType = StructureType.SubpassDescription2,
                PipelineBindPoint = PipelineBindPoint.Graphics,
                PDepthStencilAttachment = &dsAttachmentReference,
                PNext = &subpassDsResolve,
            };

            AttachmentDescription2[] attachmentDescs = new AttachmentDescription2[2];

            attachmentDescs[0] = new AttachmentDescription2(
                StructureType.AttachmentDescription2,
                null,
                0,
                src.VkFormat,
                TextureStorage.ConvertToSampleCountFlags(gd.Capabilities.SupportedSampleCounts, (uint)src.Info.Samples),
                AttachmentLoadOp.Load,
                AttachmentStoreOp.Store,
                AttachmentLoadOp.Load,
                AttachmentStoreOp.Store,
                ImageLayout.General,
                ImageLayout.General);

            attachmentDescs[1] = new AttachmentDescription2(
                StructureType.AttachmentDescription2,
                null,
                0,
                dst.VkFormat,
                TextureStorage.ConvertToSampleCountFlags(gd.Capabilities.SupportedSampleCounts, (uint)dst.Info.Samples),
                AttachmentLoadOp.Load,
                AttachmentStoreOp.Store,
                AttachmentLoadOp.Load,
                AttachmentStoreOp.Store,
                ImageLayout.General,
                ImageLayout.General);

            SubpassDependency2 subpassDependency = PipelineConverter.CreateSubpassDependency2(gd);

            fixed (AttachmentDescription2* pAttachmentDescs = attachmentDescs)
            {
                RenderPassCreateInfo2 renderPassCreateInfo = new()
                {
                    SType = StructureType.RenderPassCreateInfo2,
                    PAttachments = pAttachmentDescs,
                    AttachmentCount = (uint)attachmentDescs.Length,
                    PSubpasses = &subpass,
                    SubpassCount = 1,
                    PDependencies = &subpassDependency,
                    DependencyCount = 1,
                };

                gd.Api.CreateRenderPass2(device, in renderPassCreateInfo, null, out RenderPass renderPass).ThrowOnError();

                using Auto<DisposableRenderPass> rp = new(new DisposableRenderPass(gd.Api, device, renderPass));

                ImageView* attachments = stackalloc ImageView[2];

                Auto<DisposableImageView> srcView = src.GetImageViewForAttachment();
                Auto<DisposableImageView> dstView = dst.GetImageViewForAttachment();

                attachments[0] = srcView.Get(cbs).Value;
                attachments[1] = dstView.Get(cbs).Value;

                FramebufferCreateInfo framebufferCreateInfo = new()
                {
                    SType = StructureType.FramebufferCreateInfo,
                    RenderPass = rp.Get(cbs).Value,
                    AttachmentCount = 2,
                    PAttachments = attachments,
                    Width = (uint)src.Width,
                    Height = (uint)src.Height,
                    Layers = (uint)src.Layers,
                };

                gd.Api.CreateFramebuffer(device, in framebufferCreateInfo, null, out Framebuffer framebuffer).ThrowOnError();
                using Auto<DisposableFramebuffer> fb = new(new DisposableFramebuffer(gd.Api, device, framebuffer), null, srcView, dstView);

                Rect2D renderArea = new(null, new Extent2D((uint)src.Info.Width, (uint)src.Info.Height));
                ClearValue clearValue = new();

                RenderPassBeginInfo renderPassBeginInfo = new()
                {
                    SType = StructureType.RenderPassBeginInfo,
                    RenderPass = rp.Get(cbs).Value,
                    Framebuffer = fb.Get(cbs).Value,
                    RenderArea = renderArea,
                    PClearValues = &clearValue,
                    ClearValueCount = 1,
                };

                // The resolve operation happens at the end of the subpass, so let's just do a begin/end
                // to resolve the depth-stencil texture.
                // TODO: Do speculative resolve and part of the same render pass as the draw to avoid
                // ending the current render pass?
                gd.Api.CmdBeginRenderPass(cbs.CommandBuffer, in renderPassBeginInfo, SubpassContents.Inline);
                gd.Api.CmdEndRenderPass(cbs.CommandBuffer);
            }
        }
    }
}

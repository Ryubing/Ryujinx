using Ryujinx.Common;
using Ryujinx.Common.Logging;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Shader;
using Ryujinx.Graphics.Shader.Translation;
using SharpMetal.Metal;
using System;
using System.Collections.Generic;
using System.Runtime.Versioning;

namespace Ryujinx.Graphics.Metal
{
    [SupportedOSPlatform("macos")]
    class HelperShader : IDisposable
    {
        private const int ConvertElementsPerWorkgroup = 32 * 100; // Work group size of 32 times 100 elements.
        private const string ShadersSourcePath = "/Ryujinx.Graphics.Metal/Shaders";
        private readonly MetalRenderer _renderer;
        private readonly Pipeline _pipeline;
        private MTLDevice _device;

        private readonly ISampler _samplerLinear;
        private readonly ISampler _samplerNearest;
        private readonly IProgram _programColorBlitF;
        private readonly IProgram _programColorBlitI;
        private readonly IProgram _programColorBlitU;
        private readonly IProgram _programColorBlitMsF;
        private readonly IProgram _programColorBlitMsI;
        private readonly IProgram _programColorBlitMsU;
        private readonly List<IProgram> _programsColorClearF = [];
        private readonly List<IProgram> _programsColorClearI = [];
        private readonly List<IProgram> _programsColorClearU = [];
        private readonly IProgram _programDepthStencilClear;
        private readonly IProgram _programStrideChange;
        private readonly IProgram _programConvertD32S8ToD24S8;
        private readonly IProgram _programConvertIndexBuffer;
        private readonly IProgram _programDepthBlit;
        private readonly IProgram _programDepthBlitMs;
        private readonly IProgram _programStencilBlit;
        private readonly IProgram _programStencilBlitMs;

        private readonly EncoderState _helperShaderState = new();

        public HelperShader(MTLDevice device, MetalRenderer renderer, Pipeline pipeline)
        {
            _device = device;
            _renderer = renderer;
            _pipeline = pipeline;

            _samplerNearest = new SamplerHolder(renderer, _device, SamplerCreateInfo.Create(MinFilter.Nearest, MagFilter.Nearest));
            _samplerLinear = new SamplerHolder(renderer, _device, SamplerCreateInfo.Create(MinFilter.Linear, MagFilter.Linear));

            ResourceLayout blitResourceLayout = new ResourceLayoutBuilder()
                .Add(ResourceStages.Vertex, ResourceType.UniformBuffer, 0)
                .Add(ResourceStages.Fragment, ResourceType.TextureAndSampler, 0).Build();

            string blitSource = ReadMsl("Blit.metal");

            string blitSourceF = blitSource.Replace("FORMAT", "float", StringComparison.Ordinal);
            _programColorBlitF = new Program(renderer, device, [
                new ShaderSource(blitSourceF, ShaderStage.Fragment, TargetLanguage.Msl),
                new ShaderSource(blitSourceF, ShaderStage.Vertex, TargetLanguage.Msl)
            ], blitResourceLayout);

            string blitSourceI = blitSource.Replace("FORMAT", "int");
            _programColorBlitI = new Program(renderer, device, [
                new ShaderSource(blitSourceI, ShaderStage.Fragment, TargetLanguage.Msl),
                new ShaderSource(blitSourceI, ShaderStage.Vertex, TargetLanguage.Msl)
            ], blitResourceLayout);

            string blitSourceU = blitSource.Replace("FORMAT", "uint");
            _programColorBlitU = new Program(renderer, device, [
                new ShaderSource(blitSourceU, ShaderStage.Fragment, TargetLanguage.Msl),
                new ShaderSource(blitSourceU, ShaderStage.Vertex, TargetLanguage.Msl)
            ], blitResourceLayout);

            string blitMsSource = ReadMsl("BlitMs.metal");

            string blitMsSourceF = blitMsSource.Replace("FORMAT", "float");
            _programColorBlitMsF = new Program(renderer, device, [
                new ShaderSource(blitMsSourceF, ShaderStage.Fragment, TargetLanguage.Msl),
                new ShaderSource(blitMsSourceF, ShaderStage.Vertex, TargetLanguage.Msl)
            ], blitResourceLayout);

            string blitMsSourceI = blitMsSource.Replace("FORMAT", "int");
            _programColorBlitMsI = new Program(renderer, device, [
                new ShaderSource(blitMsSourceI, ShaderStage.Fragment, TargetLanguage.Msl),
                new ShaderSource(blitMsSourceI, ShaderStage.Vertex, TargetLanguage.Msl)
            ], blitResourceLayout);

            string blitMsSourceU = blitMsSource.Replace("FORMAT", "uint");
            _programColorBlitMsU = new Program(renderer, device, [
                new ShaderSource(blitMsSourceU, ShaderStage.Fragment, TargetLanguage.Msl),
                new ShaderSource(blitMsSourceU, ShaderStage.Vertex, TargetLanguage.Msl)
            ], blitResourceLayout);

            ResourceLayout colorClearResourceLayout = new ResourceLayoutBuilder()
                .Add(ResourceStages.Fragment, ResourceType.UniformBuffer, 0).Build();

            string colorClearSource = ReadMsl("ColorClear.metal");

            for (int i = 0; i < Constants.MaxColorAttachments; i++)
            {
                string crntSource = colorClearSource.Replace("COLOR_ATTACHMENT_INDEX", i.ToString()).Replace("FORMAT", "float");
                _programsColorClearF.Add(new Program(renderer, device, [
                    new ShaderSource(crntSource, ShaderStage.Fragment, TargetLanguage.Msl),
                    new ShaderSource(crntSource, ShaderStage.Vertex, TargetLanguage.Msl)
                ], colorClearResourceLayout));
            }

            for (int i = 0; i < Constants.MaxColorAttachments; i++)
            {
                string crntSource = colorClearSource.Replace("COLOR_ATTACHMENT_INDEX", i.ToString()).Replace("FORMAT", "int");
                _programsColorClearI.Add(new Program(renderer, device, [
                    new ShaderSource(crntSource, ShaderStage.Fragment, TargetLanguage.Msl),
                    new ShaderSource(crntSource, ShaderStage.Vertex, TargetLanguage.Msl)
                ], colorClearResourceLayout));
            }

            for (int i = 0; i < Constants.MaxColorAttachments; i++)
            {
                string crntSource = colorClearSource.Replace("COLOR_ATTACHMENT_INDEX", i.ToString()).Replace("FORMAT", "uint");
                _programsColorClearU.Add(new Program(renderer, device, [
                    new ShaderSource(crntSource, ShaderStage.Fragment, TargetLanguage.Msl),
                    new ShaderSource(crntSource, ShaderStage.Vertex, TargetLanguage.Msl)
                ], colorClearResourceLayout));
            }

            string depthStencilClearSource = ReadMsl("DepthStencilClear.metal");
            _programDepthStencilClear = new Program(renderer, device, [
                new ShaderSource(depthStencilClearSource, ShaderStage.Fragment, TargetLanguage.Msl),
                new ShaderSource(depthStencilClearSource, ShaderStage.Vertex, TargetLanguage.Msl)
            ], colorClearResourceLayout);

            ResourceLayout strideChangeResourceLayout = new ResourceLayoutBuilder()
                .Add(ResourceStages.Compute, ResourceType.UniformBuffer, 0)
                .Add(ResourceStages.Compute, ResourceType.StorageBuffer, 1)
                .Add(ResourceStages.Compute, ResourceType.StorageBuffer, 2, true).Build();

            string strideChangeSource = ReadMsl("ChangeBufferStride.metal");
            _programStrideChange = new Program(renderer, device, [
                new ShaderSource(strideChangeSource, ShaderStage.Compute, TargetLanguage.Msl)
            ], strideChangeResourceLayout, new ComputeSize(64, 1, 1));

            ResourceLayout convertD32S8ToD24S8ResourceLayout = new ResourceLayoutBuilder()
                .Add(ResourceStages.Compute, ResourceType.UniformBuffer, 0)
                .Add(ResourceStages.Compute, ResourceType.StorageBuffer, 1)
                .Add(ResourceStages.Compute, ResourceType.StorageBuffer, 2, true).Build();

            string convertD32S8ToD24S8Source = ReadMsl("ConvertD32S8ToD24S8.metal");
            _programConvertD32S8ToD24S8 = new Program(renderer, device, [
                new ShaderSource(convertD32S8ToD24S8Source, ShaderStage.Compute, TargetLanguage.Msl)
            ], convertD32S8ToD24S8ResourceLayout, new ComputeSize(64, 1, 1));

            ResourceLayout convertIndexBufferLayout = new ResourceLayoutBuilder()
                .Add(ResourceStages.Compute, ResourceType.StorageBuffer, 1)
                .Add(ResourceStages.Compute, ResourceType.StorageBuffer, 2, true)
                .Add(ResourceStages.Compute, ResourceType.StorageBuffer, 3).Build();

            string convertIndexBufferSource = ReadMsl("ConvertIndexBuffer.metal");
            _programConvertIndexBuffer = new Program(renderer, device, [
                new ShaderSource(convertIndexBufferSource, ShaderStage.Compute, TargetLanguage.Msl)
            ], convertIndexBufferLayout, new ComputeSize(16, 1, 1));

            string depthBlitSource = ReadMsl("DepthBlit.metal");
            _programDepthBlit = new Program(renderer, device, [
                new ShaderSource(depthBlitSource, ShaderStage.Fragment, TargetLanguage.Msl),
                new ShaderSource(blitSourceF, ShaderStage.Vertex, TargetLanguage.Msl)
            ], blitResourceLayout);

            string depthBlitMsSource = ReadMsl("DepthBlitMs.metal");
            _programDepthBlitMs = new Program(renderer, device, [
                new ShaderSource(depthBlitMsSource, ShaderStage.Fragment, TargetLanguage.Msl),
                new ShaderSource(blitSourceF, ShaderStage.Vertex, TargetLanguage.Msl)
            ], blitResourceLayout);

            string stencilBlitSource = ReadMsl("StencilBlit.metal");
            _programStencilBlit = new Program(renderer, device, [
                new ShaderSource(stencilBlitSource, ShaderStage.Fragment, TargetLanguage.Msl),
                new ShaderSource(blitSourceF, ShaderStage.Vertex, TargetLanguage.Msl)
            ], blitResourceLayout);

            string stencilBlitMsSource = ReadMsl("StencilBlitMs.metal");
            _programStencilBlitMs = new Program(renderer, device, [
                new ShaderSource(stencilBlitMsSource, ShaderStage.Fragment, TargetLanguage.Msl),
                new ShaderSource(blitSourceF, ShaderStage.Vertex, TargetLanguage.Msl)
            ], blitResourceLayout);
        }

        private static string ReadMsl(string fileName)
        {
            string msl = EmbeddedResources.ReadAllText(string.Join('/', ShadersSourcePath, fileName));

#pragma warning disable IDE0055 // Disable formatting
            msl = msl.Replace("CONSTANT_BUFFERS_INDEX", $"{Constants.ConstantBuffersIndex}")
                     .Replace("STORAGE_BUFFERS_INDEX", $"{Constants.StorageBuffersIndex}")
                     .Replace("TEXTURES_INDEX", $"{Constants.TexturesIndex}")
                     .Replace("IMAGES_INDEX", $"{Constants.ImagesIndex}");
#pragma warning restore IDE0055

            return msl;
        }

        public unsafe void BlitColor(
            CommandBufferScoped cbs,
            Texture src,
            Texture dst,
            Extents2D srcRegion,
            Extents2D dstRegion,
            bool linearFilter,
            bool clear = false)
        {
            _pipeline.SwapState(_helperShaderState);

            const int RegionBufferSize = 16;

            ISampler sampler = linearFilter ? _samplerLinear : _samplerNearest;

            _pipeline.SetTextureAndSampler(ShaderStage.Fragment, 0, src, sampler);

            Span<float> region = stackalloc float[RegionBufferSize / sizeof(float)];

            region[0] = srcRegion.X1 / (float)src.Width;
            region[1] = srcRegion.X2 / (float)src.Width;
            region[2] = srcRegion.Y1 / (float)src.Height;
            region[3] = srcRegion.Y2 / (float)src.Height;

            if (dstRegion.X1 > dstRegion.X2)
            {
                (region[0], region[1]) = (region[1], region[0]);
            }

            if (dstRegion.Y1 > dstRegion.Y2)
            {
                (region[2], region[3]) = (region[3], region[2]);
            }

            using ScopedTemporaryBuffer buffer = _renderer.BufferManager.ReserveOrCreate(cbs, RegionBufferSize);
            buffer.Holder.SetDataUnchecked<float>(buffer.Offset, region);
            _pipeline.SetUniformBuffers([new BufferAssignment(0, buffer.Range)]);

            Rectangle<float> rect = new(
                MathF.Min(dstRegion.X1, dstRegion.X2),
                MathF.Min(dstRegion.Y1, dstRegion.Y2),
                MathF.Abs(dstRegion.X2 - dstRegion.X1),
                MathF.Abs(dstRegion.Y2 - dstRegion.Y1));

            Span<Viewport> viewports = stackalloc Viewport[16];

            viewports[0] = new Viewport(
                rect,
                ViewportSwizzle.PositiveX,
                ViewportSwizzle.PositiveY,
                ViewportSwizzle.PositiveZ,
                ViewportSwizzle.PositiveW,
                0f,
                1f);

            bool dstIsDepthOrStencil = dst.Info.Format.IsDepthOrStencil();

            if (dstIsDepthOrStencil)
            {
                // TODO: Depth & stencil blit!
                Logger.Warning?.PrintMsg(LogClass.Gpu, "Requested a depth or stencil blit!");
                _pipeline.SwapState(null);
                return;
            }

            string debugGroupName = "Blit Color ";

            if (src.Info.Target.IsMultisample())
            {
                if (dst.Info.Format.IsSint())
                {
                    debugGroupName += "MS Int";
                    _pipeline.SetProgram(_programColorBlitMsI);
                }
                else if (dst.Info.Format.IsUint())
                {
                    debugGroupName += "MS UInt";
                    _pipeline.SetProgram(_programColorBlitMsU);
                }
                else
                {
                    debugGroupName += "MS Float";
                    _pipeline.SetProgram(_programColorBlitMsF);
                }
            }
            else
            {
                if (dst.Info.Format.IsSint())
                {
                    debugGroupName += "Int";
                    _pipeline.SetProgram(_programColorBlitI);
                }
                else if (dst.Info.Format.IsUint())
                {
                    debugGroupName += "UInt";
                    _pipeline.SetProgram(_programColorBlitU);
                }
                else
                {
                    debugGroupName += "Float";
                    _pipeline.SetProgram(_programColorBlitF);
                }
            }

            int dstWidth = dst.Width;
            int dstHeight = dst.Height;

            Span<Rectangle<int>> scissors = stackalloc Rectangle<int>[16];

            scissors[0] = new Rectangle<int>(0, 0, dstWidth, dstHeight);

            _pipeline.SetRenderTargets([dst], null);
            _pipeline.SetScissors(scissors);

            _pipeline.SetClearLoadAction(clear);

            _pipeline.SetViewports(viewports);
            _pipeline.SetPrimitiveTopology(PrimitiveTopology.TriangleStrip);
            _pipeline.Draw(4, 1, 0, 0, debugGroupName);

            // Cleanup
            if (clear)
            {
                _pipeline.SetClearLoadAction(false);
            }

            // Restore previous state
            _pipeline.SwapState(null);
        }

        public unsafe void BlitDepthStencil(
            CommandBufferScoped cbs,
            Texture src,
            Texture dst,
            Extents2D srcRegion,
            Extents2D dstRegion)
        {
            _pipeline.SwapState(_helperShaderState);

            const int RegionBufferSize = 16;

            Span<float> region = stackalloc float[RegionBufferSize / sizeof(float)];

            region[0] = srcRegion.X1 / (float)src.Width;
            region[1] = srcRegion.X2 / (float)src.Width;
            region[2] = srcRegion.Y1 / (float)src.Height;
            region[3] = srcRegion.Y2 / (float)src.Height;

            if (dstRegion.X1 > dstRegion.X2)
            {
                (region[0], region[1]) = (region[1], region[0]);
            }

            if (dstRegion.Y1 > dstRegion.Y2)
            {
                (region[2], region[3]) = (region[3], region[2]);
            }

            using ScopedTemporaryBuffer buffer = _renderer.BufferManager.ReserveOrCreate(cbs, RegionBufferSize);
            buffer.Holder.SetDataUnchecked<float>(buffer.Offset, region);
            _pipeline.SetUniformBuffers([new BufferAssignment(0, buffer.Range)]);

            Span<Viewport> viewports = stackalloc Viewport[16];

            Rectangle<float> rect = new(
                MathF.Min(dstRegion.X1, dstRegion.X2),
                MathF.Min(dstRegion.Y1, dstRegion.Y2),
                MathF.Abs(dstRegion.X2 - dstRegion.X1),
                MathF.Abs(dstRegion.Y2 - dstRegion.Y1));

            viewports[0] = new Viewport(
                rect,
                ViewportSwizzle.PositiveX,
                ViewportSwizzle.PositiveY,
                ViewportSwizzle.PositiveZ,
                ViewportSwizzle.PositiveW,
                0f,
                1f);

            int dstWidth = dst.Width;
            int dstHeight = dst.Height;

            Span<Rectangle<int>> scissors = stackalloc Rectangle<int>[16];

            scissors[0] = new Rectangle<int>(0, 0, dstWidth, dstHeight);

            _pipeline.SetRenderTargets([], dst);
            _pipeline.SetScissors(scissors);
            _pipeline.SetViewports(viewports);
            _pipeline.SetPrimitiveTopology(PrimitiveTopology.TriangleStrip);

            if (src.Info.Format is
                Format.D16Unorm or
                Format.D32Float or
                Format.X8UintD24Unorm or
                Format.D24UnormS8Uint or
                Format.D32FloatS8Uint or
                Format.S8UintD24Unorm)
            {
                Texture depthTexture = CreateDepthOrStencilView(src, DepthStencilMode.Depth);

                BlitDepthStencilDraw(depthTexture, isDepth: true);

                if (depthTexture != src)
                {
                    depthTexture.Release();
                }
            }

            if (src.Info.Format is
                Format.S8Uint or
                Format.D24UnormS8Uint or
                Format.D32FloatS8Uint or
                Format.S8UintD24Unorm)
            {
                Texture stencilTexture = CreateDepthOrStencilView(src, DepthStencilMode.Stencil);

                BlitDepthStencilDraw(stencilTexture, isDepth: false);

                if (stencilTexture != src)
                {
                    stencilTexture.Release();
                }
            }

            // Restore previous state
            _pipeline.SwapState(null);
        }

        private static Texture CreateDepthOrStencilView(Texture depthStencilTexture, DepthStencilMode depthStencilMode)
        {
            if (depthStencilTexture.Info.DepthStencilMode == depthStencilMode)
            {
                return depthStencilTexture;
            }

            return (Texture)depthStencilTexture.CreateView(new TextureCreateInfo(
                depthStencilTexture.Info.Width,
                depthStencilTexture.Info.Height,
                depthStencilTexture.Info.Depth,
                depthStencilTexture.Info.Levels,
                depthStencilTexture.Info.Samples,
                depthStencilTexture.Info.BlockWidth,
                depthStencilTexture.Info.BlockHeight,
                depthStencilTexture.Info.BytesPerPixel,
                depthStencilTexture.Info.Format,
                depthStencilMode,
                depthStencilTexture.Info.Target,
                SwizzleComponent.Red,
                SwizzleComponent.Green,
                SwizzleComponent.Blue,
                SwizzleComponent.Alpha), 0, 0);
        }

        private void BlitDepthStencilDraw(Texture src, bool isDepth)
        {
            // TODO: Check this https://github.com/Ryujinx/Ryujinx/pull/5003/
            _pipeline.SetTextureAndSampler(ShaderStage.Fragment, 0, src, _samplerNearest);

            string debugGroupName;

            if (isDepth)
            {
                debugGroupName = "Depth Blit";
                _pipeline.SetProgram(src.Info.Target.IsMultisample() ? _programDepthBlitMs : _programDepthBlit);
                _pipeline.SetDepthTest(new DepthTestDescriptor(true, true, CompareOp.Always));
            }
            else
            {
                debugGroupName = "Stencil Blit";
                _pipeline.SetProgram(src.Info.Target.IsMultisample() ? _programStencilBlitMs : _programStencilBlit);
                _pipeline.SetStencilTest(CreateStencilTestDescriptor(true));
            }

            _pipeline.Draw(4, 1, 0, 0, debugGroupName);

            if (isDepth)
            {
                _pipeline.SetDepthTest(new DepthTestDescriptor(false, false, CompareOp.Always));
            }
            else
            {
                _pipeline.SetStencilTest(CreateStencilTestDescriptor(false));
            }
        }

        public unsafe void DrawTexture(
            ITexture src,
            ISampler srcSampler,
            Extents2DF srcRegion,
            Extents2DF dstRegion)
        {
            // Save current state
            PredrawState state = _pipeline.SavePredrawState();

            _pipeline.SetFaceCulling(false, Face.Front);
            _pipeline.SetStencilTest(new StencilTestDescriptor());
            _pipeline.SetDepthTest(new DepthTestDescriptor());

            const int RegionBufferSize = 16;

            _pipeline.SetTextureAndSampler(ShaderStage.Fragment, 0, src, srcSampler);

            Span<float> region = stackalloc float[RegionBufferSize / sizeof(float)];

            region[0] = srcRegion.X1 / src.Width;
            region[1] = srcRegion.X2 / src.Width;
            region[2] = srcRegion.Y1 / src.Height;
            region[3] = srcRegion.Y2 / src.Height;

            if (dstRegion.X1 > dstRegion.X2)
            {
                (region[0], region[1]) = (region[1], region[0]);
            }

            if (dstRegion.Y1 > dstRegion.Y2)
            {
                (region[2], region[3]) = (region[3], region[2]);
            }

            BufferHandle bufferHandle = _renderer.BufferManager.CreateWithHandle(RegionBufferSize);
            _renderer.BufferManager.SetData<float>(bufferHandle, 0, region);
            _pipeline.SetUniformBuffers([new BufferAssignment(0, new BufferRange(bufferHandle, 0, RegionBufferSize))]);

            Span<Viewport> viewports = stackalloc Viewport[16];

            Rectangle<float> rect = new(
                MathF.Min(dstRegion.X1, dstRegion.X2),
                MathF.Min(dstRegion.Y1, dstRegion.Y2),
                MathF.Abs(dstRegion.X2 - dstRegion.X1),
                MathF.Abs(dstRegion.Y2 - dstRegion.Y1));

            viewports[0] = new Viewport(
                rect,
                ViewportSwizzle.PositiveX,
                ViewportSwizzle.PositiveY,
                ViewportSwizzle.PositiveZ,
                ViewportSwizzle.PositiveW,
                0f,
                1f);

            _pipeline.SetProgram(_programColorBlitF);
            _pipeline.SetViewports(viewports);
            _pipeline.SetPrimitiveTopology(PrimitiveTopology.TriangleStrip);
            _pipeline.Draw(4, 1, 0, 0, "Draw Texture");

            _renderer.BufferManager.Delete(bufferHandle);

            // Restore previous state
            _pipeline.RestorePredrawState(state);
        }

        public void ConvertI8ToI16(CommandBufferScoped cbs, BufferHolder src, BufferHolder dst, int srcOffset, int size)
        {
            ChangeStride(cbs, src, dst, srcOffset, size, 1, 2);
        }

        public unsafe void ChangeStride(
            CommandBufferScoped cbs,
            BufferHolder src,
            BufferHolder dst,
            int srcOffset,
            int size,
            int stride,
            int newStride)
        {
            int elems = size / stride;

            Auto<DisposableBuffer> srcBuffer = src.GetBuffer();
            Auto<DisposableBuffer> dstBuffer = dst.GetBuffer();

            const int ParamsBufferSize = 4 * sizeof(int);

            // Save current state
            _pipeline.SwapState(_helperShaderState);

            Span<int> shaderParams = stackalloc int[ParamsBufferSize / sizeof(int)];

            shaderParams[0] = stride;
            shaderParams[1] = newStride;
            shaderParams[2] = size;
            shaderParams[3] = srcOffset;

            using ScopedTemporaryBuffer buffer = _renderer.BufferManager.ReserveOrCreate(cbs, ParamsBufferSize);
            buffer.Holder.SetDataUnchecked<int>(buffer.Offset, shaderParams);
            _pipeline.SetUniformBuffers([new BufferAssignment(0, buffer.Range)]);

            Span<Auto<DisposableBuffer>> sbRanges = new Auto<DisposableBuffer>[2];

            sbRanges[0] = srcBuffer;
            sbRanges[1] = dstBuffer;
            _pipeline.SetStorageBuffers(1, sbRanges);

            _pipeline.SetProgram(_programStrideChange);
            _pipeline.DispatchCompute(1 + elems / ConvertElementsPerWorkgroup, 1, 1, "Change Stride");

            // Restore previous state
            _pipeline.SwapState(null);
        }

        public unsafe void ConvertD32S8ToD24S8(CommandBufferScoped cbs, BufferHolder src, Auto<DisposableBuffer> dstBuffer, int pixelCount, int dstOffset)
        {
            int inSize = pixelCount * 2 * sizeof(int);

            Auto<DisposableBuffer> srcBuffer = src.GetBuffer();

            const int ParamsBufferSize = sizeof(int) * 2;

            // Save current state
            _pipeline.SwapState(_helperShaderState);

            Span<int> shaderParams = stackalloc int[2];

            shaderParams[0] = pixelCount;
            shaderParams[1] = dstOffset;

            using ScopedTemporaryBuffer buffer = _renderer.BufferManager.ReserveOrCreate(cbs, ParamsBufferSize);
            buffer.Holder.SetDataUnchecked<int>(buffer.Offset, shaderParams);
            _pipeline.SetUniformBuffers([new BufferAssignment(0, buffer.Range)]);

            Span<Auto<DisposableBuffer>> sbRanges = new Auto<DisposableBuffer>[2];

            sbRanges[0] = srcBuffer;
            sbRanges[1] = dstBuffer;
            _pipeline.SetStorageBuffers(1, sbRanges);

            _pipeline.SetProgram(_programConvertD32S8ToD24S8);
            _pipeline.DispatchCompute(1 + inSize / ConvertElementsPerWorkgroup, 1, 1, "D32S8 to D24S8 Conversion");

            // Restore previous state
            _pipeline.SwapState(null);
        }

        public void ConvertIndexBuffer(
            CommandBufferScoped cbs,
            BufferHolder src,
            BufferHolder dst,
            IndexBufferPattern pattern,
            int indexSize,
            int srcOffset,
            int indexCount)
        {
            // TODO: Support conversion with primitive restart enabled.

            int primitiveCount = pattern.GetPrimitiveCount(indexCount);
            int outputIndexSize = 4;

            Auto<DisposableBuffer> srcBuffer = src.GetBuffer();
            Auto<DisposableBuffer> dstBuffer = dst.GetBuffer();

            const int ParamsBufferSize = 16 * sizeof(int);

            // Save current state
            _pipeline.SwapState(_helperShaderState);

            Span<int> shaderParams = stackalloc int[ParamsBufferSize / sizeof(int)];

            shaderParams[8] = pattern.PrimitiveVertices;
            shaderParams[9] = pattern.PrimitiveVerticesOut;
            shaderParams[10] = indexSize;
            shaderParams[11] = outputIndexSize;
            shaderParams[12] = pattern.BaseIndex;
            shaderParams[13] = pattern.IndexStride;
            shaderParams[14] = srcOffset;
            shaderParams[15] = primitiveCount;

            pattern.OffsetIndex.CopyTo(shaderParams[..pattern.OffsetIndex.Length]);

            using ScopedTemporaryBuffer patternScoped = _renderer.BufferManager.ReserveOrCreate(cbs, ParamsBufferSize);
            patternScoped.Holder.SetDataUnchecked<int>(patternScoped.Offset, shaderParams);

            Span<Auto<DisposableBuffer>> sbRanges = new Auto<DisposableBuffer>[2];

            sbRanges[0] = srcBuffer;
            sbRanges[1] = dstBuffer;
            _pipeline.SetStorageBuffers(1, sbRanges);
            _pipeline.SetStorageBuffers([new BufferAssignment(3, patternScoped.Range)]);

            _pipeline.SetProgram(_programConvertIndexBuffer);
            _pipeline.DispatchCompute(BitUtils.DivRoundUp(primitiveCount, 16), 1, 1, "Convert Index Buffer");

            // Restore previous state
            _pipeline.SwapState(null);
        }

        public unsafe void ClearColor(
            int index,
            ReadOnlySpan<float> clearColor,
            uint componentMask,
            int dstWidth,
            int dstHeight,
            Format format)
        {
            // Keep original scissor
            DirtyFlags clearFlags = DirtyFlags.All & (~DirtyFlags.Scissors);

            // Save current state
            EncoderState originalState = _pipeline.SwapState(_helperShaderState, clearFlags, false);

            // Inherit some state without fully recreating render pipeline.
            RenderTargetCopy save = _helperShaderState.InheritForClear(originalState, false, index);

            const int ClearColorBufferSize = 16;

            // TODO: Flush

            using ScopedTemporaryBuffer buffer = _renderer.BufferManager.ReserveOrCreate(_pipeline.Cbs, ClearColorBufferSize);
            buffer.Holder.SetDataUnchecked(buffer.Offset, clearColor);
            _pipeline.SetUniformBuffers([new BufferAssignment(0, buffer.Range)]);

            Span<Viewport> viewports = stackalloc Viewport[16];

            // TODO: Set exact viewport!
            viewports[0] = new Viewport(
                new Rectangle<float>(0, 0, dstWidth, dstHeight),
                ViewportSwizzle.PositiveX,
                ViewportSwizzle.PositiveY,
                ViewportSwizzle.PositiveZ,
                ViewportSwizzle.PositiveW,
                0f,
                1f);

            Span<uint> componentMasks = stackalloc uint[index + 1];
            componentMasks[index] = componentMask;

            string debugGroupName = "Clear Color ";

            if (format.IsSint())
            {
                debugGroupName += "Int";
                _pipeline.SetProgram(_programsColorClearI[index]);
            }
            else if (format.IsUint())
            {
                debugGroupName += "UInt";
                _pipeline.SetProgram(_programsColorClearU[index]);
            }
            else
            {
                debugGroupName += "Float";
                _pipeline.SetProgram(_programsColorClearF[index]);
            }

            _pipeline.SetBlendState(index, new BlendDescriptor());
            _pipeline.SetFaceCulling(false, Face.Front);
            _pipeline.SetDepthTest(new DepthTestDescriptor(false, false, CompareOp.Always));
            _pipeline.SetRenderTargetColorMasks(componentMasks);
            _pipeline.SetViewports(viewports);
            _pipeline.SetPrimitiveTopology(PrimitiveTopology.TriangleStrip);
            _pipeline.Draw(4, 1, 0, 0, debugGroupName);

            // Restore previous state
            _pipeline.SwapState(null, clearFlags, false);

            _helperShaderState.Restore(save);
        }

        public unsafe void ClearDepthStencil(
            float depthValue,
            bool depthMask,
            int stencilValue,
            int stencilMask,
            int dstWidth,
            int dstHeight)
        {
            // Keep original scissor
            DirtyFlags clearFlags = DirtyFlags.All & (~DirtyFlags.Scissors);
            MTLScissorRect[] helperScissors = _helperShaderState.Scissors;

            // Save current state
            EncoderState originalState = _pipeline.SwapState(_helperShaderState, clearFlags, false);

            // Inherit some state without fully recreating render pipeline.
            RenderTargetCopy save = _helperShaderState.InheritForClear(originalState, true);

            const int ClearDepthBufferSize = 16;

            using ScopedTemporaryBuffer buffer = _renderer.BufferManager.ReserveOrCreate(_pipeline.Cbs, ClearDepthBufferSize);
            buffer.Holder.SetDataUnchecked(buffer.Offset, new ReadOnlySpan<float>(ref depthValue));
            _pipeline.SetUniformBuffers([new BufferAssignment(0, buffer.Range)]);

            Span<Viewport> viewports = stackalloc Viewport[1];

            viewports[0] = new Viewport(
                new Rectangle<float>(0, 0, dstWidth, dstHeight),
                ViewportSwizzle.PositiveX,
                ViewportSwizzle.PositiveY,
                ViewportSwizzle.PositiveZ,
                ViewportSwizzle.PositiveW,
                0f,
                1f);

            _pipeline.SetProgram(_programDepthStencilClear);
            _pipeline.SetFaceCulling(false, Face.Front);
            _pipeline.SetPrimitiveTopology(PrimitiveTopology.TriangleStrip);
            _pipeline.SetViewports(viewports);
            _pipeline.SetDepthTest(new DepthTestDescriptor(true, depthMask, CompareOp.Always));
            _pipeline.SetStencilTest(CreateStencilTestDescriptor(stencilMask != 0, stencilValue, 0xFF, stencilMask));
            _pipeline.Draw(4, 1, 0, 0, "Clear Depth Stencil");

            // Cleanup
            _pipeline.SetDepthTest(new DepthTestDescriptor(false, false, CompareOp.Always));
            _pipeline.SetStencilTest(CreateStencilTestDescriptor(false));

            // Restore previous state
            _pipeline.SwapState(null, clearFlags, false);

            _helperShaderState.Restore(save);
        }

        private static StencilTestDescriptor CreateStencilTestDescriptor(
            bool enabled,
            int refValue = 0,
            int compareMask = 0xff,
            int writeMask = 0xff)
        {
            return new StencilTestDescriptor(
                enabled,
                CompareOp.Always,
                StencilOp.Replace,
                StencilOp.Replace,
                StencilOp.Replace,
                refValue,
                compareMask,
                writeMask,
                CompareOp.Always,
                StencilOp.Replace,
                StencilOp.Replace,
                StencilOp.Replace,
                refValue,
                compareMask,
                writeMask);
        }

        public void Dispose()
        {
            _programColorBlitF.Dispose();
            _programColorBlitI.Dispose();
            _programColorBlitU.Dispose();
            _programColorBlitMsF.Dispose();
            _programColorBlitMsI.Dispose();
            _programColorBlitMsU.Dispose();

            foreach (IProgram programColorClear in _programsColorClearF)
            {
                programColorClear.Dispose();
            }

            foreach (IProgram programColorClear in _programsColorClearU)
            {
                programColorClear.Dispose();
            }

            foreach (IProgram programColorClear in _programsColorClearI)
            {
                programColorClear.Dispose();
            }

            _programDepthStencilClear.Dispose();
            _pipeline.Dispose();
            _samplerLinear.Dispose();
            _samplerNearest.Dispose();
        }
    }
}

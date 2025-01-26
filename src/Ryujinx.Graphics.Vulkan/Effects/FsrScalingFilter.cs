using Ryujinx.Common;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Shader;
using Ryujinx.Graphics.Shader.Translation;
using Silk.NET.Vulkan;
using System;
using Extent2D = Ryujinx.Graphics.GAL.Extents2D;
using Format = Silk.NET.Vulkan.Format;
using SamplerCreateInfo = Ryujinx.Graphics.GAL.SamplerCreateInfo;

namespace Ryujinx.Graphics.Vulkan.Effects
{
    internal class FsrScalingFilter : IScalingFilter
    {
        private readonly VulkanRenderer _renderer;
        private PipelineHelperShader _pipeline;
        private ISampler _sampler;
        private ShaderCollection _scalingProgram;
        private ShaderCollection _sharpeningProgram;
        private float _sharpeningLevel = 1;
        private Device _device;
        private TextureView _intermediaryTexture;

        public float Level
        {
            get => _sharpeningLevel;
            set
            {
                _sharpeningLevel = MathF.Max(0.01f, value);
            }
        }

        public FsrScalingFilter(VulkanRenderer renderer, Device device)
        {
            _device = device;
            _renderer = renderer;

            Initialize();
        }

        public void Dispose()
        {
            _pipeline.Dispose();
            _scalingProgram.Dispose();
            _sharpeningProgram.Dispose();
            _sampler.Dispose();
            _intermediaryTexture?.Dispose();
        }

        public void Initialize()
        {
            _pipeline = new PipelineHelperShader(_renderer, _device);

            _pipeline.Initialize();

            byte[] scalingShader = EmbeddedResources.Read("Ryujinx.Graphics.Vulkan/Effects/Shaders/FsrScaling.spv");
            byte[] sharpeningShader = EmbeddedResources.Read("Ryujinx.Graphics.Vulkan/Effects/Shaders/FsrSharpening.spv");

            ResourceLayout scalingResourceLayout = new ResourceLayoutBuilder()
                .Add(ResourceStages.Compute, ResourceType.UniformBuffer, 2)
                .Add(ResourceStages.Compute, ResourceType.TextureAndSampler, 1)
                .Add(ResourceStages.Compute, ResourceType.Image, 0, true).Build();

            ResourceLayout sharpeningResourceLayout = new ResourceLayoutBuilder()
                .Add(ResourceStages.Compute, ResourceType.UniformBuffer, 2)
                .Add(ResourceStages.Compute, ResourceType.UniformBuffer, 3)
                .Add(ResourceStages.Compute, ResourceType.UniformBuffer, 4)
                .Add(ResourceStages.Compute, ResourceType.TextureAndSampler, 1)
                .Add(ResourceStages.Compute, ResourceType.Image, 0, true).Build();

            _sampler = _renderer.CreateSampler(SamplerCreateInfo.Create(MinFilter.Linear, MagFilter.Linear));

            _scalingProgram = _renderer.CreateProgramWithMinimalLayout([
                new ShaderSource(scalingShader, ShaderStage.Compute, TargetLanguage.Spirv)
            ], scalingResourceLayout);

            _sharpeningProgram = _renderer.CreateProgramWithMinimalLayout([
                new ShaderSource(sharpeningShader, ShaderStage.Compute, TargetLanguage.Spirv)
            ], sharpeningResourceLayout);
        }

        public void Run(
            TextureView view,
            CommandBufferScoped cbs,
            Auto<DisposableImageView> destinationTexture,
            Format format,
            int width,
            int height,
            Extent2D source,
            Extent2D destination)
        {
            if (_intermediaryTexture == null
                || _intermediaryTexture.Info.Width != width
                || _intermediaryTexture.Info.Height != height
                || !_intermediaryTexture.Info.Equals(view.Info))
            {
                TextureCreateInfo originalInfo = view.Info;

                TextureCreateInfo info = new(
                    width,
                    height,
                    originalInfo.Depth,
                    originalInfo.Levels,
                    originalInfo.Samples,
                    originalInfo.BlockWidth,
                    originalInfo.BlockHeight,
                    originalInfo.BytesPerPixel,
                    originalInfo.Format,
                    originalInfo.DepthStencilMode,
                    originalInfo.Target,
                    originalInfo.SwizzleR,
                    originalInfo.SwizzleG,
                    originalInfo.SwizzleB,
                    originalInfo.SwizzleA);
                _intermediaryTexture?.Dispose();
                _intermediaryTexture = _renderer.CreateTexture(info) as TextureView;
            }

            _pipeline.SetCommandBuffer(cbs);
            _pipeline.SetProgram(_scalingProgram);
            _pipeline.SetTextureAndSampler(ShaderStage.Compute, 1, view, _sampler);

            float srcWidth = Math.Abs(source.X2 - source.X1);
            float srcHeight = Math.Abs(source.Y2 - source.Y1);
            float scaleX = srcWidth / view.Width;
            float scaleY = srcHeight / view.Height;

            ReadOnlySpan<float> dimensionsBuffer =
            [
                source.X1,
                source.X2,
                source.Y1,
                source.Y2,
                destination.X1,
                destination.X2,
                destination.Y1,
                destination.Y2,
                scaleX,
                scaleY
            ];

            int rangeSize = dimensionsBuffer.Length * sizeof(float);
            using ScopedTemporaryBuffer buffer = _renderer.BufferManager.ReserveOrCreate(_renderer, cbs, rangeSize);
            buffer.Holder.SetDataUnchecked(buffer.Offset, dimensionsBuffer);

            ReadOnlySpan<float> sharpeningBufferData = [1.5f - (Level * 0.01f * 1.5f)];
            using ScopedTemporaryBuffer sharpeningBuffer = _renderer.BufferManager.ReserveOrCreate(_renderer, cbs, sizeof(float));
            sharpeningBuffer.Holder.SetDataUnchecked(sharpeningBuffer.Offset, sharpeningBufferData);

            int threadGroupWorkRegionDim = 16;
            int dispatchX = (width + (threadGroupWorkRegionDim - 1)) / threadGroupWorkRegionDim;
            int dispatchY = (height + (threadGroupWorkRegionDim - 1)) / threadGroupWorkRegionDim;

            _pipeline.SetUniformBuffers([new BufferAssignment(2, buffer.Range)]);
            _pipeline.SetImage(ShaderStage.Compute, 0, _intermediaryTexture.GetView(FormatTable.ConvertRgba8SrgbToUnorm(view.Info.Format)));
            _pipeline.DispatchCompute(dispatchX, dispatchY, 1);
            _pipeline.ComputeBarrier();

            // Sharpening pass
            _pipeline.SetProgram(_sharpeningProgram);
            _pipeline.SetTextureAndSampler(ShaderStage.Compute, 1, _intermediaryTexture, _sampler);
            _pipeline.SetUniformBuffers([new BufferAssignment(4, sharpeningBuffer.Range)]);
            _pipeline.SetImage(0, destinationTexture);
            _pipeline.DispatchCompute(dispatchX, dispatchY, 1);
            _pipeline.ComputeBarrier();

            _pipeline.Finish();
        }
    }
}

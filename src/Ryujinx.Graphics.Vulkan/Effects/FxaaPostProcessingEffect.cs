using Ryujinx.Common;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Shader;
using Ryujinx.Graphics.Shader.Translation;
using Silk.NET.Vulkan;
using System;
using SamplerCreateInfo = Ryujinx.Graphics.GAL.SamplerCreateInfo;

namespace Ryujinx.Graphics.Vulkan.Effects
{
    internal class FxaaPostProcessingEffect : IPostProcessingEffect
    {
        private readonly VulkanRenderer _renderer;
        private ISampler _samplerLinear;
        private ShaderCollection _shaderProgram;

        private readonly PipelineHelperShader _pipeline;
        private TextureView _texture;

        public FxaaPostProcessingEffect(VulkanRenderer renderer, Device device)
        {
            _renderer = renderer;
            _pipeline = new PipelineHelperShader(renderer, device);

            Initialize();
        }

        public void Dispose()
        {
            _shaderProgram.Dispose();
            _pipeline.Dispose();
            _samplerLinear.Dispose();
            _texture?.Dispose();
        }

        private void Initialize()
        {
            _pipeline.Initialize();

            byte[] shader = EmbeddedResources.Read("Ryujinx.Graphics.Vulkan/Effects/Shaders/Fxaa.spv");

            ResourceLayout resourceLayout = new ResourceLayoutBuilder()
                .Add(ResourceStages.Compute, ResourceType.UniformBuffer, 2)
                .Add(ResourceStages.Compute, ResourceType.TextureAndSampler, 1)
                .Add(ResourceStages.Compute, ResourceType.Image, 0, true).Build();

            _samplerLinear = _renderer.CreateSampler(SamplerCreateInfo.Create(MinFilter.Linear, MagFilter.Linear));

            _shaderProgram = _renderer.CreateProgramWithMinimalLayout([
                new ShaderSource(shader, ShaderStage.Compute, TargetLanguage.Spirv)
            ], resourceLayout);
        }

        public TextureView Run(TextureView view, CommandBufferScoped cbs, int width, int height)
        {
            if (_texture == null || _texture.Width != view.Width || _texture.Height != view.Height)
            {
                _texture?.Dispose();
                _texture = _renderer.CreateTexture(view.Info) as TextureView;
            }

            _pipeline.SetCommandBuffer(cbs);
            _pipeline.SetProgram(_shaderProgram);
            _pipeline.SetTextureAndSampler(ShaderStage.Compute, 1, view, _samplerLinear);

            ReadOnlySpan<float> resolutionBuffer = [view.Width, view.Height];
            int rangeSize = resolutionBuffer.Length * sizeof(float);
            using ScopedTemporaryBuffer buffer = _renderer.BufferManager.ReserveOrCreate(_renderer, cbs, rangeSize);

            buffer.Holder.SetDataUnchecked(buffer.Offset, resolutionBuffer);

            _pipeline.SetUniformBuffers([new BufferAssignment(2, buffer.Range)]);

            int dispatchX = BitUtils.DivRoundUp(view.Width, IPostProcessingEffect.LocalGroupSize);
            int dispatchY = BitUtils.DivRoundUp(view.Height, IPostProcessingEffect.LocalGroupSize);

            _pipeline.SetImage(ShaderStage.Compute, 0, _texture.GetView(FormatTable.ConvertRgba8SrgbToUnorm(view.Info.Format)));
            _pipeline.DispatchCompute(dispatchX, dispatchY, 1);

            _pipeline.ComputeBarrier();

            _pipeline.Finish();

            return _texture;
        }
    }
}

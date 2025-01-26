using OpenTK.Graphics.OpenGL;
using Ryujinx.Common;
using Ryujinx.Common.Memory;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.OpenGL.Image;
using System;

namespace Ryujinx.Graphics.OpenGL.Effects.Smaa
{
    internal partial class SmaaPostProcessingEffect : IPostProcessingEffect
    {
        public const int AreaWidth = 160;
        public const int AreaHeight = 560;
        public const int SearchWidth = 64;
        public const int SearchHeight = 16;

        private readonly OpenGLRenderer _renderer;
        private TextureStorage _outputTexture;
        private TextureStorage _searchTexture;
        private TextureStorage _areaTexture;
        private int[] _edgeShaderPrograms;
        private int[] _blendShaderPrograms;
        private int[] _neighbourShaderPrograms;
        private TextureStorage _edgeOutputTexture;
        private TextureStorage _blendOutputTexture;
        private readonly string[] _qualities;
        private int _inputUniform;
        private int _outputUniform;
        private int _samplerAreaUniform;
        private int _samplerSearchUniform;
        private int _samplerBlendUniform;
        private int _resolutionUniform;
        private int _quality = 1;

        public int Quality
        {
            get => _quality;
            set
            {
                _quality = Math.Clamp(value, 0, _qualities.Length - 1);
            }
        }
        public SmaaPostProcessingEffect(OpenGLRenderer renderer, int quality)
        {
            _renderer = renderer;

            _edgeShaderPrograms = [];
            _blendShaderPrograms = [];
            _neighbourShaderPrograms = [];

            _qualities = ["SMAA_PRESET_LOW", "SMAA_PRESET_MEDIUM", "SMAA_PRESET_HIGH", "SMAA_PRESET_ULTRA"];

            Quality = quality;

            Initialize();
        }

        public void Dispose()
        {
            _searchTexture?.Dispose();
            _areaTexture?.Dispose();
            _outputTexture?.Dispose();
            _edgeOutputTexture?.Dispose();
            _blendOutputTexture?.Dispose();

            DeleteShaders();
        }

        private void DeleteShaders()
        {
            for (int i = 0; i < _edgeShaderPrograms.Length; i++)
            {
                GL.DeleteProgram(_edgeShaderPrograms[i]);
                GL.DeleteProgram(_blendShaderPrograms[i]);
                GL.DeleteProgram(_neighbourShaderPrograms[i]);
            }
        }

        private unsafe void RecreateShaders(int width, int height)
        {
            string baseShader = EmbeddedResources.ReadAllText("Ryujinx.Graphics.OpenGL/Effects/Shaders/smaa.hlsl");
            string pixelSizeDefine = $"#define SMAA_RT_METRICS float4(1.0 / {width}.0, 1.0 / {height}.0, {width}, {height}) \n";

            _edgeShaderPrograms = new int[_qualities.Length];
            _blendShaderPrograms = new int[_qualities.Length];
            _neighbourShaderPrograms = new int[_qualities.Length];

            for (int i = 0; i < +_edgeShaderPrograms.Length; i++)
            {
                string presets = $"#version 430 core \n#define {_qualities[i]} 1 \n{pixelSizeDefine}#define SMAA_GLSL_4 1 \nlayout (local_size_x = 16, local_size_y = 16) in;\n{baseShader}";

                string edgeShaderData = EmbeddedResources.ReadAllText("Ryujinx.Graphics.OpenGL/Effects/Shaders/smaa_edge.glsl");
                string blendShaderData = EmbeddedResources.ReadAllText("Ryujinx.Graphics.OpenGL/Effects/Shaders/smaa_blend.glsl");
                string neighbourShaderData = EmbeddedResources.ReadAllText("Ryujinx.Graphics.OpenGL/Effects/Shaders/smaa_neighbour.glsl");

                string[] shaders = [presets, edgeShaderData];
                int edgeProgram = ShaderHelper.CompileProgram(shaders, ShaderType.ComputeShader);

                shaders[1] = blendShaderData;
                int blendProgram = ShaderHelper.CompileProgram(shaders, ShaderType.ComputeShader);

                shaders[1] = neighbourShaderData;
                int neighbourProgram = ShaderHelper.CompileProgram(shaders, ShaderType.ComputeShader);

                _edgeShaderPrograms[i] = edgeProgram;
                _blendShaderPrograms[i] = blendProgram;
                _neighbourShaderPrograms[i] = neighbourProgram;
            }

            _inputUniform = GL.GetUniformLocation(_edgeShaderPrograms[0], "inputTexture");
            _outputUniform = GL.GetUniformLocation(_edgeShaderPrograms[0], "imgOutput");
            _samplerAreaUniform = GL.GetUniformLocation(_blendShaderPrograms[0], "samplerArea");
            _samplerSearchUniform = GL.GetUniformLocation(_blendShaderPrograms[0], "samplerSearch");
            _samplerBlendUniform = GL.GetUniformLocation(_neighbourShaderPrograms[0], "samplerBlend");
            _resolutionUniform = GL.GetUniformLocation(_edgeShaderPrograms[0], "invResolution");
        }

        private void Initialize()
        {
            TextureCreateInfo areaInfo = new(AreaWidth,
                AreaHeight,
                1,
                1,
                1,
                1,
                1,
                1,
                Format.R8G8Unorm,
                DepthStencilMode.Depth,
                Target.Texture2D,
                SwizzleComponent.Red,
                SwizzleComponent.Green,
                SwizzleComponent.Blue,
                SwizzleComponent.Alpha);

            TextureCreateInfo searchInfo = new(SearchWidth,
                SearchHeight,
                1,
                1,
                1,
                1,
                1,
                1,
                Format.R8Unorm,
                DepthStencilMode.Depth,
                Target.Texture2D,
                SwizzleComponent.Red,
                SwizzleComponent.Green,
                SwizzleComponent.Blue,
                SwizzleComponent.Alpha);

            _areaTexture = new TextureStorage(_renderer, areaInfo);
            _searchTexture = new TextureStorage(_renderer, searchInfo);

            MemoryOwner<byte> areaTexture = EmbeddedResources.ReadFileToRentedMemory("Ryujinx.Graphics.OpenGL/Effects/Textures/SmaaAreaTexture.bin");
            MemoryOwner<byte> searchTexture = EmbeddedResources.ReadFileToRentedMemory("Ryujinx.Graphics.OpenGL/Effects/Textures/SmaaSearchTexture.bin");

            ITexture areaView = _areaTexture.CreateDefaultView();
            ITexture searchView = _searchTexture.CreateDefaultView();

            areaView.SetData(areaTexture);
            searchView.SetData(searchTexture);
        }

        public TextureView Run(TextureView view, int width, int height)
        {
            if (_outputTexture == null || _outputTexture.Info.Width != view.Width || _outputTexture.Info.Height != view.Height)
            {
                _outputTexture?.Dispose();
                _outputTexture = new TextureStorage(_renderer, view.Info);
                _outputTexture.CreateDefaultView();
                _edgeOutputTexture = new TextureStorage(_renderer, view.Info);
                _edgeOutputTexture.CreateDefaultView();
                _blendOutputTexture = new TextureStorage(_renderer, view.Info);
                _blendOutputTexture.CreateDefaultView();

                DeleteShaders();

                RecreateShaders(view.Width, view.Height);
            }

            TextureView textureView = _outputTexture.CreateView(view.Info, 0, 0) as TextureView;
            TextureView edgeOutput = _edgeOutputTexture.DefaultView as TextureView;
            TextureView blendOutput = _blendOutputTexture.DefaultView as TextureView;
            TextureView areaTexture = _areaTexture.DefaultView as TextureView;
            TextureView searchTexture = _searchTexture.DefaultView as TextureView;

            int previousFramebuffer = GL.GetInteger(GetPName.FramebufferBinding);
            int previousUnit = GL.GetInteger(GetPName.ActiveTexture);
            GL.ActiveTexture(TextureUnit.Texture0);
            int previousTextureBinding0 = GL.GetInteger(GetPName.TextureBinding2D);
            GL.ActiveTexture(TextureUnit.Texture1);
            int previousTextureBinding1 = GL.GetInteger(GetPName.TextureBinding2D);
            GL.ActiveTexture(TextureUnit.Texture2);
            int previousTextureBinding2 = GL.GetInteger(GetPName.TextureBinding2D);

            Framebuffer framebuffer = new();
            framebuffer.Bind();
            framebuffer.AttachColor(0, edgeOutput);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.ClearColor(0, 0, 0, 0);
            framebuffer.AttachColor(0, blendOutput);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.ClearColor(0, 0, 0, 0);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, previousFramebuffer);

            framebuffer.Dispose();

            int dispatchX = BitUtils.DivRoundUp(view.Width, IPostProcessingEffect.LocalGroupSize);
            int dispatchY = BitUtils.DivRoundUp(view.Height, IPostProcessingEffect.LocalGroupSize);

            int previousProgram = GL.GetInteger(GetPName.CurrentProgram);
            GL.BindImageTexture(0, edgeOutput.Handle, 0, false, 0, TextureAccess.ReadWrite, SizedInternalFormat.Rgba8);
            GL.UseProgram(_edgeShaderPrograms[Quality]);
            view.Bind(0);
            GL.Uniform1(_inputUniform, 0);
            GL.Uniform1(_outputUniform, 0);
            GL.Uniform2(_resolutionUniform, (float)view.Width, (float)view.Height);
            GL.DispatchCompute(dispatchX, dispatchY, 1);
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);

            GL.BindImageTexture(0, blendOutput.Handle, 0, false, 0, TextureAccess.ReadWrite, SizedInternalFormat.Rgba8);
            GL.UseProgram(_blendShaderPrograms[Quality]);
            edgeOutput.Bind(0);
            areaTexture.Bind(1);
            searchTexture.Bind(2);
            GL.Uniform1(_inputUniform, 0);
            GL.Uniform1(_outputUniform, 0);
            GL.Uniform1(_samplerAreaUniform, 1);
            GL.Uniform1(_samplerSearchUniform, 2);
            GL.Uniform2(_resolutionUniform, (float)view.Width, (float)view.Height);
            GL.DispatchCompute(dispatchX, dispatchY, 1);
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);

            GL.BindImageTexture(0, textureView.Handle, 0, false, 0, TextureAccess.ReadWrite, SizedInternalFormat.Rgba8);
            GL.UseProgram(_neighbourShaderPrograms[Quality]);
            view.Bind(0);
            blendOutput.Bind(1);
            GL.Uniform1(_inputUniform, 0);
            GL.Uniform1(_outputUniform, 0);
            GL.Uniform1(_samplerBlendUniform, 1);
            GL.Uniform2(_resolutionUniform, (float)view.Width, (float)view.Height);
            GL.DispatchCompute(dispatchX, dispatchY, 1);
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);

            (_renderer.Pipeline as Pipeline).RestoreImages1And2();

            GL.UseProgram(previousProgram);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, previousTextureBinding0);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, previousTextureBinding1);
            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, previousTextureBinding2);

            GL.ActiveTexture((TextureUnit)previousUnit);

            return textureView;
        }
    }
}

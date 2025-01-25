using OpenTK.Graphics.OpenGL;
using Ryujinx.Common;
using Ryujinx.Graphics.OpenGL.Image;

namespace Ryujinx.Graphics.OpenGL.Effects
{
    internal class FxaaPostProcessingEffect : IPostProcessingEffect
    {
        private readonly OpenGLRenderer _renderer;
        private int _resolutionUniform;
        private int _inputUniform;
        private int _outputUniform;
        private int _shaderProgram;
        private TextureStorage _textureStorage;

        public FxaaPostProcessingEffect(OpenGLRenderer renderer)
        {
            Initialize();

            _renderer = renderer;
        }

        public void Dispose()
        {
            if (_shaderProgram != 0)
            {
                GL.DeleteProgram(_shaderProgram);
                _textureStorage?.Dispose();
            }
        }

        private void Initialize()
        {
            _shaderProgram = ShaderHelper.CompileProgram(EmbeddedResources.ReadAllText("Ryujinx.Graphics.OpenGL/Effects/Shaders/fxaa.glsl"), ShaderType.ComputeShader);

            _resolutionUniform = GL.GetUniformLocation(_shaderProgram, "invResolution");
            _inputUniform = GL.GetUniformLocation(_shaderProgram, "inputTexture");
            _outputUniform = GL.GetUniformLocation(_shaderProgram, "imgOutput");
        }

        public TextureView Run(TextureView view, int width, int height)
        {
            if (_textureStorage == null || _textureStorage.Info.Width != view.Width || _textureStorage.Info.Height != view.Height)
            {
                _textureStorage?.Dispose();
                _textureStorage = new TextureStorage(_renderer, view.Info);
                _textureStorage.CreateDefaultView();
            }

            TextureView textureView = _textureStorage.CreateView(view.Info, 0, 0) as TextureView;

            int previousProgram = GL.GetInteger(GetPName.CurrentProgram);
            int previousUnit = GL.GetInteger(GetPName.ActiveTexture);
            GL.ActiveTexture(TextureUnit.Texture0);
            int previousTextureBinding = GL.GetInteger(GetPName.TextureBinding2D);

            GL.BindImageTexture(0, textureView.Handle, 0, false, 0, TextureAccess.ReadWrite, SizedInternalFormat.Rgba8);
            GL.UseProgram(_shaderProgram);

            int dispatchX = BitUtils.DivRoundUp(view.Width, IPostProcessingEffect.LocalGroupSize);
            int dispatchY = BitUtils.DivRoundUp(view.Height, IPostProcessingEffect.LocalGroupSize);

            view.Bind(0);
            GL.Uniform1(_inputUniform, 0);
            GL.Uniform1(_outputUniform, 0);
            GL.Uniform2(_resolutionUniform, (float)view.Width, (float)view.Height);
            GL.DispatchCompute(dispatchX, dispatchY, 1);
            GL.UseProgram(previousProgram);
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);

            (_renderer.Pipeline as Pipeline).RestoreImages1And2();

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, previousTextureBinding);

            GL.ActiveTexture((TextureUnit)previousUnit);

            return textureView;
        }
    }
}

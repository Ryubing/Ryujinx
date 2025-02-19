using OpenTK.Graphics.OpenGL;
using Ryujinx.Common;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.OpenGL.Image;
using static Ryujinx.Graphics.OpenGL.Effects.ShaderHelper;

namespace Ryujinx.Graphics.OpenGL.Effects
{
    internal class AreaScalingFilter : IScalingFilter
    {
        private readonly OpenGLRenderer _renderer;
        private int _inputUniform;
        private int _outputUniform;
        private int _srcX0Uniform;
        private int _srcX1Uniform;
        private int _srcY0Uniform;
        private int _scalingShaderProgram;
        private int _srcY1Uniform;
        private int _dstX0Uniform;
        private int _dstX1Uniform;
        private int _dstY0Uniform;
        private int _dstY1Uniform;

        public float Level { get; set; }

        public AreaScalingFilter(OpenGLRenderer renderer)
        {
            Initialize();

            _renderer = renderer;
        }

        public void Dispose()
        {
            if (_scalingShaderProgram != 0)
            {
                GL.DeleteProgram(_scalingShaderProgram);
            }
        }

        private void Initialize()
        {
            string scalingShader = EmbeddedResources.ReadAllText("Ryujinx.Graphics.OpenGL/Effects/Shaders/area_scaling.glsl");

            _scalingShaderProgram = CompileProgram(scalingShader, ShaderType.ComputeShader);

            _inputUniform = GL.GetUniformLocation(_scalingShaderProgram, "Source");
            _outputUniform = GL.GetUniformLocation(_scalingShaderProgram, "imgOutput");

            _srcX0Uniform = GL.GetUniformLocation(_scalingShaderProgram, "srcX0");
            _srcX1Uniform = GL.GetUniformLocation(_scalingShaderProgram, "srcX1");
            _srcY0Uniform = GL.GetUniformLocation(_scalingShaderProgram, "srcY0");
            _srcY1Uniform = GL.GetUniformLocation(_scalingShaderProgram, "srcY1");
            _dstX0Uniform = GL.GetUniformLocation(_scalingShaderProgram, "dstX0");
            _dstX1Uniform = GL.GetUniformLocation(_scalingShaderProgram, "dstX1");
            _dstY0Uniform = GL.GetUniformLocation(_scalingShaderProgram, "dstY0");
            _dstY1Uniform = GL.GetUniformLocation(_scalingShaderProgram, "dstY1");
        }

        public void Run(
            TextureView view,
            TextureView destinationTexture,
            int width,
            int height,
            Extents2D source,
            Extents2D destination)
        {
            int previousProgram = GL.GetInteger(GetPName.CurrentProgram);
            int previousUnit = GL.GetInteger(GetPName.ActiveTexture);
            GL.ActiveTexture(TextureUnit.Texture0);
            int previousTextureBinding = GL.GetInteger(GetPName.TextureBinding2D);

            GL.BindImageTexture(0, destinationTexture.Handle, 0, false, 0, TextureAccess.ReadWrite, SizedInternalFormat.Rgba8);

            int threadGroupWorkRegionDim = 16;
            int dispatchX = (width + (threadGroupWorkRegionDim - 1)) / threadGroupWorkRegionDim;
            int dispatchY = (height + (threadGroupWorkRegionDim - 1)) / threadGroupWorkRegionDim;

            // Scaling pass
            GL.UseProgram(_scalingShaderProgram);
            view.Bind(0);
            GL.Uniform1(_inputUniform, 0);
            GL.Uniform1(_outputUniform, 0);
            GL.Uniform1(_srcX0Uniform, (float)source.X1);
            GL.Uniform1(_srcX1Uniform, (float)source.X2);
            GL.Uniform1(_srcY0Uniform, (float)source.Y1);
            GL.Uniform1(_srcY1Uniform, (float)source.Y2);
            GL.Uniform1(_dstX0Uniform, (float)destination.X1);
            GL.Uniform1(_dstX1Uniform, (float)destination.X2);
            GL.Uniform1(_dstY0Uniform, (float)destination.Y1);
            GL.Uniform1(_dstY1Uniform, (float)destination.Y2);
            GL.DispatchCompute(dispatchX, dispatchY, 1);

            GL.UseProgram(previousProgram);
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);

            (_renderer.Pipeline as Pipeline).RestoreImages1And2();

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, previousTextureBinding);

            GL.ActiveTexture((TextureUnit)previousUnit);
        }
    }
}

using OpenTK.Graphics.OpenGL;
using Ryujinx.Common.Logging;

namespace Ryujinx.Graphics.OpenGL.Effects
{
    internal static class ShaderHelper
    {
        public static int CompileProgram(string shaderCode, ShaderType shaderType)
        {
            return CompileProgram([shaderCode], shaderType);
        }

        public static int CompileProgram(string[] shaders, ShaderType shaderType)
        {
            int shader = GL.CreateShader(shaderType);
            GL.ShaderSource(shader, shaders.Length, shaders, (int[])null);
            GL.CompileShader(shader);

            GL.GetShader(shader, ShaderParameter.CompileStatus, out int isCompiled);
            if (isCompiled == 0)
            {
                string log = GL.GetShaderInfoLog(shader);
                Logger.Error?.Print(LogClass.Gpu, $"Failed to compile effect shader:\n\n{log}\n");
                GL.DeleteShader(shader);
                return 0;
            }

            int program = GL.CreateProgram();
            GL.AttachShader(program, shader);
            GL.LinkProgram(program);

            GL.DetachShader(program, shader);
            GL.DeleteShader(shader);

            return program;
        }
    }
}

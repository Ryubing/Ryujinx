using System;

namespace Ryujinx.Graphics.OpenGL
{
    public interface IOpenGLContext : IDisposable
    {
        void MakeCurrent();

        bool HasContext();
    }
}

using OpenTK;
using OpenTK.Graphics.OpenGL;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using Ryujinx.Graphics.OpenGL;
using Ryujinx.Input.HLE;
using System;
using static SDL2.SDL;

namespace Ryujinx.Headless
{
    class OpenGLWindow : WindowBase
    {
        private static void CheckResult(int result)
        {
            if (result < 0)
            {
                throw new InvalidOperationException($"SDL_GL function returned an error: {SDL_GetError()}");
            }
        }

        private static void SetupOpenGLAttributes(bool sharedContext, GraphicsDebugLevel debugLevel)
        {
            CheckResult(SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, 3));
            CheckResult(SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, 3));
            CheckResult(SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_COMPATIBILITY));
            CheckResult(SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_FLAGS, debugLevel != GraphicsDebugLevel.None ? (int)SDL_GLcontext.SDL_GL_CONTEXT_DEBUG_FLAG : 0));
            CheckResult(SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_SHARE_WITH_CURRENT_CONTEXT, sharedContext ? 1 : 0));

            CheckResult(SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_ACCELERATED_VISUAL, 1));
            CheckResult(SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_RED_SIZE, 8));
            CheckResult(SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_GREEN_SIZE, 8));
            CheckResult(SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_BLUE_SIZE, 8));
            CheckResult(SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_ALPHA_SIZE, 8));
            CheckResult(SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_DEPTH_SIZE, 16));
            CheckResult(SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_STENCIL_SIZE, 0));
            CheckResult(SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_DOUBLEBUFFER, 1));
            CheckResult(SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_STEREO, 0));
        }

        private class OpenToolkitBindingsContext : IBindingsContext
        {
            public nint GetProcAddress(string procName)
            {
                return SDL_GL_GetProcAddress(procName);
            }
        }

        private class SDL2OpenGLContext : IOpenGLContext
        {
            private readonly nint _context;
            private readonly nint _window;
            private readonly bool _shouldDisposeWindow;

            public SDL2OpenGLContext(nint context, nint window, bool shouldDisposeWindow = true)
            {
                _context = context;
                _window = window;
                _shouldDisposeWindow = shouldDisposeWindow;
            }

            public static SDL2OpenGLContext CreateBackgroundContext(SDL2OpenGLContext sharedContext)
            {
                sharedContext.MakeCurrent();

                // Ensure we share our contexts.
                SetupOpenGLAttributes(true, GraphicsDebugLevel.None);
                nint windowHandle = SDL_CreateWindow("Ryujinx background context window", 0, 0, 1, 1, SDL_WindowFlags.SDL_WINDOW_OPENGL | SDL_WindowFlags.SDL_WINDOW_HIDDEN);
                nint context = SDL_GL_CreateContext(windowHandle);

                GL.LoadBindings(new OpenToolkitBindingsContext());

                CheckResult(SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_SHARE_WITH_CURRENT_CONTEXT, 0));

                CheckResult(SDL_GL_MakeCurrent(windowHandle, nint.Zero));

                return new SDL2OpenGLContext(context, windowHandle);
            }

            public void MakeCurrent()
            {
                if (SDL_GL_GetCurrentContext() == _context || SDL_GL_GetCurrentWindow() == _window)
                {
                    return;
                }

                int res = SDL_GL_MakeCurrent(_window, _context);

                if (res != 0)
                {
                    string errorMessage = $"SDL_GL_CreateContext failed with error \"{SDL_GetError()}\"";

                    Logger.Error?.Print(LogClass.Application, errorMessage);

                    throw new Exception(errorMessage);
                }
            }

            public bool HasContext() => SDL_GL_GetCurrentContext() != nint.Zero;

            public void Dispose()
            {
                SDL_GL_DeleteContext(_context);

                if (_shouldDisposeWindow)
                {
                    SDL_DestroyWindow(_window);
                }
            }
        }
        
        private SDL2OpenGLContext _openGLContext;

        public OpenGLWindow(
            InputManager inputManager,
            GraphicsDebugLevel glLogLevel,
            AspectRatio aspectRatio,
            bool enableMouse,
            HideCursorMode hideCursorMode,
            bool ignoreControllerApplet)
            : base(inputManager, glLogLevel, aspectRatio, enableMouse, hideCursorMode, ignoreControllerApplet)
        {
        }

        public override SDL_WindowFlags WindowFlags => SDL_WindowFlags.SDL_WINDOW_OPENGL;

        protected override void InitializeWindowRenderer()
        {
            // Ensure to not share this context with other contexts before this point.
            SetupOpenGLAttributes(false, GlLogLevel);
            nint context = SDL_GL_CreateContext(WindowHandle);
            CheckResult(SDL_GL_SetSwapInterval(1));

            if (context == nint.Zero)
            {
                string errorMessage = $"SDL_GL_CreateContext failed with error \"{SDL_GetError()}\"";

                Logger.Error?.Print(LogClass.Application, errorMessage);

                throw new Exception(errorMessage);
            }

            // NOTE: The window handle needs to be disposed by the thread that created it and is handled separately.
            _openGLContext = new SDL2OpenGLContext(context, WindowHandle, false);

            // First take exclusivity on the OpenGL context.
            ((OpenGLRenderer)Renderer).InitializeBackgroundContext(SDL2OpenGLContext.CreateBackgroundContext(_openGLContext));

            _openGLContext.MakeCurrent();

            GL.ClearColor(0, 0, 0, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            SwapBuffers();

            if (IsExclusiveFullscreen)
            {
                Renderer?.Window.SetSize(ExclusiveFullscreenWidth, ExclusiveFullscreenHeight);
                MouseDriver.SetClientSize(ExclusiveFullscreenWidth, ExclusiveFullscreenHeight);
            }
            else if (IsFullscreen)
            {
                // NOTE: grabbing the main display's dimensions directly as OpenGL doesn't scale along like the VulkanWindow.
                if (SDL_GetDisplayBounds(DisplayId, out SDL_Rect displayBounds) < 0)
                {
                    Logger.Warning?.Print(LogClass.Application, $"Could not retrieve display bounds: {SDL_GetError()}");

                    // Fallback to defaults
                    displayBounds.w = DefaultWidth;
                    displayBounds.h = DefaultHeight;
                }

                Renderer?.Window.SetSize(displayBounds.w, displayBounds.h);
                MouseDriver.SetClientSize(displayBounds.w, displayBounds.h);
            }
            else
            {
                Renderer?.Window.SetSize(DefaultWidth, DefaultHeight);
                MouseDriver.SetClientSize(DefaultWidth, DefaultHeight);
            }
        }

        protected override void InitializeRenderer() { }

        protected override void FinalizeWindowRenderer()
        {
            // Try to bind the OpenGL context before calling the gpu disposal.
            _openGLContext.MakeCurrent();

            Device.DisposeGpu();

            // Unbind context and destroy everything
            CheckResult(SDL_GL_MakeCurrent(WindowHandle, nint.Zero));
            _openGLContext.Dispose();
        }

        protected override void SwapBuffers()
        {
            SDL_GL_SwapWindow(WindowHandle);
        }
    }
}

using Ryujinx.Common.Configuration;
using Ryujinx.Input.HLE;
using Ryujinx.SDL2.Common;
using SharpMetal.QuartzCore;
using System.Runtime.Versioning;
using static SDL2.SDL;

namespace Ryujinx.Headless
{
    [SupportedOSPlatform("macos")]
    class MetalWindow : WindowBase
    {
        private CAMetalLayer _caMetalLayer;

        public CAMetalLayer GetLayer()
        {
            return _caMetalLayer;
        }

        public MetalWindow(
            InputManager inputManager,
            GraphicsDebugLevel glLogLevel,
            AspectRatio aspectRatio,
            bool enableMouse,
            HideCursorMode hideCursorMode, 
            bool ignoreControllerApplet)
            : base(inputManager, glLogLevel, aspectRatio, enableMouse, hideCursorMode, ignoreControllerApplet) { }

        public override SDL_WindowFlags WindowFlags => SDL_WindowFlags.SDL_WINDOW_METAL;

        protected override void InitializeWindowRenderer()
        {
            void CreateLayer()
            {
                _caMetalLayer = new CAMetalLayer(SDL_Metal_GetLayer(SDL_Metal_CreateView(WindowHandle)));
            }

            SDL2Driver.MainThreadDispatcher?.Invoke(CreateLayer);
        }

        protected override void InitializeRenderer() { }

        protected override void FinalizeWindowRenderer() { }

        protected override void SwapBuffers() { }
    }
}

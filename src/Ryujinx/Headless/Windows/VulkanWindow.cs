using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using Ryujinx.Input.HLE;
using Ryujinx.SDL2.Common;
using System;
using System.Runtime.InteropServices;
using static SDL2.SDL;

namespace Ryujinx.Headless
{
    class VulkanWindow : WindowBase
    {
        public VulkanWindow(
            InputManager inputManager,
            GraphicsDebugLevel glLogLevel,
            AspectRatio aspectRatio,
            bool enableMouse,
            HideCursorMode hideCursorMode,
            bool ignoreControllerApplet)
            : base(inputManager, glLogLevel, aspectRatio, enableMouse, hideCursorMode, ignoreControllerApplet)
        {
        }

        public override SDL_WindowFlags WindowFlags => SDL_WindowFlags.SDL_WINDOW_VULKAN;

        protected override void InitializeWindowRenderer() { }

        protected override void InitializeRenderer()
        {
            if (IsExclusiveFullscreen)
            {
                Renderer?.Window.SetSize(ExclusiveFullscreenWidth, ExclusiveFullscreenHeight);
                MouseDriver.SetClientSize(ExclusiveFullscreenWidth, ExclusiveFullscreenHeight);
            }
            else
            {
                Renderer?.Window.SetSize(DefaultWidth, DefaultHeight);
                MouseDriver.SetClientSize(DefaultWidth, DefaultHeight);
            }
        }

        private static void BasicInvoke(Action action)
        {
            action();
        }

        public nint CreateWindowSurface(nint instance)
        {
            ulong surfaceHandle = 0;

            void CreateSurface()
            {
                if (SDL_Vulkan_CreateSurface(WindowHandle, instance, out surfaceHandle) == SDL_bool.SDL_FALSE)
                {
                    string errorMessage = $"SDL_Vulkan_CreateSurface failed with error \"{SDL_GetError()}\"";

                    Logger.Error?.Print(LogClass.Application, errorMessage);

                    throw new Exception(errorMessage);
                }
            }

            if (SDL2Driver.MainThreadDispatcher != null)
            {
                SDL2Driver.MainThreadDispatcher(CreateSurface);
            }
            else
            {
                CreateSurface();
            }

            return (nint)surfaceHandle;
        }

        public unsafe string[] GetRequiredInstanceExtensions()
        {
            if (SDL_Vulkan_GetInstanceExtensions(WindowHandle, out uint extensionsCount, nint.Zero) == SDL_bool.SDL_TRUE)
            {
                nint[] rawExtensions = new nint[(int)extensionsCount];
                string[] extensions = new string[(int)extensionsCount];

                fixed (nint* rawExtensionsPtr = rawExtensions)
                {
                    if (SDL_Vulkan_GetInstanceExtensions(WindowHandle, out extensionsCount, (nint)rawExtensionsPtr) == SDL_bool.SDL_TRUE)
                    {
                        for (int i = 0; i < extensions.Length; i++)
                        {
                            extensions[i] = Marshal.PtrToStringUTF8(rawExtensions[i]);
                        }

                        return extensions;
                    }
                }
            }

            string errorMessage = $"SDL_Vulkan_GetInstanceExtensions failed with error \"{SDL_GetError()}\"";

            Logger.Error?.Print(LogClass.Application, errorMessage);

            throw new Exception(errorMessage);
        }

        protected override void FinalizeWindowRenderer()
        {
            Device.DisposeGpu();
        }

        protected override void SwapBuffers() { }
    }
}

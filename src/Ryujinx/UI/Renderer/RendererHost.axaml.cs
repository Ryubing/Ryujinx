using Avalonia;
using Avalonia.Controls;
using Gommon;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using Ryujinx.UI.Common.Configuration;
using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Ava.UI.Renderer
{
    public partial class RendererHost : UserControl, IDisposable
    {
        public readonly EmbeddedWindow EmbeddedWindow;

        public event EventHandler<EventArgs> WindowCreated;
        public event Action<object, Size> BoundsChanged;

        public RendererHost()
        {
            InitializeComponent();

            EmbeddedWindow = ConfigurationState.Instance.Graphics.GraphicsBackend.Value switch
            {
                GraphicsBackend.OpenGl => new EmbeddedWindowOpenGL(),
                GraphicsBackend.Metal => new EmbeddedWindowMetal(),
                GraphicsBackend.Vulkan or GraphicsBackend.Auto => new EmbeddedWindowVulkan(),
                _ => throw new NotSupportedException()
            };

            Initialize();
        }

        public static readonly string[] KnownGreatMetalTitles =
        [
            "01006A800016E000", // Smash Ultimate
            "0100000000010000", // Super Mario Odyessy
            "01008C0016544000", // Sea of Stars
            "01005CA01580E000", // Persona 5
            "010028600EBDA000", // Mario 3D World
        ];

        public GraphicsBackend Backend =>
            EmbeddedWindow switch
            {
                EmbeddedWindowVulkan => GraphicsBackend.Vulkan,
                EmbeddedWindowOpenGL => GraphicsBackend.OpenGl,
                EmbeddedWindowMetal => GraphicsBackend.Metal,
                _ => throw new NotImplementedException()
            };

        public RendererHost(string titleId)
        {
            InitializeComponent();

            switch (ConfigurationState.Instance.Graphics.GraphicsBackend.Value)
            {
                case GraphicsBackend.Auto:
                    EmbeddedWindow = 
                        OperatingSystem.IsMacOS() && 
                        RuntimeInformation.ProcessArchitecture == Architecture.Arm64 && 
                        KnownGreatMetalTitles.ContainsIgnoreCase(titleId) 
                            ? new EmbeddedWindowMetal() 
                            : new EmbeddedWindowVulkan();
                    break;
                case GraphicsBackend.OpenGl:
                    EmbeddedWindow = new EmbeddedWindowOpenGL();
                    break;
                case GraphicsBackend.Metal:
                    EmbeddedWindow = new EmbeddedWindowMetal();
                    break;
                case GraphicsBackend.Vulkan: 
                    EmbeddedWindow = new EmbeddedWindowVulkan(); 
                    break;
            }
            
            string backendText = EmbeddedWindow switch
            {
                EmbeddedWindowVulkan => "Vulkan",
                EmbeddedWindowOpenGL => "OpenGL",
                EmbeddedWindowMetal => "Metal",
                _ => throw new NotImplementedException()
            };
                    
            Logger.Info?.PrintMsg(LogClass.Gpu, $"Backend ({ConfigurationState.Instance.Graphics.GraphicsBackend.Value}): {backendText}");

            Initialize();
        }
        
        
        private void Initialize()
        {
            EmbeddedWindow.WindowCreated += CurrentWindow_WindowCreated;
            EmbeddedWindow.BoundsChanged += CurrentWindow_BoundsChanged;

            Content = EmbeddedWindow;
        }

        public void Dispose()
        {
            if (EmbeddedWindow != null)
            {
                EmbeddedWindow.WindowCreated -= CurrentWindow_WindowCreated;
                EmbeddedWindow.BoundsChanged -= CurrentWindow_BoundsChanged;
            }

            GC.SuppressFinalize(this);
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            Dispose();
        }

        private void CurrentWindow_BoundsChanged(object sender, Size e)
        {
            BoundsChanged?.Invoke(sender, e);
        }

        private void CurrentWindow_WindowCreated(object sender, nint e)
        {
            WindowCreated?.Invoke(this, EventArgs.Empty);
        }
    }
}

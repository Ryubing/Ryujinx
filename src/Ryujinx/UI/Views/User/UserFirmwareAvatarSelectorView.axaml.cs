using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;
using Ryujinx.Ava.UI.Controls;
using Ryujinx.Ava.UI.Models;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.HLE.FileSystem;
using SkiaSharp;
using System.IO;

namespace Ryujinx.Ava.UI.Views.User
{
    public partial class UserFirmwareAvatarSelectorView : RyujinxControl<UserFirmwareAvatarSelectorViewModel>
    {
        private NavigationDialogHost _parent;
        private TempProfile _profile;

        public UserFirmwareAvatarSelectorView(ContentManager contentManager)
        {
            ContentManager = contentManager;

            InitializeComponent();
        }

        public UserFirmwareAvatarSelectorView()
        {
            InitializeComponent();

            AddHandler(Frame.NavigatedToEvent, (s, e) =>
            {
                NavigatedTo(e);
            }, RoutingStrategies.Direct);
        }

        private void NavigatedTo(NavigationEventArgs arg)
        {
            if (Program.PreviewerDetached)
            {
                if (arg.NavigationMode == NavigationMode.New)
                {
                    (_parent, _profile) = ((NavigationDialogHost, TempProfile))arg.Parameter;
                    ContentManager = _parent.ContentManager;
                    if (Program.PreviewerDetached)
                    {
                        ViewModel = new UserFirmwareAvatarSelectorViewModel();
                    }

                    DataContext = ViewModel;
                }
            }
        }

        public ContentManager ContentManager { get; private set; }

        private void GoBack(object sender, RoutedEventArgs e)
        {
            _parent.GoBack();
        }

        private void ChooseButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedImage != null)
            {
                using MemoryStream streamJpg = new();
                using SKBitmap bitmap = SKBitmap.Decode(ViewModel.SelectedImage);
                using SKBitmap newBitmap = new(bitmap.Width, bitmap.Height);

                using (SKCanvas canvas = new(newBitmap))
                {
                    canvas.Clear(new SKColor(
                        ViewModel.BackgroundColor.R,
                        ViewModel.BackgroundColor.G,
                        ViewModel.BackgroundColor.B,
                        ViewModel.BackgroundColor.A));
                    canvas.DrawBitmap(bitmap, 0, 0);
                }

                using (SKImage image = SKImage.FromBitmap(newBitmap))
                using (SKData dataJpeg = image.Encode(SKEncodedImageFormat.Jpeg, 100))
                {
                    dataJpeg.SaveTo(streamJpg);
                }

                _profile.Image = streamJpg.ToArray();

                _parent.GoBack();
            }
        }
    }
}

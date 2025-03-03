using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Controls;
using Ryujinx.Ava.UI.Models;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.HLE.FileSystem;
using SkiaSharp;
using System.Collections.Generic;
using System.IO;

namespace Ryujinx.Ava.UI.Views.User
{
    public partial class UserProfileImageSelectorView : RyujinxControl<UserProfileImageSelectorViewModel>
    {
        private ContentManager _contentManager;
        private NavigationDialogHost _parent;
        private TempProfile _profile;

        public UserProfileImageSelectorView()
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
                switch (arg.NavigationMode)
                {
                    case NavigationMode.New:
                        (_parent, _profile) = ((NavigationDialogHost, TempProfile))arg.Parameter;
                        _contentManager = _parent.ContentManager;

                        ((ContentDialog)_parent.Parent).Title = $"{LocaleManager.Instance[LocaleKeys.UserProfileWindowTitle]} - {LocaleManager.Instance[LocaleKeys.ProfileImageSelectionHeader]}";

                        if (Program.PreviewerDetached)
                        {
                            DataContext = ViewModel = new UserProfileImageSelectorViewModel();
                            ViewModel.FirmwareFound = _contentManager.GetCurrentFirmwareVersion() != null;
                        }

                        break;
                    case NavigationMode.Back:
                        if (_profile.Image != null)
                        {
                            _parent.GoBack();
                        }
                        break;
                }
            }
        }

        private async void Import_OnClick(object sender, RoutedEventArgs e)
        {
            IReadOnlyList<IStorageFile> result = await ((Window)this.GetVisualRoot()!).StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new(LocaleManager.Instance[LocaleKeys.AllSupportedFormats])
                    {
                        Patterns = ["*.jpg", "*.jpeg", "*.png", "*.bmp"],
                        AppleUniformTypeIdentifiers = ["public.jpeg", "public.png", "com.microsoft.bmp"],
                        MimeTypes = ["image/jpeg", "image/png", "image/bmp"],
                    },
                },
            });

            if (result.Count > 0)
            {
                _profile.Image = ProcessProfileImage(File.ReadAllBytes(result[0].Path.LocalPath));
                _parent.GoBack();
            }
        }

        private void GoBack(object sender, RoutedEventArgs e)
        {
            _parent.GoBack();
        }

        private void SelectFirmwareImage_OnClick(object sender, RoutedEventArgs e)
        {
            if (ViewModel.FirmwareFound)
            {
                _parent.Navigate(typeof(UserFirmwareAvatarSelectorView), (_parent, _profile));
            }
        }

        private static byte[] ProcessProfileImage(byte[] buffer)
        {
            using SKBitmap bitmap = SKBitmap.Decode(buffer);

            SKBitmap resizedBitmap = bitmap.Resize(new SKImageInfo(256, 256), SKFilterQuality.High);

            using MemoryStream streamJpg = new();

            if (resizedBitmap != null)
            {
                using SKImage image = SKImage.FromBitmap(resizedBitmap);
                using SKData dataJpeg = image.Encode(SKEncodedImageFormat.Jpeg, 100);

                dataJpeg.SaveTo(streamJpg);
            }

            return streamJpg.ToArray();
        }
    }
}

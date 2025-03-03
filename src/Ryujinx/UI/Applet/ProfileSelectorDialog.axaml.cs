using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using FluentAvalonia.UI.Controls;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Controls;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Services.Account.Acc;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using UserProfile = Ryujinx.Ava.UI.Models.UserProfile;
using UserProfileSft = Ryujinx.HLE.HOS.Services.Account.Acc.UserProfile;

namespace Ryujinx.Ava.UI.Applet
{
    public partial class ProfileSelectorDialog : RyujinxControl<ProfileSelectorDialogViewModel>
    {
        public ProfileSelectorDialog(ProfileSelectorDialogViewModel viewModel)
        {
            DataContext = ViewModel = viewModel;
            
            InitializeComponent();
        }
        
        private void Grid_PointerEntered(object sender, PointerEventArgs e)
        {
            if (sender is Grid { DataContext: UserProfile profile })
            {
                profile.IsPointerOver = true;
            }
        }

        private void Grid_OnPointerExited(object sender, PointerEventArgs e)
        {
            if (sender is Grid { DataContext: UserProfile profile })
            {
                profile.IsPointerOver = false;
            }
        }

        private void ProfilesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox)
            {
                int selectedIndex = listBox.SelectedIndex;

                if (selectedIndex >= 0 && selectedIndex < ViewModel.Profiles.Count)
                {
                    if (ViewModel.Profiles[selectedIndex] is UserProfile userProfile)
                    {
                        ViewModel.SelectedUserId = userProfile.UserId;
                        Logger.Info?.Print(LogClass.UI, $"Selected: {userProfile.UserId}", "ProfileSelector");

                        ObservableCollection<BaseModel> newProfiles = [];

                        foreach (BaseModel item in ViewModel.Profiles)
                        {
                            if (item is UserProfile originalItem)
                            {
                                UserProfileSft profile = new(originalItem.UserId, originalItem.Name, originalItem.Image);
                                
                                if (profile.UserId == ViewModel.SelectedUserId)
                                {
                                    profile.AccountState = AccountState.Open;
                                }

                                newProfiles.Add(new UserProfile(profile, new NavigationDialogHost()));
                            }
                        }

                        ViewModel.Profiles = newProfiles;
                    }
                }
            }
        }

        public static async Task<(UserId Id, bool Result)> ShowInputDialog(ProfileSelectorDialogViewModel viewModel)
        {
            ContentDialog contentDialog = new()
            {
                Title = LocaleManager.Instance[LocaleKeys.UserProfileWindowTitle],
                PrimaryButtonText = LocaleManager.Instance[LocaleKeys.Continue],
                SecondaryButtonText = string.Empty,
                CloseButtonText = LocaleManager.Instance[LocaleKeys.Cancel],
                Content = new ProfileSelectorDialog(viewModel),
                Padding = new Thickness(0)
            };

            UserId result = UserId.Null;
            bool input = false;
            
            contentDialog.Closed += Handler;

            await ContentDialogHelper.ShowAsync(contentDialog);

            return (result, input);
            
            void Handler(ContentDialog sender, ContentDialogClosedEventArgs eventArgs)
            {
                if (eventArgs.Result == ContentDialogResult.Primary)
                {
                    result = viewModel.SelectedUserId;
                    input = true;
                }
                else
                {
                    result = UserId.Null;
                    input = false;
                }
            }
        }
    }
}

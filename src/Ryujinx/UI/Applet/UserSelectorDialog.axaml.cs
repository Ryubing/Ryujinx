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
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UserProfile = Ryujinx.Ava.UI.Models.UserProfile;
using UserProfileSft = Ryujinx.HLE.HOS.Services.Account.Acc.UserProfile;

namespace Ryujinx.Ava.UI.Applet
{
    public partial class UserSelectorDialog : UserControl, INotifyPropertyChanged
    {
        public new event PropertyChangedEventHandler PropertyChanged;

        private UserId _selectedUserId;
        private ObservableCollection<BaseModel> _profiles;

        public ObservableCollection<BaseModel> Profiles
        {
            get => _profiles;
            set
            {
                if (_profiles != value)
                {
                    _profiles = value;
                    OnPropertyChanged();
                }
            }
        }

        public UserSelectorDialog(ObservableCollection<BaseModel> profiles)
        {
            InitializeComponent();
            Profiles = profiles;
            DataContext = this;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

                if (selectedIndex >= 0 && selectedIndex < Profiles.Count)
                {
                    if (Profiles[selectedIndex] is UserProfile userProfile)
                    {
                        _selectedUserId = userProfile.UserId;
                        Logger.Info?.Print(LogClass.UI, $"Selected user: {userProfile.UserId}");

                        ObservableCollection<BaseModel> newProfiles = [];

                        foreach (var item in Profiles)
                        {
                            if (item is UserProfile originalItem)
                            {
                                var profile = new UserProfileSft(originalItem.UserId, originalItem.Name, originalItem.Image);
                                
                                if (profile.UserId == _selectedUserId)
                                {
                                    profile.AccountState = AccountState.Open;
                                }

                                newProfiles.Add(new UserProfile(profile, new NavigationDialogHost()));
                            }
                        }

                        Profiles = newProfiles;
                    }
                }
            }
        }

        public static async Task<(UserId Id, bool Result)> ShowInputDialog(UserSelectorDialog content, UserProfileSft accountManagerLastOpenedUser)
        {
            content._selectedUserId = accountManagerLastOpenedUser.UserId;

            ContentDialog contentDialog = new()
            {
                Title = LocaleManager.Instance[LocaleKeys.UserProfileWindowTitle],
                PrimaryButtonText = LocaleManager.Instance[LocaleKeys.Continue],
                SecondaryButtonText = string.Empty,
                CloseButtonText = LocaleManager.Instance[LocaleKeys.Cancel],
                Content = content,
                Padding = new Thickness(0)
            };

            UserId result = UserId.Null;
            bool input = false;

            void Handler(ContentDialog sender, ContentDialogClosedEventArgs eventArgs)
            {
                if (eventArgs.Result == ContentDialogResult.Primary)
                {
                    if (contentDialog.Content is UserSelectorDialog view)
                    {
                        result = view._selectedUserId;
                        input = true;
                    }
                }
                else
                {
                    result = UserId.Null;
                    input = false;
                }
            }

            contentDialog.Closed += Handler;

            await ContentDialogHelper.ShowAsync(contentDialog);

            return (result, input);
        }
    }
}

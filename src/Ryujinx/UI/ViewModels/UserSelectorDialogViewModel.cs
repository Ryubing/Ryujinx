using CommunityToolkit.Mvvm.ComponentModel;
using Ryujinx.HLE.HOS.Services.Account.Acc;
using System.Collections.ObjectModel;

namespace Ryujinx.Ava.UI.ViewModels
{
    public partial class UserSelectorDialogViewModel : BaseModel
    {
        private UserId _selectedUserId;
        private ObservableCollection<BaseModel> _profiles;

        public UserId SelectedUserId
        {
            get => _selectedUserId;
            set
            {
                if (_selectedUserId != value)
                {
                    _selectedUserId = value;
                    OnPropertyChanged();
                }
            }
        }

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

        public UserSelectorDialogViewModel()
        {
            Profiles = new ObservableCollection<BaseModel>();
        }
    }
}

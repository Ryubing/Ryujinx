using CommunityToolkit.Mvvm.ComponentModel;
using Ryujinx.HLE.HOS.Services.Account.Acc;
using System.Collections.ObjectModel;

namespace Ryujinx.Ava.UI.ViewModels
{
    public partial class UserSelectorDialogViewModel : BaseModel
    {

        [ObservableProperty] private UserId _selectedUserId;

        [ObservableProperty] private ObservableCollection<BaseModel> _profiles = [];
    }
}

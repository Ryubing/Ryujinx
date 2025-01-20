using CommunityToolkit.Mvvm.ComponentModel;

namespace Ryujinx.Ava.UI.ViewModels
{
    internal partial class UserProfileImageSelectorViewModel : BaseModel
    {
        [ObservableProperty] private bool _firmwareFound;
    }
}

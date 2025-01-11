using CommunityToolkit.Mvvm.ComponentModel;

namespace Ryujinx.Ava.UI.ViewModels.Input
{
    public partial class RumbleInputViewModel : BaseModel
    {
        [ObservableProperty] private float _strongRumble;

        [ObservableProperty] private float _weakRumble;
    }
}

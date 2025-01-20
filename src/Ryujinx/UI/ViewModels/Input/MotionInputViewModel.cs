using CommunityToolkit.Mvvm.ComponentModel;

namespace Ryujinx.Ava.UI.ViewModels.Input
{
    public partial class MotionInputViewModel : BaseModel
    {
        [ObservableProperty] private int _slot;

        [ObservableProperty] private int _altSlot;

        [ObservableProperty] private string _dsuServerHost;

        [ObservableProperty] private int _dsuServerPort;

        [ObservableProperty] private bool _mirrorInput;

        [ObservableProperty] private int _sensitivity;

        [ObservableProperty] private double _gyroDeadzone;

        [ObservableProperty] private bool _enableCemuHookMotion;
    }
}

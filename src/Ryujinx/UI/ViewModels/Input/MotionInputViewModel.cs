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

        private bool _enableCemuHookMotion;
        public bool EnableCemuHookMotion
        {
            get => _enableCemuHookMotion;
            set
            {
                if (value)
                {
                    EnableHandheldMotion = false;
                }
                _enableCemuHookMotion = value;
                OnPropertyChanged();
            }
        }

        private bool _enableHandheldMotion;
        public bool EnableHandheldMotion
        {
            get => _enableHandheldMotion;
            set
            {
                if (value)
                {
                    EnableCemuHookMotion = false;
                }
                _enableHandheldMotion = value;
                OnPropertyChanged();
            }
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using Gommon;
using Ryujinx.Ava.Utilities.Configuration;

namespace Ryujinx.Ava.UI.ViewModels
{
    public partial class SettingsHacksViewModel : BaseModel
    {
        private readonly SettingsViewModel _baseViewModel;

        public SettingsHacksViewModel() {}
        
        public SettingsHacksViewModel(SettingsViewModel settingsVm)
        {
            _baseViewModel = settingsVm;
        }

        [ObservableProperty] private bool _xc2MenuSoftlockFix = ConfigurationState.Instance.Hacks.Xc2MenuSoftlockFix;
        [ObservableProperty] private bool _shaderTranslationDelayEnabled = ConfigurationState.Instance.Hacks.EnableShaderTranslationDelay;
        private int _shaderTranslationSleepDelay = ConfigurationState.Instance.Hacks.ShaderTranslationDelay;

        public string ShaderTranslationDelayValueText => $"{ShaderTranslationDelay}ms"; 
        
        public int ShaderTranslationDelay
        {
            get => _shaderTranslationSleepDelay;
            set
            {
                _shaderTranslationSleepDelay = value;
                
                OnPropertiesChanged(nameof(ShaderTranslationDelay), nameof(ShaderTranslationDelayValueText));
            }
        }
        
        public static string Xc2MenuFixTooltip { get; } = Lambda.String(sb =>
        {
            sb.AppendLine(
                    "This fix applies a 2ms delay (via 'Thread.Sleep(2)') every time the game tries to read data from the emulated Switch filesystem.")
                .AppendLine();
            
            sb.AppendLine("From the issue on GitHub:").AppendLine();
            sb.Append(
                "When clicking very fast from game main menu to 2nd submenu, " +
                "there is a low chance that the game will softlock, " +
                "the submenu won't show up, while background music is still there.");
        });

        public static string ShaderTranslationDelayTooltip { get; } = Lambda.String(sb =>
        {
            sb.AppendLine("This hack applies the delay you specify every time shaders are attempted to be translated.")
                .AppendLine();

            sb.Append("Configurable via slider, only when this option is enabled.");
        });
    }
}

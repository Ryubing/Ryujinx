using Gommon;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Systems.AppLibrary;
using Ryujinx.Ava.Systems.PlayReport;

namespace Ryujinx.Ava.UI.ViewModels
{
    public class ApplicationDataViewModel : BaseModel
    {
        public ApplicationData AppData { get; }

        public ApplicationDataViewModel(ApplicationData appData) => AppData = appData;

        public string DynamicRichPresenceDescription =>
            AppData.HasDynamicRichPresenceSupport
                ? AppData.RichPresenceSpec.Value.Description
                : GameSpec.DefaultDescription;

        public string FormattedVersion => LocaleManager.Instance[LocaleKeys.GameListHeaderVersion].Format(AppData.Version);
        public string FormattedDeveloper => LocaleManager.Instance[LocaleKeys.GameListHeaderDeveloper].Format(AppData.Developer);
        public string FormattedFileExtension => LocaleManager.Instance[LocaleKeys.GameListHeaderFileExtension].Format(AppData.FileExtension);
        public string FormattedFileSize => LocaleManager.Instance[LocaleKeys.GameListHeaderFileSize].Format(AppData.FileSizeString);
        
        public string FormattedLdnInfo => 
            $"{LocaleManager.Instance[LocaleKeys.GameListHeaderHostedGames].Format(AppData.GameCount)}" +
            $"\n" +
            $"{LocaleManager.Instance[LocaleKeys.GameListHeaderPlayerCount].Format(AppData.PlayerCount)}";
    }
}

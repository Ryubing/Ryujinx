using CommunityToolkit.Mvvm.ComponentModel;
using Ryujinx.Ava.Common.Models;
using Ryujinx.Ava.Utilities.AppLibrary;
using System.Linq;

namespace Ryujinx.Ava.UI.ViewModels
{
    public partial class DlcSelectViewModel : BaseModel
    {
        [ObservableProperty] private DownloadableContentModel[] _dlcs;
        #nullable enable
        [ObservableProperty] private DownloadableContentModel? _selectedDlc;
        #nullable disable
        
        public DlcSelectViewModel(ulong titleId, ApplicationLibrary appLibrary)
        {
            _dlcs = appLibrary.DownloadableContents.Items
                .Where(x => x.Dlc.TitleIdBase == titleId)
                .Select(x => x.Dlc)
                .OrderBy(it => it.IsBundled ? 0 : 1)
                .ThenBy(it => it.TitleId)
                .ToArray();
        }
    }
}

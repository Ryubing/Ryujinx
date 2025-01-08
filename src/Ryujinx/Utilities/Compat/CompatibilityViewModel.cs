using CommunityToolkit.Mvvm.ComponentModel;
using ExCSS;
using Gommon;
using Ryujinx.Ava.Utilities.AppLibrary;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Ava.Utilities.Compat
{
    public partial class CompatibilityViewModel : ObservableObject
    {
        [ObservableProperty] private bool _onlyShowOwnedGames = true;

        private IEnumerable<CompatibilityEntry> _currentEntries = CompatibilityCsv.Shared.Entries;
        private readonly string[] _ownedGameTitleIds = [];
        private readonly ApplicationLibrary _appLibrary;

        public IEnumerable<CompatibilityEntry> CurrentEntries => OnlyShowOwnedGames
            ? _currentEntries.Where(x =>
                x.TitleId.Check(tid => _ownedGameTitleIds.ContainsIgnoreCase(tid))
                || _appLibrary.Applications.Items.Any(a => a.Name.EqualsIgnoreCase(x.GameName)))
            : _currentEntries;

        public CompatibilityViewModel() {}

        public CompatibilityViewModel(ApplicationLibrary appLibrary)
        {
            _appLibrary = appLibrary;
            _ownedGameTitleIds = appLibrary.Applications.Keys.Select(x => x.ToString("X16")).ToArray();

            PropertyChanged += (_, args) =>
            {
                if (args.PropertyName is nameof(OnlyShowOwnedGames))
                    OnPropertyChanged(nameof(CurrentEntries));
            };
        }

        public void Search(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                SetEntries(CompatibilityCsv.Shared.Entries);
                return;
            }

            SetEntries(CompatibilityCsv.Shared.Entries.Where(x =>
                x.GameName.ContainsIgnoreCase(searchTerm)
                || x.TitleId.Check(tid => tid.ContainsIgnoreCase(searchTerm))));
        }

        private void SetEntries(IEnumerable<CompatibilityEntry> entries)
        {
#pragma warning disable MVVMTK0034
            _currentEntries = entries.ToList();
#pragma warning restore MVVMTK0034
            OnPropertyChanged(nameof(CurrentEntries));
        }
    }
}

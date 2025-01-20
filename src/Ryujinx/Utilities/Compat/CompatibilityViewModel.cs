using CommunityToolkit.Mvvm.ComponentModel;
using Gommon;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Ava.Utilities.AppLibrary;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Ava.Utilities.Compat
{
    public class CompatibilityViewModel : BaseModel
    {
        private bool _onlyShowOwnedGames = true;

        private IEnumerable<CompatibilityEntry> _currentEntries = CompatibilityCsv.Entries;
        private string[] _ownedGameTitleIds = [];

        public IEnumerable<CompatibilityEntry> CurrentEntries => OnlyShowOwnedGames
            ? _currentEntries.Where(x =>
                x.TitleId.Check(tid => _ownedGameTitleIds.ContainsIgnoreCase(tid)))
            : _currentEntries;

        public CompatibilityViewModel() {}

        public CompatibilityViewModel(ApplicationLibrary appLibrary)
        {
            appLibrary.ApplicationCountUpdated += (_, _) 
                => _ownedGameTitleIds = appLibrary.Applications.Keys.Select(x => x.ToString("X16")).ToArray();
            
            _ownedGameTitleIds = appLibrary.Applications.Keys.Select(x => x.ToString("X16")).ToArray();
        }

        public bool OnlyShowOwnedGames
        {
            get => _onlyShowOwnedGames;
            set
            {
                OnPropertyChanging();
                OnPropertyChanging(nameof(CurrentEntries));
                _onlyShowOwnedGames = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentEntries));
            }
        }

        public void Search(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                SetEntries(CompatibilityCsv.Entries);
                return;
            }

            SetEntries(CompatibilityCsv.Entries.Where(x =>
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

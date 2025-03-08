using Gommon;
using Ryujinx.Ava.Systems;
using Ryujinx.Ava.Systems.AppLibrary;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Ava.UI.ViewModels
{
    public class CompatibilityViewModel : BaseModel, IDisposable
    {
        private readonly ApplicationLibrary _appLibrary;

        private IEnumerable<CompatibilityEntry> _currentEntries = CompatibilityDatabase.Entries;
        private string[] _ownedGameTitleIds = [];

        public IEnumerable<CompatibilityEntry> CurrentEntries => OnlyShowOwnedGames
            ? _currentEntries.Where(x =>
                x.TitleId.Check(tid => _ownedGameTitleIds.ContainsIgnoreCase(tid)))
            : _currentEntries;

        public CompatibilityViewModel() {}
        
        private void AppCountUpdated(object _, ApplicationCountUpdatedEventArgs __)
            => _ownedGameTitleIds = _appLibrary.Applications.Keys.Select(x => x.ToString("X16")).ToArray();

        public CompatibilityViewModel(ApplicationLibrary appLibrary)
        {
            _appLibrary = appLibrary;
            
            AppCountUpdated(null, null);

            _appLibrary.ApplicationCountUpdated += AppCountUpdated;
        }

        void IDisposable.Dispose()
        {
            GC.SuppressFinalize(this);
            _appLibrary.ApplicationCountUpdated -= AppCountUpdated;
        }
        
        private bool _onlyShowOwnedGames = true;

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
                SetEntries(CompatibilityDatabase.Entries);
                return;
            }

            SetEntries(CompatibilityDatabase.Entries.Where(x =>
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

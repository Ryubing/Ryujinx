using Ryujinx.Ava.UI.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Ryujinx.Ava.UI.ViewModels
{
    public class UserProfileViewModel : BaseModel, IDisposable
    {
        public UserProfileViewModel()
        {
            Profiles = [];
            LostProfiles = [];
            IsEmpty = !LostProfiles.Any();
        }

        public ObservableCollection<BaseModel> Profiles { get; set; }

        public ObservableCollection<UserProfile> LostProfiles { get; set; }

        public bool IsEmpty { get; set; }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}

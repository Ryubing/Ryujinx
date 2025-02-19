using CommunityToolkit.Mvvm.ComponentModel;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.HLE.HOS.Services.Account.Acc;
using System;

namespace Ryujinx.Ava.UI.Models
{
    public partial class TempProfile : BaseModel
    {
        [ObservableProperty] private byte[] _image;
        [ObservableProperty] private string _name = String.Empty;
        private UserId _userId;

        public static uint MaxProfileNameLength => 0x20;

        public UserId UserId
        {
            get => _userId;
            set
            {
                _userId = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UserIdString));
            }
        }

        public string UserIdString => _userId.ToString();

        public TempProfile(UserProfile profile)
        {
            if (profile != null)
            {
                Image = profile.Image;
                Name = profile.Name;
                UserId = profile.UserId;
            }
        }
    }
}

using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Ryujinx.Ava.UI.ViewModels;

namespace Ryujinx.Ava.UI.Models
{
    public partial class ProfileImageModel : BaseModel
    {
        public ProfileImageModel(string name, byte[] data)
        {
            Name = name;
            Data = data;
        }

        public string Name { get; set; }
        public byte[] Data { get; set; }

        [ObservableProperty] private SolidColorBrush _backgroundColor = new(Colors.White);
    }
}

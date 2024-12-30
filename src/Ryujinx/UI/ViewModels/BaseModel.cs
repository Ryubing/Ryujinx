using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Ryujinx.Ava.UI.ViewModels
{
    public class BaseModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        protected void OnPropertiesChanged(string firstPropertyName, params ReadOnlySpan<string> propertyNames)
        {
            OnPropertyChanged(firstPropertyName);
            foreach (var propertyName in propertyNames)
            {
                OnPropertyChanged(propertyName);
            }
        }
    }
}

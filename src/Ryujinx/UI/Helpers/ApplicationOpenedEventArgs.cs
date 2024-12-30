using Avalonia.Interactivity;
using Ryujinx.Ava.Utilities.AppLibrary;

namespace Ryujinx.Ava.UI.Helpers
{
    public class ApplicationOpenedEventArgs : RoutedEventArgs
    {
        public ApplicationData Application { get; }

        public ApplicationOpenedEventArgs(ApplicationData application, RoutedEvent routedEvent)
        {
            Application = application;
            RoutedEvent = routedEvent;
        }
    }
}

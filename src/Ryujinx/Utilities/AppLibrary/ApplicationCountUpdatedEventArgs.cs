using System;

namespace Ryujinx.Ava.Utilities.AppLibrary
{
    public class ApplicationCountUpdatedEventArgs : EventArgs
    {
        public int NumAppsFound { get; set; }
        public int NumAppsLoaded { get; set; }
    }
}

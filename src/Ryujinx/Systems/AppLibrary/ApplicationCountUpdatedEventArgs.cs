using System;

namespace Ryujinx.Ava.Systems.AppLibrary
{
    public class ApplicationCountUpdatedEventArgs : EventArgs
    {
        public int NumAppsFound { get; set; }
        public int NumAppsLoaded { get; set; }
    }
}

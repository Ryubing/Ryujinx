using System;
using System.Collections.Generic;

namespace Ryujinx.Ava.Utilities.AppLibrary
{
    public class LdnGameDataReceivedEventArgs : EventArgs
    {
        public static new readonly LdnGameDataReceivedEventArgs Empty = new(null);
        
        public LdnGameDataReceivedEventArgs(LdnGameData[] ldnData)
        {
            LdnData = ldnData ?? [];
        }
        
        
        public LdnGameData[] LdnData { get; set; }
    }
}

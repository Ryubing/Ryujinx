using System;
using System.Collections.Generic;

namespace Ryujinx.Ava.Systems.AppLibrary
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

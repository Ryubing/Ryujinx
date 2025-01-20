using System;
using System.Collections.Generic;

namespace Ryujinx.Ava.Utilities.AppLibrary
{
    public class LdnGameDataReceivedEventArgs : EventArgs
    {
        public IEnumerable<LdnGameData> LdnData { get; set; }
    }
}

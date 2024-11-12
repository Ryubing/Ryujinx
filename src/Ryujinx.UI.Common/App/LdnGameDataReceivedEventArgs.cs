using System;
using System.Collections.Generic;

namespace Ryujinx.UI.App.Common
{
    public class LdnGameDataReceivedEventArgs : EventArgs
    {
        public IEnumerable<LdnGameData> LdnData { get; set; }
    }
}

using System.Collections.Generic;

namespace Ryujinx.UI.App.Common
{
    public struct LdnGameData
    {
        public string Id { get; set; }
        public int PlayerCount { get; set; }
        public int MaxPlayerCount { get; set; }
        public string GameName { get; set; }
        public string TitleId { get; set; }
        public string Mode { get; set; }
        public string Status { get; set; }
        public IEnumerable<string> Players { get; set; }
    }
}

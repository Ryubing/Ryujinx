using Gommon;
using LibHac.Ns;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Ava.Systems.AppLibrary
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

        public static Array GetArrayForApp(
            LdnGameData[] receivedData, ref ApplicationControlProperty acp)
        {
            LibHac.Common.FixedArrays.Array8<ulong> communicationId = acp.LocalCommunicationId;

            return new Array(receivedData.Where(game =>
                communicationId.Items.Contains(game.TitleId.ToULong())
            ));
        }

        public class Array
        {
            private readonly LdnGameData[] _ldnDatas;
            
            internal Array(IEnumerable<LdnGameData> receivedData)
            {
                _ldnDatas = receivedData.ToArray();
            }

            public int PlayerCount => _ldnDatas.Sum(it => it.PlayerCount);
            public int GameCount => _ldnDatas.Length;
        }
    }

    public static class LdnGameDataHelper
    {
        public static LdnGameData.Array Where(this LdnGameData[] unfilteredDatas, ref ApplicationControlProperty acp) 
            => LdnGameData.GetArrayForApp(unfilteredDatas, ref acp);
    }
}

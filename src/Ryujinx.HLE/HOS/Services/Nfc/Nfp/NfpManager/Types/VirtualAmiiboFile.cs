using System;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Nfc.Nfp.NfpManager
{
    public struct VirtualAmiiboFile
    {
        public uint FileVersion { get; set; }
        public byte[] TagUuid { get; set; }
        public string AmiiboId { get; set; }
        public string NickName { get; set; }
        public DateTime FirstWriteDate { get; set; }
        public DateTime LastWriteDate { get; set; }
        public ushort WriteCounter { get; set; }
        public List<VirtualAmiiboApplicationArea> ApplicationAreas { get; set; }
    }

    public struct VirtualAmiiboApplicationArea
    {
        public uint ApplicationAreaId { get; set; }
        public byte[] ApplicationArea { get; set; }
    }
}

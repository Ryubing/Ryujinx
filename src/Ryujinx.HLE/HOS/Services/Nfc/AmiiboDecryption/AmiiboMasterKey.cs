using System;
using System.Linq;

namespace Ryujinx.HLE.HOS.Services.Nfc.AmiiboDecryption
{
    public class AmiiboMasterKey
    {
        public byte[] HmacKey { get; private set; }     // 16 bytes
        public byte[] TypeString { get; private set; }  // 14 bytes
        public byte Rfu { get; private set; }           // 1 byte
        public byte MagicSize { get; private set; }     // 1 byte
        public byte[] MagicBytes { get; private set; }  // 16 bytes
        public byte[] XorPad { get; private set; }      // 32 bytes

        public AmiiboMasterKey(byte[] data)
        {
            if (data.Length != 80)
                throw new ArgumentException("Master key data must be 80 bytes.");

            HmacKey = data.Take(16).ToArray();
            TypeString = data.Skip(16).Take(14).ToArray();
            Rfu = data[30];
            MagicSize = data[31];
            MagicBytes = data.Skip(32).Take(16).ToArray();
            XorPad = data.Skip(48).Take(32).ToArray();
        }

        public static (AmiiboMasterKey DataKey, AmiiboMasterKey TagKey) FromCombinedBin(byte[] combinedBin)
        {
            if (combinedBin.Length != 160)
                throw new ArgumentException($"Data is {combinedBin.Length} bytes (should be 160).");

            byte[] dataBin = combinedBin.Take(80).ToArray();
            byte[] tagBin = combinedBin.Skip(80).Take(80).ToArray();

            return (new AmiiboMasterKey(dataBin), new AmiiboMasterKey(tagBin));
        }
    }
}

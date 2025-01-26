using System.IO;

namespace Ryujinx.HLE.HOS.Services.Nfc.AmiiboDecryption
{
    public class AmiiboDecryptor
    {
        public AmiiboMasterKey DataKey { get; private set; }
        public AmiiboMasterKey TagKey { get; private set; }

        public AmiiboDecryptor(string keyRetailBinPath)
        {
            byte[] combinedKeys = File.ReadAllBytes(keyRetailBinPath);
            (AmiiboMasterKey DataKey, AmiiboMasterKey TagKey) keys = AmiiboMasterKey.FromCombinedBin(combinedKeys);
            DataKey = keys.DataKey;
            TagKey = keys.TagKey;
        }

        public AmiiboDump DecryptAmiiboDump(byte[] encryptedDumpData)
        {
            // Initialize AmiiboDump with encrypted data
            AmiiboDump amiiboDump = new(encryptedDumpData, DataKey, TagKey, isLocked: true);

            // Unlock (decrypt) the dump
            amiiboDump.Unlock();

            // Optional: Verify HMACs
            amiiboDump.VerifyHMACs();

            return amiiboDump;
        }

        public AmiiboDump EncryptAmiiboDump(byte[] decryptedDumpData)
        {
            // Initialize AmiiboDump with decrypted data
            AmiiboDump amiiboDump = new(decryptedDumpData, DataKey, TagKey, isLocked: false);

            // Lock (encrypt) the dump
            amiiboDump.Lock();

            return amiiboDump;
        }
    }
}

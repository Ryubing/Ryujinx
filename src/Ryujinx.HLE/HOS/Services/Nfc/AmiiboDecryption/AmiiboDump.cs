using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace Ryujinx.HLE.HOS.Services.Nfc.AmiiboDecryption
{
    public class AmiiboDump
    {
        private AmiiboMasterKey dataMasterKey;
        private AmiiboMasterKey tagMasterKey;

        private bool isLocked;
        private byte[] data;
        private byte[] hmacTagKey;
        private byte[] hmacDataKey;
        private byte[] aesKey;
        private byte[] aesIv;

        public AmiiboDump(byte[] dumpData, AmiiboMasterKey dataKey, AmiiboMasterKey tagKey, bool isLocked = true)
        {
            if (dumpData.Length < 540)
                throw new ArgumentException("Incomplete dump. Amiibo data is at least 540 bytes.");

            this.data = new byte[540];
            Array.Copy(dumpData, this.data, dumpData.Length);
            this.dataMasterKey = dataKey;
            this.tagMasterKey = tagKey;
            this.isLocked = isLocked;

            if (!isLocked)
            {
                DeriveKeysAndCipher();
            }
        }

        private byte[] DeriveKey(AmiiboMasterKey key, bool deriveAes, out byte[] derivedAesKey, out byte[] derivedAesIv)
        {
            List<byte> seed = [];

            // Start with the type string (14 bytes)
            seed.AddRange(key.TypeString);

            // Append data based on magic size
            int append = 16 - key.MagicSize;
            byte[] extract = new byte[16];
            Array.Copy(this.data, 0x011, extract, 0, 2); // Extract two bytes from user data section
            for (int i = 2; i < 16; i++)
            {
                extract[i] = 0x00;
            }
            seed.AddRange(extract.Take(append));

            // Add the magic bytes
            seed.AddRange(key.MagicBytes.Take(key.MagicSize));

            // Extract the UID (UID is 8 bytes)
            byte[] uid = new byte[8];
            Array.Copy(this.data, 0x000, uid, 0, 8);
            seed.AddRange(uid);
            seed.AddRange(uid);

            // Extract some tag data (pages 0x20 - 0x28)
            byte[] user = new byte[32];
            Array.Copy(this.data, 0x060, user, 0, 32);

            // XOR it with the key padding (XorPad)
            byte[] paddedUser = new byte[32];
            for (int i = 0; i < user.Length; i++)
            {
                paddedUser[i] = (byte)(user[i] ^ key.XorPad[i]);
            }
            seed.AddRange(paddedUser);

            byte[] seedBytes = seed.ToArray();
            if (seedBytes.Length != 78)
            {
                throw new Exception("Size check for key derived seed failed");
            }

            byte[] hmacKey;
            derivedAesKey = null;
            derivedAesIv = null;

            if (deriveAes)
            {
                // Derive AES Key and IV
                byte[] dataForAes = new byte[2 + seedBytes.Length];
                dataForAes[0] = 0x00;
                dataForAes[1] = 0x00; // Counter (0)
                Array.Copy(seedBytes, 0, dataForAes, 2, seedBytes.Length);

                byte[] derivedBytes;
                using (HMACSHA256 hmac = new(key.HmacKey))
                {
                    derivedBytes = hmac.ComputeHash(dataForAes);
                }

                derivedAesKey = derivedBytes.Take(16).ToArray();
                derivedAesIv = derivedBytes.Skip(16).Take(16).ToArray();

                // Derive HMAC Key
                byte[] dataForHmacKey = new byte[2 + seedBytes.Length];
                dataForHmacKey[0] = 0x00;
                dataForHmacKey[1] = 0x01; // Counter (1)
                Array.Copy(seedBytes, 0, dataForHmacKey, 2, seedBytes.Length);

                using (HMACSHA256 hmac = new(key.HmacKey))
                {
                    derivedBytes = hmac.ComputeHash(dataForHmacKey);
                }

                hmacKey = derivedBytes.Take(16).ToArray();
            }
            else
            {
                // Derive HMAC Key only
                byte[] dataForHmacKey = new byte[2 + seedBytes.Length];
                dataForHmacKey[0] = 0x00;
                dataForHmacKey[1] = 0x01; // Counter (1)
                Array.Copy(seedBytes, 0, dataForHmacKey, 2, seedBytes.Length);

                byte[] derivedBytes;
                using (HMACSHA256 hmac = new(key.HmacKey))
                {
                    derivedBytes = hmac.ComputeHash(dataForHmacKey);
                }

                hmacKey = derivedBytes.Take(16).ToArray();
            }

            return hmacKey;
        }

        private void DeriveKeysAndCipher()
        {
            byte[] discard;
            // Derive HMAC Tag Key
            this.hmacTagKey = DeriveKey(this.tagMasterKey, false, out discard, out discard);

            // Derive HMAC Data Key and AES Key/IV
            this.hmacDataKey = DeriveKey(this.dataMasterKey, true, out aesKey, out aesIv);
        }

        private void DecryptData()
        {
            byte[] encryptedBlock = new byte[0x020 + 0x168];
            Array.Copy(data, 0x014, encryptedBlock, 0, 0x020);       // data[0x014:0x034]
            Array.Copy(data, 0x0A0, encryptedBlock, 0x020, 0x168);   // data[0x0A0:0x208]

            byte[] decryptedBlock = AES_CTR_Transform(encryptedBlock, aesKey, aesIv);

            // Copy decrypted data back
            Array.Copy(decryptedBlock, 0, data, 0x014, 0x020);
            Array.Copy(decryptedBlock, 0x020, data, 0x0A0, 0x168);
        }

        private void EncryptData()
        {
            byte[] plainBlock = new byte[0x020 + 0x168];
            Array.Copy(data, 0x014, plainBlock, 0, 0x020);       // data[0x014:0x034]
            Array.Copy(data, 0x0A0, plainBlock, 0x020, 0x168);   // data[0x0A0:0x208]

            byte[] encryptedBlock = AES_CTR_Transform(plainBlock, aesKey, aesIv);

            // Copy encrypted data back
            Array.Copy(encryptedBlock, 0, data, 0x014, 0x020);
            Array.Copy(encryptedBlock, 0x020, data, 0x0A0, 0x168);
        }

        private byte[] AES_CTR_Transform(byte[] data, byte[] key, byte[] iv)
        {
            byte[] output = new byte[data.Length];

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.None;

                int blockSize = aes.BlockSize / 8; // in bytes, should be 16
                byte[] counter = new byte[blockSize];
                Array.Copy(iv, counter, blockSize);

                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    byte[] encryptedCounter = new byte[blockSize];

                    for (int i = 0; i < data.Length; i += blockSize)
                    {
                        // Encrypt the counter
                        encryptor.TransformBlock(counter, 0, blockSize, encryptedCounter, 0);

                        // Determine the number of bytes to process in this block
                        int blockLength = Math.Min(blockSize, data.Length - i);

                        // XOR the encrypted counter with the plaintext/ciphertext block
                        for (int j = 0; j < blockLength; j++)
                        {
                            output[i + j] = (byte)(data[i + j] ^ encryptedCounter[j]);
                        }

                        // Increment the counter
                        IncrementCounter(counter);
                    }
                }
            }

            return output;
        }

        private void IncrementCounter(byte[] counter)
        {
            for (int i = counter.Length - 1; i >= 0; i--)
            {
                if (++counter[i] != 0)
                    break;
            }
        }

        private void DeriveHMACs()
        {
            if (isLocked)
                throw new InvalidOperationException("Cannot derive HMACs when data is locked.");

            // Calculate tag HMAC
            byte[] tagHmacData = new byte[8 + 44];
            Array.Copy(data, 0x000, tagHmacData, 0, 8);
            Array.Copy(data, 0x054, tagHmacData, 8, 44);

            byte[] tagHmac;
            using (HMACSHA256 hmac = new(hmacTagKey))
            {
                tagHmac = hmac.ComputeHash(tagHmacData);
            }

            // Overwrite the stored tag HMAC
            Array.Copy(tagHmac, 0, data, 0x034, 32);

            // Prepare data for data HMAC
            int len1 = 0x023; // 0x011 to 0x034 (0x034 - 0x011)
            int len2 = 0x168; // 0x0A0 to 0x208 (0x208 - 0x0A0)
            int len3 = tagHmac.Length; // 32 bytes
            int len4 = 0x008; // 0x000 to 0x008 (0x008 - 0x000)
            int len5 = 0x02C; // 0x054 to 0x080 (0x080 - 0x054)
            int totalLength = len1 + len2 + len3 + len4 + len5;
            byte[] dataHmacData = new byte[totalLength];

            int offset = 0;
            Array.Copy(data, 0x011, dataHmacData, offset, len1);
            offset += len1;
            Array.Copy(data, 0x0A0, dataHmacData, offset, len2);
            offset += len2;
            Array.Copy(tagHmac, 0, dataHmacData, offset, len3);
            offset += len3;
            Array.Copy(data, 0x000, dataHmacData, offset, len4);
            offset += len4;
            Array.Copy(data, 0x054, dataHmacData, offset, len5);

            byte[] dataHmac;
            using (HMACSHA256 hmac = new(hmacDataKey))
            {
                dataHmac = hmac.ComputeHash(dataHmacData);
            }

            // Overwrite the stored data HMAC
            Array.Copy(dataHmac, 0, data, 0x080, 32);
        }

        public void VerifyHMACs()
        {
            if (isLocked)
                throw new InvalidOperationException("Cannot verify HMACs when data is locked.");

            // Calculate tag HMAC
            byte[] tagHmacData = new byte[8 + 44];
            Array.Copy(data, 0x000, tagHmacData, 0, 8);
            Array.Copy(data, 0x054, tagHmacData, 8, 44);

            byte[] calculatedTagHmac;
            using (HMACSHA256 hmac = new(hmacTagKey))
            {
                calculatedTagHmac = hmac.ComputeHash(tagHmacData);
            }

            byte[] storedTagHmac = new byte[32];
            Array.Copy(data, 0x034, storedTagHmac, 0, 32);

            if (!calculatedTagHmac.SequenceEqual(storedTagHmac))
            {
                throw new Exception("Tag HMAC verification failed.");
            }

            // Prepare data for data HMAC
            int len1 = 0x023; // 0x011 to 0x034
            int len2 = 0x168; // 0x0A0 to 0x208
            int len3 = calculatedTagHmac.Length; // 32 bytes
            int len4 = 0x008; // 0x000 to 0x008
            int len5 = 0x02C; // 0x054 to 0x080
            int totalLength = len1 + len2 + len3 + len4 + len5;
            byte[] dataHmacData = new byte[totalLength];

            int offset = 0;
            Array.Copy(data, 0x011, dataHmacData, offset, len1);
            offset += len1;
            Array.Copy(data, 0x0A0, dataHmacData, offset, len2);
            offset += len2;
            Array.Copy(calculatedTagHmac, 0, dataHmacData, offset, len3);
            offset += len3;
            Array.Copy(data, 0x000, dataHmacData, offset, len4);
            offset += len4;
            Array.Copy(data, 0x054, dataHmacData, offset, len5);

            byte[] calculatedDataHmac;
            using (HMACSHA256 hmac = new(hmacDataKey))
            {
                calculatedDataHmac = hmac.ComputeHash(dataHmacData);
            }

            byte[] storedDataHmac = new byte[32];
            Array.Copy(data, 0x080, storedDataHmac, 0, 32);

            if (!calculatedDataHmac.SequenceEqual(storedDataHmac))
            {
                throw new Exception("Data HMAC verification failed.");
            }
        }

        public void Unlock()
        {
            if (!isLocked)
                throw new InvalidOperationException("Data is already unlocked.");

            // Derive keys and cipher
            DeriveKeysAndCipher();

            // Decrypt the encrypted data
            DecryptData();

            isLocked = false;
        }

        public void Lock()
        {
            if (isLocked)
                throw new InvalidOperationException("Data is already locked.");

            // Recalculate HMACs
            DeriveHMACs();

            // Encrypt the data
            EncryptData();

            isLocked = true;
        }

        public byte[] GetData()
        {
            return data;
        }

        // Property to get or set Amiibo nickname
        public string AmiiboNickname
        {
            get
            {
                // data[0x020:0x034], big endian UTF-16
                byte[] nicknameBytes = new byte[0x014];
                Array.Copy(data, 0x020, nicknameBytes, 0, 0x014);
                string nickname = System.Text.Encoding.BigEndianUnicode.GetString(nicknameBytes).TrimEnd('\0');
                return nickname;
            }
            set
            {
                byte[] nicknameBytes = System.Text.Encoding.BigEndianUnicode.GetBytes(value.PadRight(10, '\0'));
                if (nicknameBytes.Length > 20)
                    throw new ArgumentException("Nickname too long.");
                Array.Copy(nicknameBytes, 0, data, 0x020, nicknameBytes.Length);
                // Pad remaining bytes with zeros
                for (int i = 0x020 + nicknameBytes.Length; i < 0x034; i++)
                {
                    data[i] = 0x00;
                }
            }
        }
    }
}

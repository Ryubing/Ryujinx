using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Services.Nfc.Nfp;
using Ryujinx.HLE.HOS.Services.Nfc.Nfp.NfpManager;
using System;
using System.IO;

namespace Ryujinx.HLE.HOS.Services.Nfc.AmiiboDecryption
{
    public class AmiiboBinReader
    {
        private static byte CalculateBCC0(byte[] uid)
        {
            return (byte)(uid[0] ^ uid[1] ^ uid[2] ^ 0x88);
        }

        private static byte CalculateBCC1(byte[] uid)
        {
            return (byte)(uid[3] ^ uid[4] ^ uid[5] ^ uid[6]);
        }

        public static VirtualAmiiboFile ReadBinFile(byte[] fileBytes)
        {
            string keyRetailBinPath = GetKeyRetailBinPath();
            if (string.IsNullOrEmpty(keyRetailBinPath))
            {
                return new VirtualAmiiboFile();
            }

            byte[] initialCounter = new byte[16];

            const int totalPages = 135;
            const int pageSize = 4;
            const int totalBytes = totalPages * pageSize;

            if (fileBytes.Length == 532)
            {
                // add 8 bytes to the end of the file
                byte[] newFileBytes = new byte[totalBytes];
                Array.Copy(fileBytes, newFileBytes, fileBytes.Length);
                fileBytes = newFileBytes;
            }

            AmiiboDecryptor amiiboDecryptor = new(keyRetailBinPath);
            AmiiboDump amiiboDump = amiiboDecryptor.DecryptAmiiboDump(fileBytes);

            byte[] titleId = new byte[8];
            byte[] usedCharacter = new byte[2];
            byte[] variation = new byte[2];
            byte[] amiiboID = new byte[2];
            byte[] setID = new byte[1];
            byte[] initDate = new byte[2];
            byte[] writeDate = new byte[2];
            byte[] writeCounter = new byte[2];
            byte[] appId = new byte[8];
            byte[] settingsBytes = new byte[2];
            byte formData = 0;
            byte[] applicationAreas = new byte[216];
            byte[] dataFull = amiiboDump.GetData();
            Logger.Debug?.Print(LogClass.ServiceNfp, $"Data Full Length: {dataFull.Length}");
            byte[] uid = new byte[7];
            Array.Copy(dataFull, 0, uid, 0, 7);

            byte bcc0 = CalculateBCC0(uid);
            byte bcc1 = CalculateBCC1(uid);
            LogDebugData(uid, bcc0, bcc1);
            for (int page = 0; page < 128; page++) // NTAG215 has 128 pages
            {
                int pageStartIdx = page * 4; // Each page is 4 bytes
                byte[] pageData = new byte[4];
                byte[] sourceBytes = dataFull;
                Array.Copy(sourceBytes, pageStartIdx, pageData, 0, 4);
                // Special handling for specific pages
                switch (page)
                {
                    case 0: // Page 0 (UID + BCC0)
                        Logger.Debug?.Print(LogClass.ServiceNfp, "Page 0: UID and BCC0.");
                        break;
                    case 2: // Page 2 (BCC1 + Internal Value)
                        byte internalValue = pageData[1];
                        Logger.Debug?.Print(LogClass.ServiceNfp, $"Page 2: BCC1 + Internal Value 0x{internalValue:X2} (Expected 0x48).");
                        break;
                    case 6:
                        // Bytes 0 and 1 are init date, bytes 2 and 3 are write date
                        Array.Copy(pageData, 0, initDate, 0, 2);
                        Array.Copy(pageData, 2, writeDate, 0, 2);
                        break;
                    case 21:
                        // Bytes 0 and 1 are used character, bytes 2 and 3 are variation
                        Array.Copy(pageData, 0, usedCharacter, 0, 2);
                        Array.Copy(pageData, 2, variation, 0, 2);
                        break;
                    case 22:
                        // Bytes 0 and 1 are amiibo ID, byte 2 is set ID, byte 3 is form data
                        Array.Copy(pageData, 0, amiiboID, 0, 2);
                        setID[0] = pageData[2];
                        formData = pageData[3];
                        break;
                    case 64:
                    case 65:
                        // Extract title ID
                        int titleIdOffset = (page - 64) * 4;
                        Array.Copy(pageData, 0, titleId, titleIdOffset, 4);
                        break;
                    case 66:
                        // Bytes 0 and 1 are write counter
                        Array.Copy(pageData, 0, writeCounter, 0, 2);
                        break;
                    // Pages 76 to 127 are application areas
                    case >= 76 and <= 127:
                        int appAreaOffset = (page - 76) * 4;
                        Array.Copy(pageData, 0, applicationAreas, appAreaOffset, 4);
                        break;
                }
            }

            string usedCharacterStr = Convert.ToHexString(usedCharacter);
            string variationStr = Convert.ToHexString(variation);
            string amiiboIDStr = Convert.ToHexString(amiiboID);
            string setIDStr = Convert.ToHexString(setID);
            string head = usedCharacterStr + variationStr;
            string tail = amiiboIDStr + setIDStr + "02";
            string finalID = head + tail;

            ushort settingsValue = BitConverter.ToUInt16(settingsBytes, 0);
            ushort initDateValue = BitConverter.ToUInt16(initDate, 0);
            ushort writeDateValue = BitConverter.ToUInt16(writeDate, 0);
            DateTime initDateTime = DateTimeFromTag(initDateValue);
            DateTime writeDateTime = DateTimeFromTag(writeDateValue);
            ushort writeCounterValue = BitConverter.ToUInt16(writeCounter, 0);
            string nickName = amiiboDump.AmiiboNickname;
            LogFinalData(titleId, appId, head, tail, finalID, nickName, initDateTime, writeDateTime, settingsValue, writeCounterValue, applicationAreas);

            VirtualAmiiboFile virtualAmiiboFile = new()
            {
                FileVersion = 1,
                TagUuid = uid,
                AmiiboId = finalID,
                NickName = nickName,
                FirstWriteDate = initDateTime,
                LastWriteDate = writeDateTime,
                WriteCounter = writeCounterValue,
            };
            if (writeCounterValue > 0)
            {
                VirtualAmiibo.ApplicationBytes = applicationAreas;
            }
            VirtualAmiibo.NickName = nickName;
            return virtualAmiiboFile;
        }
        public static bool SaveBinFile(string inputFile, byte[] appData)
        {
            Logger.Info?.Print(LogClass.ServiceNfp, "Saving bin file.");
            byte[] readBytes;
            try
            {
                readBytes = File.ReadAllBytes(inputFile);
            }
            catch (Exception ex)
            {
                Logger.Error?.Print(LogClass.ServiceNfp, $"Error reading file: {ex.Message}");
                return false;
            }
            string keyRetailBinPath = GetKeyRetailBinPath();
            if (string.IsNullOrEmpty(keyRetailBinPath))
            {
                Logger.Error?.Print(LogClass.ServiceNfp, "Key retail path is empty.");
                return false;
            }

            if (appData.Length != 216) // Ensure application area size is valid
            {
                Logger.Error?.Print(LogClass.ServiceNfp, "Invalid application data length. Expected 216 bytes.");
                return false;
            }

            if (readBytes.Length == 532)
            {
                // add 8 bytes to the end of the file
                byte[] newFileBytes = new byte[540];
                Array.Copy(readBytes, newFileBytes, readBytes.Length);
                readBytes = newFileBytes;
            }

            AmiiboDecryptor amiiboDecryptor = new(keyRetailBinPath);
            AmiiboDump amiiboDump = amiiboDecryptor.DecryptAmiiboDump(readBytes);

            byte[] oldData = amiiboDump.GetData();
            if (oldData.Length != 540) // Verify the expected length for NTAG215 tags
            {
                Logger.Error?.Print(LogClass.ServiceNfp, "Invalid tag data length. Expected 540 bytes.");
                return false;
            }

            byte[] newData = new byte[oldData.Length];
            Array.Copy(oldData, newData, oldData.Length);

            // Replace application area with appData
            int appAreaOffset = 76 * 4; // Starting page (76) times 4 bytes per page
            Array.Copy(appData, 0, newData, appAreaOffset, appData.Length);

            AmiiboDump encryptedDump = amiiboDecryptor.EncryptAmiiboDump(newData);
            byte[] encryptedData = encryptedDump.GetData();

            if (encryptedData == null || encryptedData.Length != readBytes.Length)
            {
                Logger.Error?.Print(LogClass.ServiceNfp, "Failed to encrypt data correctly.");
                return false;
            }
            inputFile = inputFile.Replace("_modified", string.Empty);
            // Save the encrypted data to file or return it for saving externally
            string outputFilePath = Path.Combine(Path.GetDirectoryName(inputFile), Path.GetFileNameWithoutExtension(inputFile) + "_modified.bin");
            try
            {
                File.WriteAllBytes(outputFilePath, encryptedData);
                Logger.Info?.Print(LogClass.ServiceNfp, $"Modified Amiibo data saved to {outputFilePath}.");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error?.Print(LogClass.ServiceNfp, $"Error saving file: {ex.Message}");
                return false;
            }
        }
        public static bool SaveBinFile(string inputFile, string newNickName)
        {
            Logger.Info?.Print(LogClass.ServiceNfp, "Saving bin file.");
            byte[] readBytes;
            try
            {
                readBytes = File.ReadAllBytes(inputFile);
            }
            catch (Exception ex)
            {
                Logger.Error?.Print(LogClass.ServiceNfp, $"Error reading file: {ex.Message}");
                return false;
            }
            string keyRetailBinPath = GetKeyRetailBinPath();
            if (string.IsNullOrEmpty(keyRetailBinPath))
            {
                Logger.Error?.Print(LogClass.ServiceNfp, "Key retail path is empty.");
                return false;
            }

            if (readBytes.Length == 532)
            {
                // add 8 bytes to the end of the file
                byte[] newFileBytes = new byte[540];
                Array.Copy(readBytes, newFileBytes, readBytes.Length);
                readBytes = newFileBytes;
            }

            AmiiboDecryptor amiiboDecryptor = new(keyRetailBinPath);
            AmiiboDump amiiboDump = amiiboDecryptor.DecryptAmiiboDump(readBytes);
            amiiboDump.AmiiboNickname = newNickName;
            byte[] oldData = amiiboDump.GetData();
            if (oldData.Length != 540) // Verify the expected length for NTAG215 tags
            {
                Logger.Error?.Print(LogClass.ServiceNfp, "Invalid tag data length. Expected 540 bytes.");
                return false;
            }
            byte[] encryptedData = amiiboDecryptor.EncryptAmiiboDump(oldData).GetData();

            if (encryptedData == null || encryptedData.Length != readBytes.Length)
            {
                Logger.Error?.Print(LogClass.ServiceNfp, "Failed to encrypt data correctly.");
                return false;
            }
            inputFile = inputFile.Replace("_modified", string.Empty);
            // Save the encrypted data to file or return it for saving externally
            string outputFilePath = Path.Combine(Path.GetDirectoryName(inputFile), Path.GetFileNameWithoutExtension(inputFile) + "_modified.bin");
            try
            {
                File.WriteAllBytes(outputFilePath, encryptedData);
                Logger.Info?.Print(LogClass.ServiceNfp, $"Modified Amiibo data saved to {outputFilePath}.");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error?.Print(LogClass.ServiceNfp, $"Error saving file: {ex.Message}");
                return false;
            }
        }
        private static void LogDebugData(byte[] uid, byte bcc0, byte bcc1)
        {
            Logger.Debug?.Print(LogClass.ServiceNfp, $"UID: {BitConverter.ToString(uid)}");
            Logger.Debug?.Print(LogClass.ServiceNfp, $"BCC0: 0x{bcc0:X2}, BCC1: 0x{bcc1:X2}");
        }

        private static void LogFinalData(byte[] titleId, byte[] appId, string head, string tail, string finalID, string nickName, DateTime initDateTime, DateTime writeDateTime, ushort settingsValue, ushort writeCounterValue, byte[] applicationAreas)
        {
            Logger.Debug?.Print(LogClass.ServiceNfp, $"Title ID: 0x{Convert.ToHexString(titleId)}");
            Logger.Debug?.Print(LogClass.ServiceNfp, $"Application Program ID: 0x{Convert.ToHexString(appId)}");
            Logger.Debug?.Print(LogClass.ServiceNfp, $"Head: {head}");
            Logger.Debug?.Print(LogClass.ServiceNfp, $"Tail: {tail}");
            Logger.Debug?.Print(LogClass.ServiceNfp, $"Final ID: {finalID}");
            Logger.Debug?.Print(LogClass.ServiceNfp, $"Nickname: {nickName}");
            Logger.Debug?.Print(LogClass.ServiceNfp, $"Init Date: {initDateTime}");
            Logger.Debug?.Print(LogClass.ServiceNfp, $"Write Date: {writeDateTime}");
            Logger.Debug?.Print(LogClass.ServiceNfp, $"Settings: 0x{settingsValue:X4}");
            Logger.Debug?.Print(LogClass.ServiceNfp, $"Write Counter: {writeCounterValue}");
            Logger.Debug?.Print(LogClass.ServiceNfp, "Length of Application Areas: " + applicationAreas.Length);
        }

        private static uint CalculateCRC32(byte[] input)
        {
            uint[] table = new uint[256];
            uint polynomial = 0xEDB88320;
            for (uint i = 0; i < table.Length; ++i)
            {
                uint crc = i;
                for (int j = 0; j < 8; ++j)
                {
                    if ((crc & 1) != 0)
                        crc = (crc >> 1) ^ polynomial;
                    else
                        crc >>= 1;
                }
                table[i] = crc;
            }

            uint result = 0xFFFFFFFF;
            foreach (byte b in input)
            {
                byte index = (byte)((result & 0xFF) ^ b);
                result = (result >> 8) ^ table[index];
            }
            return ~result;
        }

        private static string GetKeyRetailBinPath()
        {
            return Path.Combine(AppDataManager.KeysDirPath, "key_retail.bin");
        }

        public static bool HasAmiiboKeyFile => File.Exists(GetKeyRetailBinPath());

        
        public static DateTime DateTimeFromTag(ushort value)
        {
            try
            {
                int day = value & 0x1F;
                int month = (value >> 5) & 0x0F;
                int year = (value >> 9) & 0x7F;

                if (day == 0 || month == 0 || month > 12 || day > DateTime.DaysInMonth(2000 + year, month))
                    throw new ArgumentOutOfRangeException();

                return new DateTime(2000 + year, month, day);
            }
            catch
            {
                return DateTime.Now;
            }
        }
    }
}

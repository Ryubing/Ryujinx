using LibHac.Common;
using Microsoft.IO;
using Ryujinx.Common.Memory;
using Ryujinx.HLE.HOS;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Ryujinx.HLE.Utilities
{
    static class StringUtils
    {
        public static byte[] GetFixedLengthBytes(string inputString, int size, Encoding encoding)
        {
            inputString += "\0";

            int bytesCount = encoding.GetByteCount(inputString);

            byte[] output = new byte[size];

            if (bytesCount < size)
            {
                encoding.GetBytes(inputString, 0, inputString.Length, output, 0);
            }
            else
            {
                int nullSize = encoding.GetByteCount("\0");

                output = encoding.GetBytes(inputString);

                Array.Resize(ref output, size - nullSize);

                output = [.. output, .. encoding.GetBytes("\0")];
            }

            return output;
        }

        public static string ReadInlinedAsciiString(BinaryReader reader, int maxSize)
        {
            byte[] data = reader.ReadBytes(maxSize);

            int stringSize = Array.IndexOf<byte>(data, 0);

            return Encoding.ASCII.GetString(data, 0, stringSize < 0 ? maxSize : stringSize);
        }

        public static byte[] HexToBytes(string hexString)
        {
            // Ignore last character if HexLength % 2 != 0.
            int bytesInHex = hexString.Length / 2;

            byte[] output = new byte[bytesInHex];

            for (int index = 0; index < bytesInHex; index++)
            {
                output[index] = byte.Parse(hexString.AsSpan(index * 2, 2), NumberStyles.HexNumber);
            }

            return output;
        }

        public static string ReadUtf8String(ReadOnlySpan<byte> data, out int dataRead)
        {
            dataRead = data.IndexOf((byte)0) + 1;

            if (dataRead <= 1)
            {
                return string.Empty;
            }

            return Encoding.UTF8.GetString(data[..dataRead]);
        }

        public static string ReadUtf8String(ServiceCtx context, int index = 0)
        {
            ulong position = context.Request.PtrBuff[index].Position;
            ulong size = context.Request.PtrBuff[index].Size;

            using RecyclableMemoryStream ms = MemoryStreamManager.Shared.GetStream();
            while (size-- > 0)
            {
                byte value = context.Memory.Read<byte>(position++);

                if (value == 0)
                {
                    break;
                }

                ms.WriteByte(value);
            }

            return Encoding.UTF8.GetString(ms.GetReadOnlySequence());
        }

        public static U8Span ReadUtf8Span(ServiceCtx context, int index = 0)
        {
            ulong position = context.Request.PtrBuff[index].Position;
            ulong size = context.Request.PtrBuff[index].Size;

            ReadOnlySpan<byte> buffer = context.Memory.GetSpan(position, (int)size);

            return new U8Span(buffer);
        }

        public static string ReadUtf8StringSend(ServiceCtx context, int index = 0)
        {
            ulong position = context.Request.SendBuff[index].Position;
            ulong size = context.Request.SendBuff[index].Size;

            using RecyclableMemoryStream ms = MemoryStreamManager.Shared.GetStream();

            while (size-- > 0)
            {
                byte value = context.Memory.Read<byte>(position++);

                if (value == 0)
                {
                    break;
                }

                ms.WriteByte(value);
            }

            return Encoding.UTF8.GetString(ms.GetReadOnlySequence());
        }

        public static int CompareCStr(ReadOnlySpan<byte> s1, ReadOnlySpan<byte> s2)
        {
            int s1Index = 0;
            int s2Index = 0;

            while (s1[s1Index] != 0 && s2[s2Index] != 0 && s1[s1Index] == s2[s2Index])
            {
                s1Index += 1;
                s2Index += 1;
            }

            return s2[s2Index] - s1[s1Index];
        }

        public static int LengthCstr(ReadOnlySpan<byte> s)
        {
            int i = 0;

            while (s[i] != 0)
            {
                i++;
            }

            return i;
        }
    }
}

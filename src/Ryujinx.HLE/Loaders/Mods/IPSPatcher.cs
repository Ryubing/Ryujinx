using Ryujinx.Common.Logging;
using System;
using System.IO;

namespace Ryujinx.HLE.Loaders.Mods
{
    class IpsPatcher
    {
        readonly MemPatch _patches;

        public IpsPatcher(BinaryReader reader)
        {
            _patches = ParseIps(reader);
            if (_patches != null)
            {
                Logger.Info?.Print(LogClass.ModLoader, "IPS patch loaded successfully");
            }
        }

        private static MemPatch ParseIps(BinaryReader reader)
        {
            ReadOnlySpan<byte> ipsHeaderMagic = "PATCH"u8;
            ReadOnlySpan<byte> ipsTailMagic = "EOF"u8;
            ReadOnlySpan<byte> ips32HeaderMagic = "IPS32"u8;
            ReadOnlySpan<byte> ips32TailMagic = "EEOF"u8;

            MemPatch patches = new();
            Span<byte> header = reader.ReadBytes(ipsHeaderMagic.Length).AsSpan();

            if (header.Length != ipsHeaderMagic.Length)
            {
                return null;
            }

            bool is32;
            ReadOnlySpan<byte> tailSpan;

            if (header.SequenceEqual(ipsHeaderMagic))
            {
                is32 = false;
                tailSpan = ipsTailMagic;
            }
            else if (header.SequenceEqual(ips32HeaderMagic))
            {
                is32 = true;
                tailSpan = ips32TailMagic;
            }
            else
            {
                return null;
            }

            byte[] buf = new byte[tailSpan.Length];

            bool ReadNext(int size) => reader.Read(buf, 0, size) != size;

            while (true)
            {
                if (ReadNext(buf.Length))
                {
                    return null;
                }

                if (buf.AsSpan().SequenceEqual(tailSpan))
                {
                    break;
                }

                int patchOffset = is32 ? buf[0] << 24 | buf[1] << 16 | buf[2] << 8 | buf[3]
                                       : buf[0] << 16 | buf[1] << 8 | buf[2];

                if (ReadNext(2))
                {
                    return null;
                }

                int patchSize = buf[0] << 8 | buf[1];

                if (patchSize == 0) // RLE/Fill mode
                {
                    if (ReadNext(2))
                    {
                        return null;
                    }

                    int fillLength = buf[0] << 8 | buf[1];

                    if (ReadNext(1))
                    {
                        return null;
                    }

                    patches.AddFill((uint)patchOffset, fillLength, buf[0]);
                }
                else // Copy mode
                {
                    byte[] patch = reader.ReadBytes(patchSize);

                    if (patch.Length != patchSize)
                    {
                        return null;
                    }

                    patches.Add((uint)patchOffset, patch);
                }
            }

            return patches;
        }

        public void AddPatches(MemPatch patches)
        {
            patches.AddFrom(_patches);
        }
    }
}

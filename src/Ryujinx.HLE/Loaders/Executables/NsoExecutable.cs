using LibHac.Common.FixedArrays;
using LibHac.Fs;
using LibHac.Loader;
using LibHac.Tools.FsSystem;
using Ryujinx.Common.Helper;
using Ryujinx.Common.Logging;
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Ryujinx.HLE.Loaders.Executables
{
    partial class NsoExecutable : IExecutable
    {
        public byte[] Program { get; }
        public Span<byte> Text => Program.AsSpan((int)TextOffset, (int)TextSize);
        public Span<byte> Ro => Program.AsSpan((int)RoOffset, (int)RoSize);
        public Span<byte> Data => Program.AsSpan((int)DataOffset, (int)DataSize);

        public uint TextOffset { get; }
        public uint RoOffset { get; }
        public uint DataOffset { get; }
        public uint BssOffset => DataOffset + (uint)Data.Length;

        public uint TextSize { get; }
        public uint RoSize { get; }
        public uint DataSize { get; }
        public uint BssSize { get; }

        public string Name;
        public Array32<byte> BuildId;

        public NsoExecutable(IStorage inStorage, string name = null)
        {
            NsoReader reader = new();

            reader.Initialize(inStorage.AsFile(OpenMode.Read)).ThrowIfFailure();

            TextOffset = reader.Header.Segments[0].MemoryOffset;
            RoOffset = reader.Header.Segments[1].MemoryOffset;
            DataOffset = reader.Header.Segments[2].MemoryOffset;
            BssSize = reader.Header.BssSize;

            reader.GetSegmentSize(NsoReader.SegmentType.Data, out uint uncompressedSize).ThrowIfFailure();

            Program = new byte[DataOffset + uncompressedSize];

            TextSize = DecompressSection(reader, NsoReader.SegmentType.Text, TextOffset);
            RoSize = DecompressSection(reader, NsoReader.SegmentType.Ro, RoOffset);
            DataSize = DecompressSection(reader, NsoReader.SegmentType.Data, DataOffset);

            Name = name;
            BuildId = reader.Header.ModuleId;

            PrintRoSectionInfo();
        }

        private uint DecompressSection(NsoReader reader, NsoReader.SegmentType segmentType, uint offset)
        {
            reader.GetSegmentSize(segmentType, out uint uncompressedSize).ThrowIfFailure();

            Span<byte> span = Program.AsSpan((int)offset, (int)uncompressedSize);

            reader.ReadSegment(segmentType, span).ThrowIfFailure();

            return uncompressedSize;
        }

        private void PrintRoSectionInfo()
        {
            string rawTextBuffer = Encoding.ASCII.GetString(Ro);
            StringBuilder stringBuilder = new();

            string modulePath = null;

            if (BitConverter.ToInt32(Ro[..4]) == 0)
            {
                int length = BitConverter.ToInt32(Ro.Slice(4, 4));
                if (length > 0)
                {
                    modulePath = Encoding.UTF8.GetString(Ro.Slice(8, length));
                }
            }

            if (string.IsNullOrEmpty(modulePath))
            {
                Match moduleMatch = Patterns.Module.Match(rawTextBuffer);
                if (moduleMatch.Success)
                {
                    modulePath = moduleMatch.Value;
                }
            }

            stringBuilder.AppendLine($"    Module: {modulePath}");

            Match fsSdkMatch = Patterns.FsSdk.Match(rawTextBuffer);
            if (fsSdkMatch.Success)
            {
                stringBuilder.AppendLine($"    FS SDK Version: {fsSdkMatch.Value.Replace("sdk_version: ", string.Empty)}");
            }

            MatchCollection sdkMwMatches = Patterns.SdkMw.Matches(rawTextBuffer);
            if (sdkMwMatches.Count != 0)
            {
                string libHeader = "    SDK Libraries: ";
                string libContent = string.Join($"\n{new string(' ', libHeader.Length)}", sdkMwMatches);

                stringBuilder.AppendLine($"{libHeader}{libContent}");
            }

            if (stringBuilder.Length > 0)
            {
                Logger.Info?.Print(LogClass.Loader, $"{Name}:\n{stringBuilder.ToString().TrimEnd('\r', '\n')}");
            }
        }
    }
}

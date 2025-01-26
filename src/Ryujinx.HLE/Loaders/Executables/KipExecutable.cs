using LibHac.Common;
using LibHac.Fs;
using LibHac.Kernel;
using System;

namespace Ryujinx.HLE.Loaders.Executables
{
    class KipExecutable : IExecutable
    {
        public byte[] Program { get; }
        public Span<byte> Text => Program.AsSpan((int)TextOffset, (int)TextSize);
        public Span<byte> Ro => Program.AsSpan((int)RoOffset, (int)RoSize);
        public Span<byte> Data => Program.AsSpan((int)DataOffset, (int)DataSize);

        public uint TextOffset { get; }
        public uint RoOffset { get; }
        public uint DataOffset { get; }
        public uint BssOffset { get; }

        public uint TextSize { get; }
        public uint RoSize { get; }
        public uint DataSize { get; }
        public uint BssSize { get; }

        public uint[] Capabilities { get; }
        public bool UsesSecureMemory { get; }
        public bool Is64BitAddressSpace { get; }
        public bool Is64Bit { get; }
        public ulong ProgramId { get; }
        public byte Priority { get; }
        public int StackSize { get; }
        public byte IdealCoreId { get; }
        public int Version { get; }
        public string Name { get; }

        public KipExecutable(in SharedRef<IStorage> inStorage)
        {
            KipReader reader = new();

            reader.Initialize(in inStorage).ThrowIfFailure();

            TextOffset = (uint)reader.Segments[0].MemoryOffset;
            RoOffset = (uint)reader.Segments[1].MemoryOffset;
            DataOffset = (uint)reader.Segments[2].MemoryOffset;
            BssOffset = (uint)reader.Segments[3].MemoryOffset;
            BssSize = (uint)reader.Segments[3].Size;

            StackSize = reader.StackSize;

            UsesSecureMemory = reader.UsesSecureMemory;
            Is64BitAddressSpace = reader.Is64BitAddressSpace;
            Is64Bit = reader.Is64Bit;

            ProgramId = reader.ProgramId;
            Priority = reader.Priority;
            IdealCoreId = reader.IdealCoreId;
            Version = reader.Version;
            Name = reader.Name.ToString();

            Capabilities = new uint[32];

            for (int index = 0; index < Capabilities.Length; index++)
            {
                Capabilities[index] = reader.Capabilities[index];
            }

            reader.GetSegmentSize(KipReader.SegmentType.Data, out int uncompressedSize).ThrowIfFailure();
            Program = new byte[DataOffset + uncompressedSize];

            TextSize = DecompressSection(reader, KipReader.SegmentType.Text, TextOffset, Program);
            RoSize = DecompressSection(reader, KipReader.SegmentType.Ro, RoOffset, Program);
            DataSize = DecompressSection(reader, KipReader.SegmentType.Data, DataOffset, Program);
        }

        private static uint DecompressSection(KipReader reader, KipReader.SegmentType segmentType, uint offset, byte[] program)
        {
            reader.GetSegmentSize(segmentType, out int uncompressedSize).ThrowIfFailure();

            Span<byte> span = program.AsSpan((int)offset, uncompressedSize);

            reader.ReadSegment(segmentType, span).ThrowIfFailure();

            return (uint)uncompressedSize;
        }
    }
}

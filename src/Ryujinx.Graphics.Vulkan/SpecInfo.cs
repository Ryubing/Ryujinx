using Silk.NET.Vulkan;
using System;

namespace Ryujinx.Graphics.Vulkan
{
    public enum SpecConstType
    {
        Bool32,
        Int16,
        Int32,
        Int64,
        Float16,
        Float32,
        Float64,
    }

    sealed class SpecDescription
    {
        public readonly SpecializationInfo Info;
        public readonly SpecializationMapEntry[] Map;

        // For mapping a simple packed struct or single entry
        public SpecDescription(params (uint Id, SpecConstType Type)[] description)
        {
            int count = description.Length;
            Map = new SpecializationMapEntry[count];

            uint structSize = 0;

            for (int i = 0; i < Map.Length; ++i)
            {
                uint typeSize = SizeOf(description[i].Type);
                Map[i] = new SpecializationMapEntry(description[i].Id, structSize, typeSize);
                structSize += typeSize;
            }

            Info = new SpecializationInfo
            {
                DataSize = structSize,
                MapEntryCount = (uint)count,
            };
        }

        // For advanced mapping with overlapping or staggered fields
        public SpecDescription(SpecializationMapEntry[] map)
        {
            Map = map;

            uint structSize = 0;
            for (int i = 0; i < map.Length; ++i)
            {
                structSize = Math.Max(structSize, map[i].Offset + (uint)map[i].Size);
            }

            Info = new SpecializationInfo
            {
                DataSize = structSize,
                MapEntryCount = (uint)map.Length,
            };
        }

        private static uint SizeOf(SpecConstType type) => type switch
        {
            SpecConstType.Int16 or SpecConstType.Float16 => 2,
            SpecConstType.Bool32 or SpecConstType.Int32 or SpecConstType.Float32 => 4,
            SpecConstType.Int64 or SpecConstType.Float64 => 8,
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };

        private SpecDescription()
        {
            Info = new();
        }

        public static readonly SpecDescription Empty = new();
    }

    readonly struct SpecData : IRefEquatable<SpecData>
    {
        private readonly byte[] _data;
        private readonly int _hash;

        public int Length => _data.Length;
        public ReadOnlySpan<byte> Span => _data.AsSpan();
        public override int GetHashCode() => _hash;

        public SpecData(ReadOnlySpan<byte> data)
        {
            _data = new byte[data.Length];
            data.CopyTo(_data);

            HashCode hc = new();
            hc.AddBytes(data);
            _hash = hc.ToHashCode();
        }

        public override bool Equals(object obj) => obj is SpecData other && Equals(other);
        public bool Equals(ref SpecData other) => _data.AsSpan().SequenceEqual(other._data);
    }
}

using Gommon;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Common.Configuration
{
    [Flags]
    public enum DirtyHack : byte
    {
        Xc2MenuSoftlockFix = 1,
        ShaderTranslationDelay = 2
    }

    public readonly struct EnabledDirtyHack(DirtyHack hack, int value)
    {
        public DirtyHack Hack => hack;
        public int Value => value;
        
        
        
        public ulong Pack() => Raw.PackBitFields(PackedFormat);

        public static EnabledDirtyHack Unpack(ulong packedHack)
        {
            var unpackedFields = packedHack.UnpackBitFields(PackedFormat);
            if (unpackedFields is not [var hack, var value])
                throw new Exception("The unpack operation on the integer resulted in an invalid unpacked result.");
            
            return new EnabledDirtyHack((DirtyHack)hack, (int)value);
        }
        
        private uint[] Raw => [(uint)Hack, (uint)Value.CoerceAtLeast(0)];
        
        public static readonly byte[] PackedFormat = [8, 32];
    }

    public class DirtyHacks : Dictionary<DirtyHack, int>
    {
        public DirtyHacks(IEnumerable<EnabledDirtyHack> hacks) 
            => hacks.ForEach(edh => Add(edh.Hack, edh.Value));

        public DirtyHacks(ulong[] packedHacks) : this(packedHacks.Select(EnabledDirtyHack.Unpack)) {}

        public ulong[] PackEntries() 
            => Entries.Select(it => it.Pack()).ToArray();
        
        public EnabledDirtyHack[] Entries 
            => this
                .Select(it => new EnabledDirtyHack(it.Key, it.Value))
                .ToArray();

        public static implicit operator DirtyHacks(EnabledDirtyHack[] hacks) => new(hacks);
        public static implicit operator DirtyHacks(ulong[] packedHacks) => new(packedHacks);

        public new int this[DirtyHack hack] => TryGetValue(hack, out var value) ? value : -1;

        public bool IsEnabled(DirtyHack hack) => ContainsKey(hack);
    }
}

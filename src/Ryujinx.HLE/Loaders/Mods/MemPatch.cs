using Ryujinx.Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.HLE.Loaders.Mods
{
    public class MemPatch
    {
        readonly Dictionary<uint, byte[]> _patches = new();

        /// <summary>
        /// Adds a patch to specified offset. Overwrites if already present. 
        /// </summary>
        /// <param name="offset">Memory offset</param>
        /// <param name="patch">The patch to add</param>
        public void Add(uint offset, byte[] patch)
        {
            _patches[offset] = patch;
        }

        /// <summary>
        /// Adds a patch in the form of an RLE (Fill mode).
        /// </summary>
        /// <param name="offset">Memory offset</param>
        /// <param name="length"The fill length</param>
        /// <param name="filler">The byte to fill</param>
        public void AddFill(uint offset, int length, byte filler)
        {
            // TODO: Can be made space efficient by changing `_patches`
            // Should suffice for now
            byte[] patch = new byte[length];
            patch.AsSpan().Fill(filler);

            _patches[offset] = patch;
        }

        /// <summary>
        /// Adds all patches from an existing MemPatch
        /// </summary>
        /// <param name="patches">The patches to add</param>
        public void AddFrom(MemPatch patches)
        {
            if (patches == null)
            {
                return;
            }

            foreach ((uint patchOffset, byte[] patch) in patches._patches)
            {
                _patches[patchOffset] = patch;
            }
        }

        /// <summary>
        /// Applies all the patches added to this instance.
        /// </summary>
        /// <remarks>
        /// Patches are applied in ascending order of offsets to guarantee
        /// overlapping patches always apply the same way.
        /// </remarks>
        /// <param name="memory">The span of bytes to patch</param>
        /// <param name="maxSize">The maximum size of the slice of patchable memory</param>
        /// <param name="protectedOffset">A secondary offset used in special cases (NSO header)</param>
        /// <returns>Successful patches count</returns>
        public int Patch(Span<byte> memory, int protectedOffset = 0)
        {
            int count = 0;
            foreach ((uint offset, byte[] patch) in _patches.OrderBy(item => item.Key))
            {
                int patchOffset = (int)offset;
                int patchSize = patch.Length;

                if (patchOffset < protectedOffset)
                {
                    Logger.Warning?.Print(LogClass.ModLoader, $"Attempted to patch protected memory ({patchOffset:x} is within protected boundary of {protectedOffset:x}).");
                    continue;
                }
                
                if (patchOffset > memory.Length)
                {
                    Logger.Warning?.Print(LogClass.ModLoader, $"Attempted to patch out of bounds memory (offset {patchOffset} ({patchOffset:x}) exceeds memory buffer length {memory.Length}).");
                    continue;
                }

                patchOffset -= protectedOffset;

                if (patchOffset + patchSize > memory.Length)
                {
                    Logger.Warning?.Print(LogClass.ModLoader, $"Patch offset ({patchOffset:x}) + size ({patchSize}) is greater than the size of the memory buffer ({memory.Length}). Attempting to fix this...");
                    patchSize = memory.Length - patchOffset;
                }

                Logger.Info?.Print(LogClass.ModLoader, $"Patching address offset {patchOffset:x} <= {BitConverter.ToString(patch).Replace('-', ' ')} len={patchSize}");

                patch.AsSpan(0, patchSize).CopyTo(memory.Slice(patchOffset, patchSize));

                count++;
            }

            return count;
        }
    }
}

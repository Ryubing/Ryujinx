using Ryujinx.Common;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Gpu.Engine.Threed;
using Ryujinx.Graphics.Gpu.Engine.Twod;
using Ryujinx.Graphics.Gpu.Engine.Types;
using Ryujinx.Graphics.Gpu.Memory;
using Ryujinx.Graphics.Texture;
using Ryujinx.Memory.Range;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Ryujinx.Graphics.Gpu.Image
{
    /// <summary>
    /// Texture cache.
    /// </summary>
    class TextureCache : IDisposable
    {
        private readonly struct OverlapInfo
        {
            public TextureViewCompatibility Compatibility { get; }
            public int FirstLayer { get; }
            public int FirstLevel { get; }

            public OverlapInfo(TextureViewCompatibility compatibility, int firstLayer, int firstLevel)
            {
                Compatibility = compatibility;
                FirstLayer = firstLayer;
                FirstLevel = firstLevel;
            }
        }

        private const int OverlapsBufferInitialCapacity = 10;
        private const int OverlapsBufferMaxCapacity = 10000;

        private readonly GpuContext _context;
        private readonly PhysicalMemory _physicalMemory;

        private readonly MultiRangeList<Texture> _textures;
        private readonly HashSet<Texture> _partiallyMappedTextures;

        private readonly ReaderWriterLockSlim _texturesLock;

        private Texture[] _textureOverlaps;
        private OverlapInfo[] _overlapInfo;

        private readonly AutoDeleteCache _cache;

        /// <summary>
        /// Constructs a new instance of the texture manager.
        /// </summary>
        /// <param name="context">The GPU context that the texture manager belongs to</param>
        /// <param name="physicalMemory">Physical memory where the textures managed by this cache are mapped</param>
        public TextureCache(GpuContext context, PhysicalMemory physicalMemory)
        {
            _context = context;
            _physicalMemory = physicalMemory;

            _textures = new MultiRangeList<Texture>();
            _partiallyMappedTextures = new HashSet<Texture>();

            _texturesLock = new ReaderWriterLockSlim();

            _textureOverlaps = new Texture[OverlapsBufferInitialCapacity];
            _overlapInfo = new OverlapInfo[OverlapsBufferInitialCapacity];

            _cache = new AutoDeleteCache();
        }

        /// <summary>
        /// Initializes the cache, setting the maximum texture capacity for the specified GPU context.
        /// </summary>
        /// <param name="cpuMemorySize">The amount of physical CPU Memory Avaiable on the device.</param>
        public void Initialize(ulong cpuMemorySize)
        {
            _cache.Initialize(_context, cpuMemorySize);
        }

        /// <summary>
        /// Handles marking of textures written to a memory region being (partially) remapped.
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        public void MemoryUnmappedHandler(object sender, UnmapEventArgs e)
        {
            Texture[] overlaps = new Texture[10];
            int overlapCount;

            MultiRange unmapped = ((MemoryManager)sender).GetPhysicalRegions(e.Address, e.Size);

            _texturesLock.EnterReadLock();

            try
            {
                overlapCount = _textures.FindOverlaps(unmapped, ref overlaps);
            }
            finally
            {
                _texturesLock.ExitReadLock();
            }

            if (overlapCount > 0)
            {
                for (int i = 0; i < overlapCount; i++)
                {
                    overlaps[i].Unmapped(unmapped);
                }
            }

            lock (_partiallyMappedTextures)
            {
                if (overlapCount > 0 || _partiallyMappedTextures.Count > 0)
                {
                    e.AddRemapAction(() =>
                    {
                        lock (_partiallyMappedTextures)
                        {
                            if (overlapCount > 0)
                            {
                                for (int i = 0; i < overlapCount; i++)
                                {
                                    _partiallyMappedTextures.Add(overlaps[i]);
                                }
                            }

                            // Any texture that has been unmapped at any point or is partially unmapped
                            // should update their pool references after the remap completes.

                            foreach (var texture in _partiallyMappedTextures)
                            {
                                texture.UpdatePoolMappings();
                            }
                        }
                    });
                }
            }
        }

        /// <summary>
        /// Determines if a given texture is eligible for upscaling from its info.
        /// </summary>
        /// <param name="info">The texture info to check</param>
        /// <param name="withUpscale">True if the user of the texture would prefer it to be upscaled immediately</param>
        /// <returns>True if eligible</returns>
        private static TextureScaleMode IsUpscaleCompatible(TextureInfo info, bool withUpscale)
        {
            if ((info.Target == Target.Texture2D || info.Target == Target.Texture2DArray || info.Target == Target.Texture2DMultisample) && !info.FormatInfo.IsCompressed)
            {
                return UpscaleSafeMode(info) ? (withUpscale ? TextureScaleMode.Scaled : TextureScaleMode.Eligible) : TextureScaleMode.Undesired;
            }

            return TextureScaleMode.Blacklisted;
        }

        /// <summary>
        /// Determines if a given texture is "safe" for upscaling from its info.
        /// Note that this is different from being compatible - this elilinates targets that would have detrimental effects when scaled.
        /// </summary>
        /// <param name="info">The texture info to check</param>
        /// <returns>True if safe</returns>
        private static bool UpscaleSafeMode(TextureInfo info)
        {
            // While upscaling works for all targets defined by IsUpscaleCompatible, we additionally blacklist targets here that
            // may have undesirable results (upscaling blur textures) or simply waste GPU resources (upscaling texture atlas).

            if (info.Levels > 3)
            {
                // Textures with more than 3 levels are likely to be game textures, rather than render textures.
                // Small textures with full mips are likely to be removed by the next check.
                return false;
            }

            if (info.Width < 8 || info.Height < 8)
            {
                // Discount textures with small dimensions.
                return false;
            }

            int widthAlignment = (info.IsLinear ? Constants.StrideAlignment : Constants.GobAlignment) / info.FormatInfo.BytesPerPixel;

            if (!(info.FormatInfo.Format.IsDepthOrStencil() || info.FormatInfo.Components == 1))
            {
                // Discount square textures that aren't depth-stencil like. (excludes game textures, cubemap faces, most 3D texture LUT, texture atlas)
                // Detect if the texture is possibly square. Widths may be aligned, so to remove the uncertainty we align both the width and height.

                bool possiblySquare = BitUtils.AlignUp(info.Width, widthAlignment) == BitUtils.AlignUp(info.Height, widthAlignment);

                if (possiblySquare)
                {
                    return false;
                }
            }

            if (info.Height < 360)
            {
                int aspectWidth = (int)MathF.Ceiling((info.Height / 9f) * 16f);
                int aspectMaxWidth = BitUtils.AlignUp(aspectWidth, widthAlignment);
                int aspectMinWidth = BitUtils.AlignDown(aspectWidth, widthAlignment);

                if (info.Width >= aspectMinWidth && info.Width <= aspectMaxWidth && info.Height < 360)
                {
                    // Targets that are roughly 16:9 can only be rescaled if they're equal to or above 360p. (excludes blur and bloom textures)
                    return false;
                }
            }

            if (info.Width == info.Height * info.Height)
            {
                // Possibly used for a "3D texture" drawn onto a 2D surface.
                // Some games do this to generate a tone mapping LUT without rendering into 3D texture slices.

                return false;
            }

            return true;
        }

        /// <summary>
        /// Lifts the texture to the top of the AutoDeleteCache. This is primarily used to enforce that
        /// data written to a target will be flushed to memory should the texture be deleted, but also
        /// keeps rendered textures alive without a pool reference.
        /// </summary>
        /// <param name="texture">Texture to lift</param>
        public void Lift(Texture texture)
        {
            _cache.Lift(texture);
        }

        /// <summary>
        /// Attempts to update a texture's physical memory range.
        /// Returns false if there is an existing texture that matches with the updated range.
        /// </summary>
        /// <param name="texture">Texture to update</param>
        /// <param name="range">New physical memory range</param>
        /// <returns>True if the mapping was updated, false otherwise</returns>
        public bool UpdateMapping(Texture texture, MultiRange range)
        {
            // There cannot be an existing texture compatible with this mapping in the texture cache already.
            int overlapCount;

            _texturesLock.EnterReadLock();

            try
            {
                overlapCount = _textures.FindOverlaps(range, ref _textureOverlaps);
            }
            finally
            {
                _texturesLock.ExitReadLock();
            }

            for (int i = 0; i < overlapCount; i++)
            {
                var other = _textureOverlaps[i];

                if (texture != other &&
                    (texture.IsViewCompatible(other.Info, other.Range, true, other.LayerSize, _context.Capabilities, out _, out _) != TextureViewCompatibility.Incompatible ||
                    other.IsViewCompatible(texture.Info, texture.Range, true, texture.LayerSize, _context.Capabilities, out _, out _) != TextureViewCompatibility.Incompatible))
                {
                    return false;
                }
            }

            _texturesLock.EnterWriteLock();

            try
            {
                _textures.Remove(texture);

                texture.ReplaceRange(range);

                _textures.Add(texture);
            }
            finally
            {
                _texturesLock.ExitWriteLock();
            }

            return true;
        }

        /// <summary>
        /// Tries to find an existing texture, or create a new one if not found.
        /// </summary>
        /// <param name="memoryManager">GPU memory manager where the texture is mapped</param>
        /// <param name="copyTexture">Copy texture to find or create</param>
        /// <param name="offset">Offset to be added to the physical texture address</param>
        /// <param name="formatInfo">Format information of the copy texture</param>
        /// <param name="depthAlias">Indicates if aliasing between color and depth format should be allowed</param>
        /// <param name="shouldCreate">Indicates if a new texture should be created if none is found on the cache</param>
        /// <param name="preferScaling">Indicates if the texture should be scaled from the start</param>
        /// <param name="sizeHint">A hint indicating the minimum used size for the texture</param>
        /// <returns>The texture</returns>
        public Texture FindOrCreateTexture(
            MemoryManager memoryManager,
            TwodTexture copyTexture,
            ulong offset,
            FormatInfo formatInfo,
            bool depthAlias,
            bool shouldCreate,
            bool preferScaling,
            Size sizeHint)
        {
            int gobBlocksInY = copyTexture.MemoryLayout.UnpackGobBlocksInY();
            int gobBlocksInZ = copyTexture.MemoryLayout.UnpackGobBlocksInZ();

            int width;

            if (copyTexture.LinearLayout)
            {
                width = copyTexture.Stride / formatInfo.BytesPerPixel;
            }
            else
            {
                width = copyTexture.Width;
            }

            TextureInfo info = new(
                copyTexture.Address.Pack() + offset,
                GetMinimumWidthInGob(width, sizeHint.Width, formatInfo.BytesPerPixel, copyTexture.LinearLayout),
                copyTexture.Height,
                copyTexture.Depth,
                1,
                1,
                1,
                copyTexture.Stride,
                copyTexture.LinearLayout,
                gobBlocksInY,
                gobBlocksInZ,
                1,
                Target.Texture2D,
                formatInfo);

            TextureSearchFlags flags = TextureSearchFlags.ForCopy;

            if (depthAlias)
            {
                flags |= TextureSearchFlags.DepthAlias;
            }

            if (preferScaling)
            {
                flags |= TextureSearchFlags.WithUpscale;
            }

            if (!shouldCreate)
            {
                flags |= TextureSearchFlags.NoCreate;
            }

            Texture texture = FindOrCreateTexture(memoryManager, flags, info, 0, sizeHint: sizeHint);

            texture?.SynchronizeMemory();

            return texture;
        }

        /// <summary>
        /// Tries to find an existing texture, or create a new one if not found.
        /// </summary>
        /// <param name="memoryManager">GPU memory manager where the texture is mapped</param>
        /// <param name="formatInfo">Format of the texture</param>
        /// <param name="gpuAddress">GPU virtual address of the texture</param>
        /// <param name="xCount">Texture width in bytes</param>
        /// <param name="yCount">Texture height</param>
        /// <param name="stride">Texture stride if linear, otherwise ignored</param>
        /// <param name="isLinear">Indicates if the texture is linear or block linear</param>
        /// <param name="gobBlocksInY">GOB blocks in Y for block linear textures</param>
        /// <param name="gobBlocksInZ">GOB blocks in Z for 3D block linear textures</param>
        /// <returns>The texture</returns>
        public Texture FindOrCreateTexture(
            MemoryManager memoryManager,
            FormatInfo formatInfo,
            ulong gpuAddress,
            int xCount,
            int yCount,
            int stride,
            bool isLinear,
            int gobBlocksInY,
            int gobBlocksInZ)
        {
            TextureInfo info = new(
                gpuAddress,
                xCount / formatInfo.BytesPerPixel,
                yCount,
                1,
                1,
                1,
                1,
                stride,
                isLinear,
                gobBlocksInY,
                gobBlocksInZ,
                1,
                Target.Texture2D,
                formatInfo);

            Texture texture = FindOrCreateTexture(memoryManager, TextureSearchFlags.ForCopy, info, 0, sizeHint: new Size(xCount, yCount, 1));

            texture?.SynchronizeMemory();

            return texture;
        }

        /// <summary>
        /// Tries to find an existing texture, or create a new one if not found.
        /// </summary>
        /// <param name="memoryManager">GPU memory manager where the texture is mapped</param>
        /// <param name="colorState">Color buffer texture to find or create</param>
        /// <param name="layered">Indicates if the texture might be accessed with a non-zero layer index</param>
        /// <param name="discard">Indicates that the sizeHint region's data will be overwritten</param>
        /// <param name="samplesInX">Number of samples in the X direction, for MSAA</param>
        /// <param name="samplesInY">Number of samples in the Y direction, for MSAA</param>
        /// <param name="sizeHint">A hint indicating the minimum used size for the texture</param>
        /// <returns>The texture</returns>
        public Texture FindOrCreateTexture(
            MemoryManager memoryManager,
            RtColorState colorState,
            bool layered,
            bool discard,
            int samplesInX,
            int samplesInY,
            Size sizeHint)
        {
            bool isLinear = colorState.MemoryLayout.UnpackIsLinear();

            int gobBlocksInY = colorState.MemoryLayout.UnpackGobBlocksInY();
            int gobBlocksInZ = colorState.MemoryLayout.UnpackGobBlocksInZ();

            Target target;

            if (colorState.MemoryLayout.UnpackIsTarget3D())
            {
                target = Target.Texture3D;
            }
            else if ((samplesInX | samplesInY) != 1)
            {
                target = colorState.Depth > 1 && layered
                    ? Target.Texture2DMultisampleArray
                    : Target.Texture2DMultisample;
            }
            else
            {
                target = colorState.Depth > 1 && layered
                    ? Target.Texture2DArray
                    : Target.Texture2D;
            }

            FormatInfo formatInfo = colorState.Format.Convert();

            int width, stride;

            // For linear textures, the width value is actually the stride.
            // We can easily get the width by dividing the stride by the bpp,
            // since the stride is the total number of bytes occupied by a
            // line. The stride should also meet alignment constraints however,
            // so the width we get here is the aligned width.
            if (isLinear)
            {
                width = colorState.WidthOrStride / formatInfo.BytesPerPixel;
                stride = colorState.WidthOrStride;
            }
            else
            {
                width = colorState.WidthOrStride;
                stride = 0;
            }

            TextureInfo info = new(
                colorState.Address.Pack(),
                GetMinimumWidthInGob(width, sizeHint.Width, formatInfo.BytesPerPixel, isLinear),
                colorState.Height,
                colorState.Depth,
                1,
                samplesInX,
                samplesInY,
                stride,
                isLinear,
                gobBlocksInY,
                gobBlocksInZ,
                1,
                target,
                formatInfo);

            int layerSize = !isLinear ? colorState.LayerSize * 4 : 0;

            var flags = TextureSearchFlags.WithUpscale;

            if (discard)
            {
                flags |= TextureSearchFlags.DiscardData;
            }

            Texture texture = FindOrCreateTexture(memoryManager, flags, info, layerSize, sizeHint: sizeHint);

            texture?.SynchronizeMemory();

            return texture;
        }

        /// <summary>
        /// Tries to find an existing texture, or create a new one if not found.
        /// </summary>
        /// <param name="memoryManager">GPU memory manager where the texture is mapped</param>
        /// <param name="dsState">Depth-stencil buffer texture to find or create</param>
        /// <param name="size">Size of the depth-stencil texture</param>
        /// <param name="layered">Indicates if the texture might be accessed with a non-zero layer index</param>
        /// <param name="discard">Indicates that the sizeHint region's data will be overwritten</param>
        /// <param name="samplesInX">Number of samples in the X direction, for MSAA</param>
        /// <param name="samplesInY">Number of samples in the Y direction, for MSAA</param>
        /// <param name="sizeHint">A hint indicating the minimum used size for the texture</param>
        /// <returns>The texture</returns>
        public Texture FindOrCreateTexture(
            MemoryManager memoryManager,
            RtDepthStencilState dsState,
            Size3D size,
            bool layered,
            bool discard,
            int samplesInX,
            int samplesInY,
            Size sizeHint)
        {
            int gobBlocksInY = dsState.MemoryLayout.UnpackGobBlocksInY();
            int gobBlocksInZ = dsState.MemoryLayout.UnpackGobBlocksInZ();

            layered &= size.UnpackIsLayered();

            Target target;

            if ((samplesInX | samplesInY) != 1)
            {
                target = size.Depth > 1 && layered
                    ? Target.Texture2DMultisampleArray
                    : Target.Texture2DMultisample;
            }
            else
            {
                target = size.Depth > 1 && layered
                    ? Target.Texture2DArray
                    : Target.Texture2D;
            }

            FormatInfo formatInfo = dsState.Format.Convert();

            TextureInfo info = new(
                dsState.Address.Pack(),
                GetMinimumWidthInGob(size.Width, sizeHint.Width, formatInfo.BytesPerPixel, false),
                size.Height,
                size.Depth,
                1,
                samplesInX,
                samplesInY,
                0,
                false,
                gobBlocksInY,
                gobBlocksInZ,
                1,
                target,
                formatInfo);

            var flags = TextureSearchFlags.WithUpscale;

            if (discard)
            {
                flags |= TextureSearchFlags.DiscardData;
            }

            Texture texture = FindOrCreateTexture(memoryManager, flags, info, dsState.LayerSize * 4, sizeHint: sizeHint);

            texture?.SynchronizeMemory();

            return texture;
        }

        /// <summary>
        /// For block linear textures, gets the minimum width of the texture
        /// that would still have the same number of GOBs per row as the original width.
        /// </summary>
        /// <param name="width">The possibly aligned texture width</param>
        /// <param name="minimumWidth">The minimum width that the texture may have without losing data</param>
        /// <param name="bytesPerPixel">Bytes per pixel of the texture format</param>
        /// <param name="isLinear">True if the texture is linear, false for block linear</param>
        /// <returns>The minimum width of the texture with the same amount of GOBs per row</returns>
        private static int GetMinimumWidthInGob(int width, int minimumWidth, int bytesPerPixel, bool isLinear)
        {
            if (isLinear || (uint)minimumWidth >= (uint)width)
            {
                return width;
            }

            // Calculate the minimum possible that would not cause data loss
            // and would be still within the same GOB (aligned size would be the same).
            // This is useful for render and copy operations, where we don't know the
            // exact width of the texture, but it doesn't matter, as long the texture is
            // at least as large as the region being rendered or copied.

            int alignment = 64 / bytesPerPixel;
            int widthAligned = BitUtils.AlignUp(width, alignment);

            return Math.Clamp(widthAligned - alignment + 1, minimumWidth, widthAligned);
        }

        /// <summary>
        /// Determines if texture data should be fully discarded
        /// based on the size hint region and whether it is set to be discarded.
        /// </summary>
        /// <param name="discard">Whether the size hint region should be discarded</param>
        /// <param name="texture">The texture being discarded</param>
        /// <param name="sizeHint">A hint indicating the minimum used size for the texture</param>
        /// <returns>True if the data should be discarded, false otherwise</returns>
        private static bool ShouldDiscard(bool discard, Texture texture, Size? sizeHint)
        {
            return discard &&
                texture.Info.DepthOrLayers == 1 &&
                sizeHint != null &&
                texture.Width <= sizeHint.Value.Width &&
                texture.Height <= sizeHint.Value.Height;
        }

        /// <summary>
        /// Discards texture data if requested and possible.
        /// </summary>
        /// <param name="discard">Whether the size hint region should be discarded</param>
        /// <param name="texture">The texture being discarded</param>
        /// <param name="sizeHint">A hint indicating the minimum used size for the texture</param>
        private static void DiscardIfNeeded(bool discard, Texture texture, Size? sizeHint)
        {
            if (ShouldDiscard(discard, texture, sizeHint))
            {
                texture.DiscardData();
            }
        }

        /// <summary>
        /// Tries to find an existing texture, or create a new one if not found.
        /// </summary>
        /// <param name="memoryManager">GPU memory manager where the texture is mapped</param>
        /// <param name="flags">The texture search flags, defines texture comparison rules</param>
        /// <param name="info">Texture information of the texture to be found or created</param>
        /// <param name="layerSize">Size in bytes of a single texture layer</param>
        /// <param name="sizeHint">A hint indicating the minimum used size for the texture</param>
        /// <param name="range">Optional ranges of physical memory where the texture data is located</param>
        /// <returns>The texture</returns>
        public Texture FindOrCreateTexture(
            MemoryManager memoryManager,
            TextureSearchFlags flags,
            TextureInfo info,
            int layerSize = 0,
            Size? sizeHint = null,
            MultiRange? range = null)
        {
            bool isSamplerTexture = (flags & TextureSearchFlags.ForSampler) != 0;
            bool discard = (flags & TextureSearchFlags.DiscardData) != 0;

            TextureScaleMode scaleMode = IsUpscaleCompatible(info, (flags & TextureSearchFlags.WithUpscale) != 0);

            ulong address;

            if (range != null)
            {
                address = range.Value.GetSubRange(0).Address;
            }
            else
            {
                address = memoryManager.Translate(info.GpuAddress);

                // If the start address is unmapped, let's try to find a page of memory that is mapped.
                if (address == MemoryManager.PteUnmapped)
                {
                    // Make sure that the dimensions are valid before calculating the texture size.
                    if (info.Width < 1 || info.Height < 1 || info.Levels < 1)
                    {
                        return null;
                    }

                    if ((info.Target == Target.Texture3D ||
                         info.Target == Target.Texture2DArray ||
                         info.Target == Target.Texture2DMultisampleArray ||
                         info.Target == Target.CubemapArray) && info.DepthOrLayers < 1)
                    {
                        return null;
                    }

                    ulong dataSize = (ulong)info.CalculateSizeInfo(layerSize).TotalSize;

                    address = memoryManager.TranslateFirstMapped(info.GpuAddress, dataSize);
                }

                // If address is still invalid, the texture is fully unmapped, so it has no data, just return null.
                if (address == MemoryManager.PteUnmapped)
                {
                    return null;
                }
            }

            int sameAddressOverlapsCount;

            _texturesLock.EnterReadLock();

            try
            {
                // Try to find a perfect texture match, with the same address and parameters.
                sameAddressOverlapsCount = _textures.FindOverlaps(address, ref _textureOverlaps);
            }
            finally
            {
                _texturesLock.ExitReadLock();
            }

            Texture texture = null;

            long bestSequence = 0;

            for (int index = 0; index < sameAddressOverlapsCount; index++)
            {
                Texture overlap = _textureOverlaps[index];

                TextureMatchQuality matchQuality = overlap.IsExactMatch(info, flags);

                if (matchQuality != TextureMatchQuality.NoMatch)
                {
                    // If the parameters match, we need to make sure the texture is mapped to the same memory regions.
                    if (range != null)
                    {
                        // If a range of memory was supplied, just check if the ranges match.
                        if (!overlap.Range.Equals(range.Value))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        // If no range was supplied, we can check if the GPU virtual address match. If they do,
                        // we know the textures are located at the same memory region.
                        // If they don't, it may still be mapped to the same physical region, so we
                        // do a more expensive check to tell if they are mapped into the same physical regions.
                        // If the GPU VA for the texture has ever been unmapped, then the range must be checked regardless.
                        if ((overlap.Info.GpuAddress != info.GpuAddress || overlap.ChangedMapping) &&
                            !memoryManager.CompareRange(overlap.Range, info.GpuAddress))
                        {
                            continue;
                        }
                    }

                    if (texture == null || overlap.Group.ModifiedSequence - bestSequence > 0)
                    {
                        texture = overlap;
                        bestSequence = overlap.Group.ModifiedSequence;
                    }
                }
            }

            if (texture != null)
            {
                DiscardIfNeeded(discard, texture, sizeHint);

                texture.SynchronizeMemory();

                return texture;
            }
            else if (flags.HasFlag(TextureSearchFlags.NoCreate))
            {
                return null;
            }

            // Calculate texture sizes, used to find all overlapping textures.
            SizeInfo sizeInfo = info.CalculateSizeInfo(layerSize);

            ulong size = (ulong)sizeInfo.TotalSize;
            bool partiallyMapped = false;

            if (range == null)
            {
                range = memoryManager.GetPhysicalRegions(info.GpuAddress, size);

                for (int i = 0; i < range.Value.Count; i++)
                {
                    if (range.Value.GetSubRange(i).Address == MemoryManager.PteUnmapped)
                    {
                        partiallyMapped = true;
                        break;
                    }
                }
            }

            // Find view compatible matches.
            int overlapsCount = 0;

            if (info.Target != Target.TextureBuffer)
            {
                _texturesLock.EnterReadLock();

                try
                {
                    overlapsCount = _textures.FindOverlaps(range.Value, ref _textureOverlaps);
                }
                finally
                {
                    _texturesLock.ExitReadLock();
                }
            }

            if (_overlapInfo.Length != _textureOverlaps.Length)
            {
                Array.Resize(ref _overlapInfo, _textureOverlaps.Length);
            }

            // =============== Find Texture View of Existing Texture ===============

            int fullyCompatible = 0;

            // Evaluate compatibility of overlaps, add temporary references
            int preferredOverlap = -1;

            for (int index = 0; index < overlapsCount; index++)
            {
                Texture overlap = _textureOverlaps[index];
                TextureViewCompatibility overlapCompatibility = overlap.IsViewCompatible(
                    info,
                    range.Value,
                    isSamplerTexture,
                    sizeInfo.LayerSize,
                    _context.Capabilities,
                    out int firstLayer,
                    out int firstLevel,
                    flags);

                if (overlapCompatibility >= TextureViewCompatibility.FormatAlias)
                {
                    if (overlap.IsView)
                    {
                        overlapCompatibility = TextureViewCompatibility.CopyOnly;
                    }
                    else
                    {
                        fullyCompatible++;

                        if (preferredOverlap == -1 || overlap.Group.ModifiedSequence - bestSequence > 0)
                        {
                            preferredOverlap = index;
                            bestSequence = overlap.Group.ModifiedSequence;
                        }
                    }
                }

                _overlapInfo[index] = new OverlapInfo(overlapCompatibility, firstLayer, firstLevel);
                overlap.IncrementReferenceCount();
            }

            // Search through the overlaps to find a compatible view and establish any copy dependencies.

            if (preferredOverlap != -1)
            {
                Texture overlap = _textureOverlaps[preferredOverlap];
                OverlapInfo oInfo = _overlapInfo[preferredOverlap];

                bool aliased = oInfo.Compatibility == TextureViewCompatibility.FormatAlias;

                if (!isSamplerTexture)
                {
                    // If this is not a sampler texture, the size might be different from the requested size,
                    // so we need to make sure the texture information has the correct size for this base texture,
                    // before creating the view.

                    info = info.CreateInfoForLevelView(overlap, oInfo.FirstLevel, aliased);
                }
                else if (aliased)
                {
                    // The format must be changed to match the parent.
                    info = info.CreateInfoWithFormat(overlap.Info.FormatInfo);
                }

                texture = overlap.CreateView(info, sizeInfo, range.Value, oInfo.FirstLayer, oInfo.FirstLevel);
                texture.SynchronizeMemory();
            }
            else
            {
                for (int index = 0; index < overlapsCount; index++)
                {
                    Texture overlap = _textureOverlaps[index];
                    OverlapInfo oInfo = _overlapInfo[index];

                    if (oInfo.Compatibility == TextureViewCompatibility.CopyOnly && fullyCompatible == 0)
                    {
                        // Only copy compatible. If there's another choice for a FULLY compatible texture, choose that instead.

                        texture = new Texture(_context, _physicalMemory, info, sizeInfo, range.Value, scaleMode);

                        // If the new texture is larger than the existing one, we need to fill the remaining space with CPU data,
                        // otherwise we only need the data that is copied from the existing texture, without loading the CPU data.
                        bool updateNewTexture = texture.Width > overlap.Width || texture.Height > overlap.Height;

                        texture.InitializeGroup(true, true, new List<TextureIncompatibleOverlap>());
                        texture.InitializeData(false, updateNewTexture);

                        overlap.SynchronizeMemory();
                        overlap.CreateCopyDependency(texture, oInfo.FirstLayer, oInfo.FirstLevel, true);
                        break;
                    }
                }
            }

            if (texture != null)
            {
                // This texture could be a view of multiple parent textures with different storages, even if it is a view.
                // When a texture is created, make sure all possible dependencies to other textures are created as copies.
                // (even if it could be fulfilled without a copy)

                for (int index = 0; index < overlapsCount; index++)
                {
                    Texture overlap = _textureOverlaps[index];
                    OverlapInfo oInfo = _overlapInfo[index];

                    if (oInfo.Compatibility <= TextureViewCompatibility.LayoutIncompatible)
                    {
                        if (!overlap.IsView && texture.DataOverlaps(overlap, oInfo.Compatibility))
                        {
                            texture.Group.RegisterIncompatibleOverlap(new TextureIncompatibleOverlap(overlap.Group, oInfo.Compatibility), true);
                        }
                    }
                    else if (overlap.Group != texture.Group)
                    {
                        overlap.SynchronizeMemory();
                        overlap.CreateCopyDependency(texture, oInfo.FirstLayer, oInfo.FirstLevel, true);
                    }
                }

                texture.SynchronizeMemory();
            }

            // =============== Create a New Texture ===============

            // No match, create a new texture.
            if (texture == null)
            {
                texture = new Texture(_context, _physicalMemory, info, sizeInfo, range.Value, scaleMode);

                // Step 1: Find textures that are view compatible with the new texture.
                // Any textures that are incompatible will contain garbage data, so they should be removed where possible.

                int viewCompatible = 0;
                fullyCompatible = 0;
                bool setData = isSamplerTexture || overlapsCount == 0 || flags.HasFlag(TextureSearchFlags.ForCopy);

                bool hasLayerViews = false;
                bool hasMipViews = false;

                var incompatibleOverlaps = new List<TextureIncompatibleOverlap>();

                for (int index = 0; index < overlapsCount; index++)
                {
                    Texture overlap = _textureOverlaps[index];
                    bool overlapInCache = overlap.CacheNode != null;

                    TextureViewCompatibility compatibility = texture.IsViewCompatible(
                        overlap.Info,
                        overlap.Range,
                        exactSize: true,
                        overlap.LayerSize,
                        _context.Capabilities,
                        out int firstLayer,
                        out int firstLevel);

                    if (overlap.IsView && compatibility == TextureViewCompatibility.Full)
                    {
                        compatibility = TextureViewCompatibility.CopyOnly;
                    }

                    if (compatibility > TextureViewCompatibility.LayoutIncompatible)
                    {
                        _overlapInfo[viewCompatible] = new OverlapInfo(compatibility, firstLayer, firstLevel);
                        _textureOverlaps[index] = _textureOverlaps[viewCompatible];
                        _textureOverlaps[viewCompatible] = overlap;

                        if (compatibility == TextureViewCompatibility.Full)
                        {
                            if (viewCompatible != fullyCompatible)
                            {
                                // Swap overlaps so that the fully compatible views have priority.

                                _overlapInfo[viewCompatible] = _overlapInfo[fullyCompatible];
                                _textureOverlaps[viewCompatible] = _textureOverlaps[fullyCompatible];

                                _overlapInfo[fullyCompatible] = new OverlapInfo(compatibility, firstLayer, firstLevel);
                                _textureOverlaps[fullyCompatible] = overlap;
                            }

                            fullyCompatible++;
                        }

                        viewCompatible++;

                        hasLayerViews |= overlap.Info.GetSlices() < texture.Info.GetSlices();
                        hasMipViews |= overlap.Info.Levels < texture.Info.Levels;
                    }
                    else
                    {
                        bool dataOverlaps = texture.DataOverlaps(overlap, compatibility);

                        if (!overlap.IsView && dataOverlaps && !incompatibleOverlaps.Any(incompatible => incompatible.Group == overlap.Group))
                        {
                            incompatibleOverlaps.Add(new TextureIncompatibleOverlap(overlap.Group, compatibility));
                        }

                        bool removeOverlap;
                        bool modified = overlap.CheckModified(false);

                        if (overlapInCache || !setData)
                        {
                            if (!dataOverlaps)
                            {
                                // Allow textures to overlap if their data does not actually overlap.
                                // This typically happens when mip level subranges of a layered texture are used. (each texture fills the gaps of the others)
                                continue;
                            }

                            // The overlap texture is going to contain garbage data after we draw, or is generally incompatible.
                            // The texture group will obtain copy dependencies for any subresources that are compatible between the two textures,
                            // but sometimes its data must be flushed regardless.

                            // If the texture was modified since its last use, then that data is probably meant to go into this texture.
                            // If the data has been modified by the CPU, then it also shouldn't be flushed.

                            bool flush = overlapInCache && !modified && overlap.AlwaysFlushOnOverlap;

                            setData |= modified || flush;

                            if (overlapInCache)
                            {
                                if (flush || overlap.HadPoolOwner || overlap.IsView)
                                {
                                    _cache.Remove(overlap, flush);
                                }
                                else
                                {
                                    // This texture has only ever been referenced in the AutoDeleteCache.
                                    // Keep this texture alive with the short duration cache, as it may be used often but not sampled.

                                    _cache.AddShortCache(overlap);
                                }
                            }

                            removeOverlap = modified;
                        }
                        else
                        {
                            // If an incompatible overlapping texture has been modified, then it's data is likely destined for this texture,
                            // and the overlapped texture will contain garbage. In this case, it should be removed to save memory.
                            removeOverlap = modified;
                        }

                        if (removeOverlap && overlap.Info.Target != Target.TextureBuffer)
                        {
                            overlap.RemoveFromPools(false);
                        }
                    }
                }

                texture.InitializeGroup(hasLayerViews, hasMipViews, incompatibleOverlaps);

                // We need to synchronize before copying the old view data to the texture,
                // otherwise the copied data would be overwritten by a future synchronization.
                texture.InitializeData(false, setData && !ShouldDiscard(discard, texture, sizeHint));

                texture.Group.InitializeOverlaps();

                for (int index = 0; index < viewCompatible; index++)
                {
                    Texture overlap = _textureOverlaps[index];

                    OverlapInfo oInfo = _overlapInfo[index];

                    if (overlap.Group == texture.Group)
                    {
                        // If the texture group is equal, then this texture (or its parent) is already a view.
                        continue;
                    }

                    // Note: If we allow different sizes for those overlaps,
                    // we need to make sure that the "info" has the correct size for the parent texture here.
                    // Since this is not allowed right now, we don't need to do it.

                    TextureInfo overlapInfo = overlap.Info;

                    if (texture.ScaleFactor != overlap.ScaleFactor)
                    {
                        // A bit tricky, our new texture may need to contain an existing texture that is upscaled, but isn't itself.
                        // In that case, we prefer the higher scale only if our format is render-target-like, otherwise we scale the view down before copy.

                        texture.PropagateScale(overlap);
                    }

                    if (oInfo.Compatibility != TextureViewCompatibility.Full)
                    {
                        // Copy only compatibility, or target texture is already a view.

                        overlap.SynchronizeMemory();
                        texture.CreateCopyDependency(overlap, oInfo.FirstLayer, oInfo.FirstLevel, false);
                    }
                    else
                    {
                        TextureCreateInfo createInfo = GetCreateInfo(overlapInfo, _context.Capabilities, overlap.ScaleFactor);

                        ITexture newView = texture.HostTexture.CreateView(createInfo, oInfo.FirstLayer, oInfo.FirstLevel);

                        overlap.SynchronizeMemory();

                        overlap.HostTexture.CopyTo(newView, 0, 0);

                        overlap.ReplaceView(texture, overlapInfo, newView, oInfo.FirstLayer, oInfo.FirstLevel);
                    }
                }

                texture.SynchronizeMemory();
            }

            // Sampler textures are managed by the texture pool, all other textures
            // are managed by the auto delete cache.
            if (!isSamplerTexture)
            {
                _cache.Add(texture);
            }

            _texturesLock.EnterWriteLock();

            try
            {
                _textures.Add(texture);
            }
            finally
            {
                _texturesLock.ExitWriteLock();
            }

            if (partiallyMapped)
            {
                lock (_partiallyMappedTextures)
                {
                    _partiallyMappedTextures.Add(texture);
                }
            }

            ShrinkOverlapsBufferIfNeeded();

            for (int i = 0; i < overlapsCount; i++)
            {
                _textureOverlaps[i].DecrementReferenceCount();
            }

            return texture;
        }

        /// <summary>
        /// Attempt to find a texture on the short duration cache.
        /// </summary>
        /// <param name="descriptor">The texture descriptor</param>
        /// <returns>The texture if found, null otherwise</returns>
        public Texture FindShortCache(in TextureDescriptor descriptor)
        {
            return _cache.FindShortCache(descriptor);
        }

        /// <summary>
        /// Tries to find an existing texture matching the given buffer copy destination. If none is found, returns null.
        /// </summary>
        /// <param name="memoryManager">GPU memory manager where the texture is mapped</param>
        /// <param name="gpuVa">GPU virtual address of the texture</param>
        /// <param name="bpp">Bytes per pixel</param>
        /// <param name="stride">If <paramref name="linear"/> is true, should have the texture stride, otherwise ignored</param>
        /// <param name="height">If <paramref name="linear"/> is false, should have the texture height, otherwise ignored</param>
        /// <param name="xCount">Number of pixels to be copied per line</param>
        /// <param name="yCount">Number of lines to be copied</param>
        /// <param name="linear">True if the texture has a linear layout, false otherwise</param>
        /// <param name="gobBlocksInY">If <paramref name="linear"/> is false, the amount of GOB blocks in the Y axis</param>
        /// <param name="gobBlocksInZ">If <paramref name="linear"/> is false, the amount of GOB blocks in the Z axis</param>
        /// <returns>A matching texture, or null if there is no match</returns>
        public Texture FindTexture(
            MemoryManager memoryManager,
            ulong gpuVa,
            int bpp,
            int stride,
            int height,
            int xCount,
            int yCount,
            bool linear,
            int gobBlocksInY,
            int gobBlocksInZ)
        {
            ulong address = memoryManager.Translate(gpuVa);

            if (address == MemoryManager.PteUnmapped)
            {
                return null;
            }

            int addressMatches;

            _texturesLock.EnterReadLock();

            try
            {
                addressMatches = _textures.FindOverlaps(address, ref _textureOverlaps);
            }
            finally
            {
                _texturesLock.ExitReadLock();
            }

            Texture textureMatch = null;

            for (int i = 0; i < addressMatches; i++)
            {
                Texture texture = _textureOverlaps[i];
                FormatInfo format = texture.Info.FormatInfo;

                if (texture.Info.DepthOrLayers > 1 || texture.Info.Levels > 1 || texture.Info.FormatInfo.IsCompressed)
                {
                    // Don't support direct buffer copies to anything that isn't a single 2D image, uncompressed.
                    continue;
                }

                bool match;

                if (linear)
                {
                    // Size is not available for linear textures. Use the stride and end of the copy region instead.

                    match = texture.Info.IsLinear && texture.Info.Stride == stride && yCount == texture.Info.Height;
                }
                else
                {
                    // Bpp may be a mismatch between the target texture and the param.
                    // Due to the way linear strided and block layouts work, widths can be multiplied by Bpp for comparison.
                    // Note: tex.Width is the aligned texture size. Prefer param.XCount, as the destination should be a texture with that exact size.

                    bool sizeMatch = xCount * bpp == texture.Info.Width * format.BytesPerPixel && height == texture.Info.Height;
                    bool formatMatch = !texture.Info.IsLinear &&
                                        texture.Info.GobBlocksInY == gobBlocksInY &&
                                        texture.Info.GobBlocksInZ == gobBlocksInZ;

                    match = sizeMatch && formatMatch;
                }

                if (match)
                {
                    if (textureMatch == null)
                    {
                        textureMatch = texture;
                    }
                    else if (texture.Group != textureMatch.Group)
                    {
                        return null; // It's ambiguous which texture should match between multiple choices, so leave it up to the slow path.
                    }
                }
            }

            return textureMatch;
        }

        /// <summary>
        /// Resizes the temporary buffer used for range list intersection results, if it has grown too much.
        /// </summary>
        private void ShrinkOverlapsBufferIfNeeded()
        {
            if (_textureOverlaps.Length > OverlapsBufferMaxCapacity)
            {
                Array.Resize(ref _textureOverlaps, OverlapsBufferMaxCapacity);
            }
        }

        /// <summary>
        /// Gets a texture creation information from texture information.
        /// This can be used to create new host textures.
        /// </summary>
        /// <param name="info">Texture information</param>
        /// <param name="caps">GPU capabilities</param>
        /// <param name="scale">Texture scale factor, to be applied to the texture size</param>
        /// <returns>The texture creation information</returns>
        public static TextureCreateInfo GetCreateInfo(TextureInfo info, Capabilities caps, float scale)
        {
            FormatInfo formatInfo = TextureCompatibility.ToHostCompatibleFormat(info, caps);

            if (info.Target == Target.TextureBuffer && !caps.SupportsSnormBufferTextureFormat)
            {
                // If the host does not support signed normalized formats, we use a signed integer format instead.
                // The shader will need the appropriate conversion code to compensate.
                switch (formatInfo.Format)
                {
                    case Format.R8Snorm:
                        formatInfo = new FormatInfo(Format.R8Sint, 1, 1, 1, 1);
                        break;
                    case Format.R16Snorm:
                        formatInfo = new FormatInfo(Format.R16Sint, 1, 1, 2, 1);
                        break;
                    case Format.R8G8Snorm:
                        formatInfo = new FormatInfo(Format.R8G8Sint, 1, 1, 2, 2);
                        break;
                    case Format.R16G16Snorm:
                        formatInfo = new FormatInfo(Format.R16G16Sint, 1, 1, 4, 2);
                        break;
                    case Format.R8G8B8A8Snorm:
                        formatInfo = new FormatInfo(Format.R8G8B8A8Sint, 1, 1, 4, 4);
                        break;
                    case Format.R16G16B16A16Snorm:
                        formatInfo = new FormatInfo(Format.R16G16B16A16Sint, 1, 1, 8, 4);
                        break;
                }
            }

            int width = info.Width / info.SamplesInX;
            int height = info.Height / info.SamplesInY;

            int depth = info.GetDepth() * info.GetLayers();

            if (scale != 1f)
            {
                width = (int)MathF.Ceiling(width * scale);
                height = (int)MathF.Ceiling(height * scale);
            }

            return new TextureCreateInfo(
                width,
                height,
                depth,
                info.Levels,
                info.Samples,
                formatInfo.BlockWidth,
                formatInfo.BlockHeight,
                formatInfo.BytesPerPixel,
                formatInfo.Format,
                info.DepthStencilMode,
                info.Target,
                info.SwizzleR,
                info.SwizzleG,
                info.SwizzleB,
                info.SwizzleA);
        }

        /// <summary>
        /// Removes a texture from the cache.
        /// </summary>
        /// <remarks>
        /// This only removes the texture from the internal list, not from the auto-deletion cache.
        /// It may still have live references after the removal.
        /// </remarks>
        /// <param name="texture">The texture to be removed</param>
        public void RemoveTextureFromCache(Texture texture)
        {
            _texturesLock.EnterWriteLock();

            try
            {
                _textures.Remove(texture);
            }
            finally
            {
                _texturesLock.ExitWriteLock();
            }

            lock (_partiallyMappedTextures)
            {
                _partiallyMappedTextures.Remove(texture);
            }
        }

        /// <summary>
        /// Queries a texture's memory range and marks it as partially mapped or not.
        /// Partially mapped textures re-evaluate their memory range after each time GPU memory is mapped.
        /// </summary>
        /// <param name="memoryManager">GPU memory manager where the texture is mapped</param>
        /// <param name="address">The virtual address of the texture</param>
        /// <param name="texture">The texture to be marked</param>
        /// <returns>The physical regions for the texture, found when evaluating whether the texture was partially mapped</returns>
        public MultiRange UpdatePartiallyMapped(MemoryManager memoryManager, ulong address, Texture texture)
        {
            MultiRange range;
            lock (_partiallyMappedTextures)
            {
                range = memoryManager.GetPhysicalRegions(address, texture.Size);
                bool partiallyMapped = false;

                for (int i = 0; i < range.Count; i++)
                {
                    if (range.GetSubRange(i).Address == MemoryManager.PteUnmapped)
                    {
                        partiallyMapped = true;
                        break;
                    }
                }

                if (partiallyMapped)
                {
                    _partiallyMappedTextures.Add(texture);
                }
                else
                {
                    _partiallyMappedTextures.Remove(texture);
                }
            }

            return range;
        }

        /// <summary>
        /// Adds a texture to the short duration cache. This typically keeps it alive for two ticks.
        /// </summary>
        /// <param name="texture">Texture to add to the short cache</param>
        /// <param name="descriptor">Last used texture descriptor</param>
        public void AddShortCache(Texture texture, ref TextureDescriptor descriptor)
        {
            _cache.AddShortCache(texture, ref descriptor);
        }

        /// <summary>
        /// Adds a texture to the short duration cache without a descriptor. This typically keeps it alive for two ticks.
        /// On expiry, it will be removed from the AutoDeleteCache.
        /// </summary>
        /// <param name="texture">Texture to add to the short cache</param>
        public void AddShortCache(Texture texture)
        {
            _cache.AddShortCache(texture);
        }

        /// <summary>
        /// Removes a texture from the short duration cache.
        /// </summary>
        /// <param name="texture">Texture to remove from the short cache</param>
        public void RemoveShortCache(Texture texture)
        {
            _cache.RemoveShortCache(texture);
        }

        /// <summary>
        /// Ticks periodic elements of the texture cache.
        /// </summary>
        public void Tick()
        {
            _cache.ProcessShortCache();
        }

        /// <summary>
        /// Disposes all textures and samplers in the cache.
        /// It's an error to use the texture cache after disposal.
        /// </summary>
        public void Dispose()
        {
            _texturesLock.EnterReadLock();

            try
            {
                foreach (Texture texture in _textures)
                {
                    texture.Dispose();
                }
            }
            finally
            {
                _texturesLock.ExitReadLock();
            }
        }
    }
}

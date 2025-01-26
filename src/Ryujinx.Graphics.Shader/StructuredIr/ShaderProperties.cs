using System.Collections.Generic;

namespace Ryujinx.Graphics.Shader.StructuredIr
{
    class ShaderProperties
    {
        private readonly Dictionary<int, BufferDefinition> _constantBuffers;
        private readonly Dictionary<int, BufferDefinition> _storageBuffers;
        private readonly Dictionary<SetBindingPair, TextureDefinition> _textures;
        private readonly Dictionary<SetBindingPair, TextureDefinition> _images;
        private readonly Dictionary<int, MemoryDefinition> _localMemories;
        private readonly Dictionary<int, MemoryDefinition> _sharedMemories;

        public IReadOnlyDictionary<int, BufferDefinition> ConstantBuffers => _constantBuffers;
        public IReadOnlyDictionary<int, BufferDefinition> StorageBuffers => _storageBuffers;
        public IReadOnlyDictionary<SetBindingPair, TextureDefinition> Textures => _textures;
        public IReadOnlyDictionary<SetBindingPair, TextureDefinition> Images => _images;
        public IReadOnlyDictionary<int, MemoryDefinition> LocalMemories => _localMemories;
        public IReadOnlyDictionary<int, MemoryDefinition> SharedMemories => _sharedMemories;

        public ShaderProperties()
        {
            _constantBuffers = new Dictionary<int, BufferDefinition>();
            _storageBuffers = new Dictionary<int, BufferDefinition>();
            _textures = new Dictionary<SetBindingPair, TextureDefinition>();
            _images = new Dictionary<SetBindingPair, TextureDefinition>();
            _localMemories = new Dictionary<int, MemoryDefinition>();
            _sharedMemories = new Dictionary<int, MemoryDefinition>();
        }

        public void AddOrUpdateConstantBuffer(BufferDefinition definition)
        {
            _constantBuffers[definition.Binding] = definition;
        }

        public void AddOrUpdateStorageBuffer(BufferDefinition definition)
        {
            _storageBuffers[definition.Binding] = definition;
        }

        public void AddOrUpdateTexture(TextureDefinition definition)
        {
            _textures[new(definition.Set, definition.Binding)] = definition;
        }

        public void AddOrUpdateImage(TextureDefinition definition)
        {
            _images[new(definition.Set, definition.Binding)] = definition;
        }

        public int AddLocalMemory(MemoryDefinition definition)
        {
            int id = _localMemories.Count;
            _localMemories.Add(id, definition);

            return id;
        }

        public int AddSharedMemory(MemoryDefinition definition)
        {
            int id = _sharedMemories.Count;
            _sharedMemories.Add(id, definition);

            return id;
        }

        public static TextureFormat GetTextureFormat(IGpuAccessor gpuAccessor, int handle, int cbufSlot = -1)
        {
            // When the formatted load extension is supported, we don't need to
            // specify a format, we can just declare it without a format and the GPU will handle it.
            if (gpuAccessor.QueryHostSupportsImageLoadFormatted())
            {
                return TextureFormat.Unknown;
            }

            TextureFormat format = gpuAccessor.QueryTextureFormat(handle, cbufSlot);

            if (format == TextureFormat.Unknown)
            {
                gpuAccessor.Log($"Unknown format for texture {handle}.");

                format = TextureFormat.R8G8B8A8Unorm;
            }

            return format;
        }

        private static bool FormatSupportsAtomic(TextureFormat format)
        {
            return format == TextureFormat.R32Sint || format == TextureFormat.R32Uint;
        }

        public static TextureFormat GetTextureFormatAtomic(IGpuAccessor gpuAccessor, int handle, int cbufSlot = -1)
        {
            // Atomic image instructions do not support GL_EXT_shader_image_load_formatted,
            // and must have a type specified. Default to R32Sint if not available.

            TextureFormat format = gpuAccessor.QueryTextureFormat(handle, cbufSlot);

            if (!FormatSupportsAtomic(format))
            {
                gpuAccessor.Log($"Unsupported format for texture {handle}: {format}.");

                format = TextureFormat.R32Sint;
            }

            return format;
        }
    }
}

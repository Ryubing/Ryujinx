using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Gpu.Image;
using Ryujinx.Memory.Range;

namespace Ryujinx.Graphics.Gpu.Memory
{
    /// <summary>
    /// A buffer binding to apply to a buffer texture array element.
    /// </summary>
    readonly struct BufferTextureArrayBinding<T>
    {
        /// <summary>
        /// Backend texture or image array.
        /// </summary>
        public T Array { get; }

        /// <summary>
        /// The buffer texture.
        /// </summary>
        public ITexture Texture { get; }

        /// <summary>
        /// Buffer cache that owns the buffer.
        /// </summary>
        public BufferCache BufferCache { get; }

        /// <summary>
        /// Physical ranges of memory where the buffer texture data is located.
        /// </summary>
        public MultiRange Range { get; }

        /// <summary>
        /// The image or sampler binding info for the buffer texture.
        /// </summary>
        public TextureBindingInfo BindingInfo { get; }

        /// <summary>
        /// Index of the binding on the array.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Create a new buffer texture binding.
        /// </summary>
        /// <param name="array">Array</param>
        /// <param name="texture">Buffer texture</param>
        /// <param name="bufferCache">Buffer cache that owns the buffer</param>
        /// <param name="range">Physical ranges of memory where the buffer texture data is located</param>
        /// <param name="bindingInfo">Binding info</param>
        /// <param name="index">Index of the binding on the array</param>
        public BufferTextureArrayBinding(
            T array,
            ITexture texture,
            BufferCache bufferCache,
            MultiRange range,
            TextureBindingInfo bindingInfo,
            int index)
        {
            Array = array;
            Texture = texture;
            BufferCache = bufferCache;
            Range = range;
            BindingInfo = bindingInfo;
            Index = index;
        }
    }
}

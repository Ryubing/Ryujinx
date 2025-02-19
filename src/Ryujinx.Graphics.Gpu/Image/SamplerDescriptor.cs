using Ryujinx.Graphics.GAL;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Ryujinx.Graphics.Gpu.Image
{
    /// <summary>
    /// Maxwell sampler descriptor structure.
    /// This structure defines the sampler descriptor as it is packed on the GPU sampler pool region.
    /// </summary>
    struct SamplerDescriptor
    {
        private static readonly float[] _f5ToF32ConversionLut =
        [
            0.0f,
            0.055555556f,
            0.1f,
            0.13636364f,
            0.16666667f,
            0.1923077f,
            0.21428572f,
            0.23333333f,
            0.25f,
            0.2777778f,
            0.3f,
            0.3181818f,
            0.33333334f,
            0.34615386f,
            0.35714287f,
            0.36666667f,
            0.375f,
            0.3888889f,
            0.4f,
            0.4090909f,
            0.41666666f,
            0.42307693f,
            0.42857143f,
            0.43333334f,
            0.4375f,
            0.44444445f,
            0.45f,
            0.45454547f,
            0.45833334f,
            0.46153846f,
            0.4642857f,
            0.46666667f
        ];

        private static readonly float[] _maxAnisotropyLut =
        [
            1, 2, 4, 6, 8, 10, 12, 16
        ];

        private const float Frac8ToF32 = 1.0f / 256.0f;

#pragma warning disable CS0649 // Field is never assigned to
        public uint Word0;
        public uint Word1;
        public uint Word2;
        public uint Word3;
        public float BorderColorR;
        public float BorderColorG;
        public float BorderColorB;
        public float BorderColorA;
#pragma warning restore CS0649

        /// <summary>
        /// Unpacks the texture wrap mode along the X axis.
        /// </summary>
        /// <returns>The texture wrap mode enum</returns>
        public readonly AddressMode UnpackAddressU()
        {
            return (AddressMode)(Word0 & 7);
        }

        // <summary>
        /// Unpacks the texture wrap mode along the Y axis.
        /// </summary>
        /// <returns>The texture wrap mode enum</returns>
        public readonly AddressMode UnpackAddressV()
        {
            return (AddressMode)((Word0 >> 3) & 7);
        }

        // <summary>
        /// Unpacks the texture wrap mode along the Z axis.
        /// </summary>
        /// <returns>The texture wrap mode enum</returns>
        public readonly AddressMode UnpackAddressP()
        {
            return (AddressMode)((Word0 >> 6) & 7);
        }

        /// <summary>
        /// Unpacks the compare mode used for depth comparison on the shader, for
        /// depth buffer texture.
        /// This is only relevant for shaders with shadow samplers.
        /// </summary>
        /// <returns>The depth comparison mode enum</returns>
        public readonly CompareMode UnpackCompareMode()
        {
            return (CompareMode)((Word0 >> 9) & 1);
        }

        /// <summary>
        /// Unpacks the compare operation used for depth comparison on the shader, for
        /// depth buffer texture.
        /// This is only relevant for shaders with shadow samplers.
        /// </summary>
        /// <returns>The depth comparison operation enum</returns>
        public readonly CompareOp UnpackCompareOp()
        {
            return (CompareOp)(((Word0 >> 10) & 7) + 1);
        }

        /// <summary>
        /// Unpacks the sampler sRGB format flag.
        /// </summary>
        /// <returns>True if the has sampler is sRGB conversion enabled, false otherwise</returns>
        public readonly bool UnpackSrgb()
        {
            return (Word0 & (1 << 13)) != 0;
        }

        /// <summary>
        /// Unpacks and converts the maximum anisotropy value used for texture anisotropic filtering.
        /// </summary>
        /// <returns>The maximum anisotropy</returns>
        public readonly float UnpackMaxAnisotropy()
        {
            return _maxAnisotropyLut[(Word0 >> 20) & 7];
        }

        /// <summary>
        /// Unpacks the texture magnification filter.
        /// This defines the filtering used when the texture covers an area on the screen
        /// that is larger than the texture size.
        /// </summary>
        /// <returns>The magnification filter</returns>
        public readonly MagFilter UnpackMagFilter()
        {
            return (MagFilter)(Word1 & 3);
        }

        /// <summary>
        /// Unpacks the texture minification filter.
        /// This defines the filtering used when the texture covers an area on the screen
        /// that is smaller than the texture size.
        /// </summary>
        /// <returns>The minification filter</returns>
        public readonly MinFilter UnpackMinFilter()
        {
            SamplerMinFilter minFilter = (SamplerMinFilter)((Word1 >> 4) & 3);
            SamplerMipFilter mipFilter = (SamplerMipFilter)((Word1 >> 6) & 3);

            return ConvertFilter(minFilter, mipFilter);
        }

        /// <summary>
        /// Converts two minification and filter enum, to a single minification enum,
        /// including mipmap filtering information, as expected from the host API.
        /// </summary>
        /// <param name="minFilter">The minification filter</param>
        /// <param name="mipFilter">The mipmap level filter</param>
        /// <returns>The combined, host API compatible filter enum</returns>
        private static MinFilter ConvertFilter(SamplerMinFilter minFilter, SamplerMipFilter mipFilter)
        {
            switch (mipFilter)
            {
                case SamplerMipFilter.None:
                    switch (minFilter)
                    {
                        case SamplerMinFilter.Nearest:
                            return MinFilter.Nearest;
                        case SamplerMinFilter.Linear:
                            return MinFilter.Linear;
                    }
                    break;

                case SamplerMipFilter.Nearest:
                    switch (minFilter)
                    {
                        case SamplerMinFilter.Nearest:
                            return MinFilter.NearestMipmapNearest;
                        case SamplerMinFilter.Linear:
                            return MinFilter.LinearMipmapNearest;
                    }
                    break;

                case SamplerMipFilter.Linear:
                    switch (minFilter)
                    {
                        case SamplerMinFilter.Nearest:
                            return MinFilter.NearestMipmapLinear;
                        case SamplerMinFilter.Linear:
                            return MinFilter.LinearMipmapLinear;
                    }
                    break;
            }

            return MinFilter.Nearest;
        }

        /// <summary>
        /// Unpacks the seamless cubemap flag.
        /// </summary>
        /// <returns>The seamless cubemap flag</returns>
        public readonly bool UnpackSeamlessCubemap()
        {
            return (Word1 & (1 << 9)) != 0;
        }

        /// <summary>
        /// Unpacks the reduction filter, used with texture minification linear filtering.
        /// This describes how the final value will be computed from neighbouring pixels.
        /// </summary>
        /// <returns>The reduction filter</returns>
        public readonly ReductionFilter UnpackReductionFilter()
        {
            return (ReductionFilter)((Word1 >> 10) & 3);
        }

        /// <summary>
        /// Unpacks the level-of-detail bias value.
        /// This is a bias added to the level-of-detail value as computed by the GPU, used to select
        /// which mipmap level to use from a given texture.
        /// </summary>
        /// <returns>The level-of-detail bias value</returns>
        public readonly float UnpackMipLodBias()
        {
            int fixedValue = (int)(Word1 >> 12) & 0x1fff;

            fixedValue = (fixedValue << 19) >> 19;

            return fixedValue * Frac8ToF32;
        }

        /// <summary>
        /// Unpacks the level-of-detail snap value.
        /// </summary>
        /// <returns>The level-of-detail snap value</returns>
        public readonly float UnpackLodSnap()
        {
            return _f5ToF32ConversionLut[(Word1 >> 26) & 0x1f];
        }

        /// <summary>
        /// Unpacks the minimum level-of-detail value.
        /// </summary>
        /// <returns>The minimum level-of-detail value</returns>
        public readonly float UnpackMinLod()
        {
            return (Word2 & 0xfff) * Frac8ToF32;
        }

        /// <summary>
        /// Unpacks the maximum level-of-detail value.
        /// </summary>
        /// <returns>The maximum level-of-detail value</returns>
        public readonly float UnpackMaxLod()
        {
            return ((Word2 >> 12) & 0xfff) * Frac8ToF32;
        }

        /// <summary>
        /// Check if two descriptors are equal.
        /// </summary>
        /// <param name="other">The descriptor to compare against</param>
        /// <returns>True if they are equal, false otherwise</returns>
        public bool Equals(ref SamplerDescriptor other)
        {
            return Unsafe.As<SamplerDescriptor, Vector256<byte>>(ref this).Equals(Unsafe.As<SamplerDescriptor, Vector256<byte>>(ref other));
        }
    }
}

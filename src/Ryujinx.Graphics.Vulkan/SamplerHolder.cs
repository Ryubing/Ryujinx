using Ryujinx.Graphics.GAL;
using Silk.NET.Vulkan;
using SamplerCreateInfo = Ryujinx.Graphics.GAL.SamplerCreateInfo;

namespace Ryujinx.Graphics.Vulkan
{
    class SamplerHolder : ISampler
    {
        private readonly VulkanRenderer _gd;
        private readonly Auto<DisposableSampler> _sampler;

        public unsafe SamplerHolder(VulkanRenderer gd, Device device, SamplerCreateInfo info)
        {
            _gd = gd;

            gd.Samplers.Add(this);

            (Filter minFilter, SamplerMipmapMode mipFilter) = info.MinFilter.Convert();

            float minLod = info.MinLod;
            float maxLod = info.MaxLod;

            if (info.MinFilter == MinFilter.Nearest || info.MinFilter == MinFilter.Linear)
            {
                minLod = 0;
                maxLod = 0.25f;
            }

            BorderColor borderColor = GetConstrainedBorderColor(info.BorderColor, out bool cantConstrain);

            Silk.NET.Vulkan.SamplerCreateInfo samplerCreateInfo = new()
            {
                SType = StructureType.SamplerCreateInfo,
                MagFilter = info.MagFilter.Convert(),
                MinFilter = minFilter,
                MipmapMode = mipFilter,
                AddressModeU = info.AddressU.Convert(),
                AddressModeV = info.AddressV.Convert(),
                AddressModeW = info.AddressP.Convert(),
                MipLodBias = info.MipLodBias,
                AnisotropyEnable = info.MaxAnisotropy != 1f,
                MaxAnisotropy = info.MaxAnisotropy,
                CompareEnable = info.CompareMode == CompareMode.CompareRToTexture,
                CompareOp = info.CompareOp.Convert(),
                MinLod = minLod,
                MaxLod = maxLod,
                BorderColor = borderColor,
                UnnormalizedCoordinates = false, // TODO: Use unnormalized coordinates.
            };

            SamplerCustomBorderColorCreateInfoEXT customBorderColor;

            if (cantConstrain && gd.Capabilities.SupportsCustomBorderColor)
            {
                ClearColorValue color = new(
                    info.BorderColor.Red,
                    info.BorderColor.Green,
                    info.BorderColor.Blue,
                    info.BorderColor.Alpha);

                customBorderColor = new SamplerCustomBorderColorCreateInfoEXT
                {
                    SType = StructureType.SamplerCustomBorderColorCreateInfoExt,
                    CustomBorderColor = color,
                };

                samplerCreateInfo.PNext = &customBorderColor;
                samplerCreateInfo.BorderColor = BorderColor.FloatCustomExt;
            }

            gd.Api.CreateSampler(device, in samplerCreateInfo, null, out Sampler sampler).ThrowOnError();

            _sampler = new Auto<DisposableSampler>(new DisposableSampler(gd.Api, device, sampler));
        }

        private static BorderColor GetConstrainedBorderColor(ColorF arbitraryBorderColor, out bool cantConstrain)
        {
            float r = arbitraryBorderColor.Red;
            float g = arbitraryBorderColor.Green;
            float b = arbitraryBorderColor.Blue;
            float a = arbitraryBorderColor.Alpha;

            if (r == 0f && g == 0f && b == 0f)
            {
                if (a == 1f)
                {
                    cantConstrain = false;
                    return BorderColor.FloatOpaqueBlack;
                }

                if (a == 0f)
                {
                    cantConstrain = false;
                    return BorderColor.FloatTransparentBlack;
                }
            }
            else if (r == 1f && g == 1f && b == 1f && a == 1f)
            {
                cantConstrain = false;
                return BorderColor.FloatOpaqueWhite;
            }

            cantConstrain = true;
            return BorderColor.FloatOpaqueBlack;
        }

        public Auto<DisposableSampler> GetSampler()
        {
            return _sampler;
        }

        public void Dispose()
        {
            if (_gd.Samplers.Remove(this))
            {
                _sampler.Dispose();
            }
        }
    }
}

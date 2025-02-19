using Ryujinx.Graphics.Metal.State;
using SharpMetal.Metal;
using System.Runtime.Versioning;

namespace Ryujinx.Graphics.Metal
{
    [SupportedOSPlatform("macos")]
    class DepthStencilCache : StateCache<MTLDepthStencilState, DepthStencilUid, DepthStencilUid>
    {
        private readonly MTLDevice _device;

        public DepthStencilCache(MTLDevice device)
        {
            _device = device;
        }

        protected override DepthStencilUid GetHash(DepthStencilUid descriptor)
        {
            return descriptor;
        }

        protected override MTLDepthStencilState CreateValue(DepthStencilUid descriptor)
        {
            // Create descriptors

            ref StencilUid frontUid = ref descriptor.FrontFace;

            using MTLStencilDescriptor frontFaceStencil = new()
            {
                StencilFailureOperation = frontUid.StencilFailureOperation,
                DepthFailureOperation = frontUid.DepthFailureOperation,
                DepthStencilPassOperation = frontUid.DepthStencilPassOperation,
                StencilCompareFunction = frontUid.StencilCompareFunction,
                ReadMask = frontUid.ReadMask,
                WriteMask = frontUid.WriteMask
            };

            ref StencilUid backUid = ref descriptor.BackFace;

            using MTLStencilDescriptor backFaceStencil = new()
            {
                StencilFailureOperation = backUid.StencilFailureOperation,
                DepthFailureOperation = backUid.DepthFailureOperation,
                DepthStencilPassOperation = backUid.DepthStencilPassOperation,
                StencilCompareFunction = backUid.StencilCompareFunction,
                ReadMask = backUid.ReadMask,
                WriteMask = backUid.WriteMask
            };

            MTLDepthStencilDescriptor mtlDescriptor = new()
            {
                DepthCompareFunction = descriptor.DepthCompareFunction,
                DepthWriteEnabled = descriptor.DepthWriteEnabled
            };

            if (descriptor.StencilTestEnabled)
            {
                mtlDescriptor.BackFaceStencil = backFaceStencil;
                mtlDescriptor.FrontFaceStencil = frontFaceStencil;
            }

            using (mtlDescriptor)
            {
                return _device.NewDepthStencilState(mtlDescriptor);
            }
        }
    }
}

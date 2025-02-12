using Ryujinx.Common.Memory;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Vi.RootService.ApplicationDisplayService.Types.Fbshare
{
    [StructLayout(LayoutKind.Sequential, Size = 0x10, Pack = 0x4)]
    struct SharedLayerTextureIndexList
    {
        public Array4<int> Indices;
    }
}

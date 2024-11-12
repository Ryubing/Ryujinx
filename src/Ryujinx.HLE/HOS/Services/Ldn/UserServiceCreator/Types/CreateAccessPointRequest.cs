using Ryujinx.HLE.HOS.Services.Ldn.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnRyu.Types;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Types
{
    /// <remarks>
    /// Advertise data is appended separately (remaining data in the buffer).
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Size = 0xBC, Pack = 1)]
    struct CreateAccessPointRequest
    {
        public SecurityConfig SecurityConfig;
        public UserConfig UserConfig;
        public NetworkConfig NetworkConfig;

        public RyuNetworkConfig RyuNetworkConfig;
    }
}

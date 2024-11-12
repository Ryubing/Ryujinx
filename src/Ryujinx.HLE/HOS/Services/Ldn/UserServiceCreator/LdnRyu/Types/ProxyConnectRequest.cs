using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnRyu.Types
{
    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    struct ProxyConnectRequest
    {
        public ProxyInfo Info;
    }
}

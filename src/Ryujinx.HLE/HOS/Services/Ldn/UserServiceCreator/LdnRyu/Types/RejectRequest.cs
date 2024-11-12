using Ryujinx.HLE.HOS.Services.Ldn.Types;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnRyu.Types
{
    [StructLayout(LayoutKind.Sequential, Size = 0x8)]
    struct RejectRequest
    {
        public uint NodeId;
        public DisconnectReason DisconnectReason;

        public RejectRequest(DisconnectReason disconnectReason, uint nodeId)
        {
            DisconnectReason = disconnectReason;
            NodeId = nodeId;
        }
    }
}

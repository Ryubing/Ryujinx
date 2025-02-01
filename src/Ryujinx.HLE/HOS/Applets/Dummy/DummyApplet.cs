using Ryujinx.Common.Memory;
using Ryujinx.HLE.HOS.Services.Am.AppletAE;
using System;
using System.IO;

namespace Ryujinx.HLE.HOS.Applets.Dummy
{
    internal class DummyApplet : IApplet
    {
        private readonly Horizon _system;
        private AppletSession _normalSession;
        
        public event EventHandler AppletStateChanged;
        
        public DummyApplet(Horizon system)
        {
            _system = system;
        }
        
        public ResultCode Start(AppletSession normalSession, AppletSession interactiveSession)
        {
            _normalSession = normalSession;
            _normalSession.Push(BuildResponse());
            AppletStateChanged?.Invoke(this, null);
            _system.ReturnFocus();
            return ResultCode.Success;
        }
        
        private static byte[] BuildResponse()
        {
            using MemoryStream stream = MemoryStreamManager.Shared.GetStream();
            using BinaryWriter writer = new(stream);
            writer.Write((ulong)ResultCode.Success);
            return stream.ToArray();
        }
    }
}

using Ryujinx.Common;
using Ryujinx.HLE.HOS;
using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.HOS.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Ryujinx.HLE.Exceptions
{
    [Serializable]
    internal class ServiceNotImplementedException : Exception
    {
        public IpcService Service { get; }
        public ServiceCtx Context { get; }
        public IpcMessage Request { get; }

        public ServiceNotImplementedException(IpcService service, ServiceCtx context)
            : this(service, context, "The service call is not implemented.") { }

        public ServiceNotImplementedException(IpcService service, ServiceCtx context, string message) : base(message)
        {
            Service = service;
            Context = context;
            Request = context.Request;
        }

        public ServiceNotImplementedException(IpcService service, ServiceCtx context, string message, Exception inner) : base(message, inner)
        {
            Service = service;
            Context = context;
            Request = context.Request;
        }

        public override string Message
        {
            get
            {
                return base.Message + Environment.NewLine + Environment.NewLine + BuildMessage();
            }
        }

        private string BuildMessage()
        {
            StringBuilder sb = new();

            // Print the IPC command details (service name, command ID, and handler)
            (Type callingType, MethodBase callingMethod) = WalkStackTrace(new StackTrace(this));

            if (callingType != null && callingMethod != null)
            {
                // If the type is past 0xF, we are using TIPC
                IReadOnlyDictionary<int, MethodInfo> ipcCommands = Request.Type > IpcMessageType.TipcCloseSession ? Service.TipcCommands : Service.CmifCommands;

                // Find the handler for the method called
                KeyValuePair<int, MethodInfo> ipcHandler = ipcCommands.FirstOrDefault(x => x.Value == callingMethod);
                int ipcCommandId = ipcHandler.Key;
                MethodInfo ipcMethod = ipcHandler.Value;

                if (ipcMethod != null)
                {
                    sb.AppendLine($"Service Command: {Service.GetType().FullName}: {ipcCommandId} ({ipcMethod.Name})");
                    sb.AppendLine();
                }
            }

            sb.AppendLine("Guest Stack Trace:");
            sb.AppendLine(Context.Thread.GetGuestStackTrace());

            // Print buffer information
            if (Request.PtrBuff.Count > 0 ||
                Request.SendBuff.Count > 0 ||
                Request.ReceiveBuff.Count > 0 ||
                Request.ExchangeBuff.Count > 0 ||
                Request.RecvListBuff.Count > 0)
            {
                sb.AppendLine("Buffer Information:");

                if (Request.PtrBuff.Count > 0)
                {
                    sb.AppendLine("\tPtrBuff:");

                    foreach (IpcPtrBuffDesc buff in Request.PtrBuff)
                    {
                        sb.AppendLine($"\t[{buff.Index}] Position: 0x{buff.Position:x16} Size: 0x{buff.Size:x16}");
                    }
                }

                if (Request.SendBuff.Count > 0)
                {
                    sb.AppendLine("\tSendBuff:");

                    foreach (IpcBuffDesc buff in Request.SendBuff)
                    {
                        sb.AppendLine($"\tPosition: 0x{buff.Position:x16} Size: 0x{buff.Size:x16} Flags: {buff.Flags}");
                    }
                }

                if (Request.ReceiveBuff.Count > 0)
                {
                    sb.AppendLine("\tReceiveBuff:");

                    foreach (IpcBuffDesc buff in Request.ReceiveBuff)
                    {
                        sb.AppendLine($"\tPosition: 0x{buff.Position:x16} Size: 0x{buff.Size:x16} Flags: {buff.Flags}");
                    }
                }

                if (Request.ExchangeBuff.Count > 0)
                {
                    sb.AppendLine("\tExchangeBuff:");

                    foreach (IpcBuffDesc buff in Request.ExchangeBuff)
                    {
                        sb.AppendLine($"\tPosition: 0x{buff.Position:x16} Size: 0x{buff.Size:x16} Flags: {buff.Flags}");
                    }
                }

                if (Request.RecvListBuff.Count > 0)
                {
                    sb.AppendLine("\tRecvListBuff:");

                    foreach (IpcRecvListBuffDesc buff in Request.RecvListBuff)
                    {
                        sb.AppendLine($"\tPosition: 0x{buff.Position:x16} Size: 0x{buff.Size:x16}");
                    }
                }

                sb.AppendLine();
            }

            sb.AppendLine("Raw Request Data:");
            sb.Append(HexUtils.HexTable(Request.RawData));

            return sb.ToString();
        }

        private static (Type, MethodBase) WalkStackTrace(StackTrace trace)
        {
            int i = 0;

            StackFrame frame;

            // Find the IIpcService method that threw this exception
            while ((frame = trace.GetFrame(i++)) != null)
            {
                MethodBase method = frame.GetMethod();
                Type declType = method.DeclaringType;

                if (typeof(IpcService).IsAssignableFrom(declType))
                {
                    return (declType, method);
                }
            }

            return (null, null);
        }
    }
}

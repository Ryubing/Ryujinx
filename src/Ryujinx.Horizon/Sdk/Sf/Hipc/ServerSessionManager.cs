using Ryujinx.Common.Logging;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.OsTypes;
using Ryujinx.Horizon.Sdk.Sf.Cmif;
using Ryujinx.Horizon.Sdk.Sm;
using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Horizon.Sdk.Sf.Hipc
{
    class ServerSessionManager
    {
        public Result AcceptSession(int portHandle, ServiceObjectHolder obj)
        {
            return AcceptSession(out _, portHandle, obj);
        }

        private Result AcceptSession(out ServerSession session, int portHandle, ServiceObjectHolder obj)
        {
            return AcceptSessionImpl(out session, portHandle, obj);
        }

        private Result AcceptSessionImpl(out ServerSession session, int portHandle, ServiceObjectHolder obj)
        {
            session = null;

            Result result = HorizonStatic.Syscall.AcceptSession(out int sessionHandle, portHandle);

            if (result.IsFailure)
            {
                return result;
            }

            bool succeeded = false;

            try
            {
                result = RegisterSessionImpl(out session, sessionHandle, obj);

                if (result.IsFailure)
                {
                    return result;
                }

                succeeded = true;
            }
            finally
            {
                if (!succeeded)
                {
                    HorizonStatic.Syscall.CloseHandle(sessionHandle);
                }
            }

            return Result.Success;
        }

        public Result RegisterSession(int sessionHandle, ServiceObjectHolder obj)
        {
            return RegisterSession(out _, sessionHandle, obj);
        }

        public Result RegisterSession(out ServerSession session, int sessionHandle, ServiceObjectHolder obj)
        {
            return RegisterSessionImpl(out session, sessionHandle, obj);
        }

        private Result RegisterSessionImpl(out ServerSession session, int sessionHandle, ServiceObjectHolder obj)
        {
            Result result = CreateSessionImpl(out session, sessionHandle, obj);

            if (result.IsFailure)
            {
                return result;
            }

            session.PointerBuffer = GetSessionPointerBuffer(session);
            session.SavedMessage = GetSessionSavedMessageBuffer(session);

            RegisterSessionToWaitList(session);

            return Result.Success;
        }

        protected virtual void RegisterSessionToWaitList(ServerSession session)
        {
            throw new NotSupportedException();
        }

        private Result CreateSessionImpl(out ServerSession session, int sessionHandle, ServiceObjectHolder obj)
        {
            session = AllocateSession(sessionHandle, obj);

            if (session == null)
            {
                return HipcResult.OutOfSessionMemory;
            }

            return Result.Success;
        }

        protected virtual ServerSession AllocateSession(int sessionHandle, ServiceObjectHolder obj)
        {
            throw new NotSupportedException();
        }

        protected virtual void FreeSession(ServerSession session)
        {
            throw new NotSupportedException();
        }

        protected virtual Server AllocateServer(
            int portIndex,
            int portHandle,
            ServiceName name,
            bool managed,
            ServiceObjectHolder staticHoder)
        {
            throw new NotSupportedException();
        }

        protected virtual void DestroyServer(Server server)
        {
            throw new NotSupportedException();
        }

        protected virtual PointerAndSize GetSessionPointerBuffer(ServerSession session)
        {
            throw new NotSupportedException();
        }

        protected virtual PointerAndSize GetSessionSavedMessageBuffer(ServerSession session)
        {
            throw new NotSupportedException();
        }

        private void DestroySession(ServerSession session)
        {
            FreeSession(session);
        }

        protected void CloseSessionImpl(ServerSession session)
        {
            int sessionHandle = session.Handle;

            Os.FinalizeMultiWaitHolder(session);
            DestroySession(session);
            HorizonStatic.Syscall.CloseHandle(sessionHandle).AbortOnFailure();
        }

        private static CommandType GetCmifCommandType(ReadOnlySpan<byte> message)
        {
            return MemoryMarshal.Cast<byte, Header>(message)[0].Type;
        }

        public Result ProcessRequest(ServerSession session, Span<byte> message)
        {
            if (session.IsClosed || GetCmifCommandType(message) == CommandType.Close)
            {
                CloseSessionImpl(session);

                return Result.Success;
            }

            Result result = ProcessRequestImpl(session, message, message);

            if (result.IsSuccess)
            {
                RegisterSessionToWaitList(session);

                return Result.Success;
            }
            else if (SfResult.RequestContextChanged(result))
            {
                return result;
            }

            Logger.Warning?.Print(LogClass.KernelIpc, $"Request processing returned error {result}");

            CloseSessionImpl(session);

            return Result.Success;
        }

        private Result ProcessRequestImpl(ServerSession session, Span<byte> inMessage, Span<byte> outMessage)
        {
            CommandType commandType = GetCmifCommandType(inMessage);

            using ScopedInlineContextChange _ = new(GetInlineContext(commandType, inMessage));

            return commandType switch
            {
                CommandType.Request or CommandType.RequestWithContext => DispatchRequest(session.ServiceObjectHolder, session, inMessage, outMessage),
                CommandType.Control or CommandType.ControlWithContext => DispatchManagerRequest(session, inMessage, outMessage),
                _ => HipcResult.UnknownCommandType,
            };
        }

        private static int GetInlineContext(CommandType commandType, ReadOnlySpan<byte> inMessage)
        {
            switch (commandType)
            {
                case CommandType.RequestWithContext:
                case CommandType.ControlWithContext:
                    if (inMessage.Length >= 0x10)
                    {
                        return MemoryMarshal.Cast<byte, int>(inMessage)[3];
                    }
                    break;
            }

            return 0;
        }

        protected static Result ReceiveRequest(ServerSession session, Span<byte> message)
        {
            return ReceiveRequestImpl(session, message);
        }

        private static Result ReceiveRequestImpl(ServerSession session, Span<byte> message)
        {
            PointerAndSize pointerBuffer = session.PointerBuffer;

            while (true)
            {
                if (pointerBuffer.Address != 0)
                {
                    HipcMessageData messageData = HipcMessage.WriteMessage(message, new HipcMetadata
                    {
                        Type = (int)CommandType.Invalid,
                        ReceiveStaticsCount = HipcMessage.AutoReceiveStatic,
                    });

                    messageData.ReceiveList[0] = new HipcReceiveListEntry(pointerBuffer.Address, pointerBuffer.Size);
                }
                else
                {
                    MemoryMarshal.Cast<byte, Header>(message)[0] = new Header
                    {
                        Type = CommandType.Invalid,
                    };
                }

                Result result = Api.Receive(out ReceiveResult recvResult, session.Handle, message);

                if (result.IsFailure)
                {
                    return result;
                }

                switch (recvResult)
                {
                    case ReceiveResult.Success:
                        session.IsClosed = false;
                        return Result.Success;
                    case ReceiveResult.Closed:
                        session.IsClosed = true;
                        return Result.Success;
                }
            }
        }

        protected virtual Result DispatchManagerRequest(ServerSession session, Span<byte> inMessage, Span<byte> outMessage)
        {
            return SfResult.NotSupported;
        }

        protected virtual Result DispatchRequest(
            ServiceObjectHolder objectHolder,
            ServerSession session,
            Span<byte> inMessage,
            Span<byte> outMessage)
        {
            HipcMessage request;

            try
            {
                request = new HipcMessage(inMessage);
            }
            catch (ArgumentOutOfRangeException)
            {
                return HipcResult.InvalidRequestSize;
            }

            ServiceDispatchContext dispatchCtx = new()
            {
                ServiceObject = objectHolder.ServiceObject,
                Manager = this,
                Session = session,
                HandlesToClose = new HandlesToClose(),
                PointerBuffer = session.PointerBuffer,
                InMessageBuffer = inMessage,
                OutMessageBuffer = outMessage,
                Request = request,
            };

            ReadOnlySpan<byte> inRawData = MemoryMarshal.Cast<uint, byte>(dispatchCtx.Request.Data.DataWords);

            int inRawSize = dispatchCtx.Request.Meta.DataWordsCount * sizeof(uint);

            if (inRawSize < 0x10)
            {
                return HipcResult.InvalidRequestSize;
            }

            Result result = objectHolder.ProcessMessage(ref dispatchCtx, inRawData);

            if (result.IsFailure)
            {
                return result;
            }

            result = Api.Reply(session.SessionHandle, outMessage);

            ref HandlesToClose handlesToClose = ref dispatchCtx.HandlesToClose;

            for (int i = 0; i < handlesToClose.Count; i++)
            {
                HorizonStatic.Syscall.CloseHandle(handlesToClose[i]).AbortOnFailure();
            }

            return result;
        }

        public ServerSessionManager GetSessionManagerByTag(uint tag)
        {
            // Official FW does not do anything with the tag currently.
            return this;
        }
    }
}

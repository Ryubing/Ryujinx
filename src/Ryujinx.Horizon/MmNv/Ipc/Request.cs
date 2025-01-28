using Ryujinx.Common.Logging;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.MmNv;
using Ryujinx.Horizon.Sdk.Sf;
using System.Collections.Generic;

namespace Ryujinx.Horizon.MmNv.Ipc
{
    partial class Request : IRequest
    {
        private readonly List<Session> _sessionList = [];

        private uint _uniqueId = 1;

        [CmifCommand(0)]
        public Result InitializeOld(Module module, uint fgmPriority, uint autoClearEvent)
        {
            bool isAutoClearEvent = autoClearEvent != 0;

            Logger.Stub?.PrintStub(LogClass.ServiceMm, new { module, fgmPriority, isAutoClearEvent });

            Register(module, fgmPriority, isAutoClearEvent);

            return Result.Success;
        }

        [CmifCommand(1)]
        public Result FinalizeOld(Module module)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceMm, new { module });

            lock (_sessionList)
            {
                _sessionList.Remove(GetSessionByModule(module));
            }

            return Result.Success;
        }

        [CmifCommand(2)]
        public Result SetAndWaitOld(Module module, uint clockRateMin, int clockRateMax)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceMm, new { module, clockRateMin, clockRateMax });

            lock (_sessionList)
            {
                GetSessionByModule(module)?.SetAndWait(clockRateMin, clockRateMax);
            }

            return Result.Success;
        }

        [CmifCommand(3)]
        public Result GetOld(out uint clockRateActual, Module module)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceMm, new { module });

            lock (_sessionList)
            {
                Session session = GetSessionByModule(module);

                clockRateActual = session == null ? 0 : session.ClockRateMin;
            }

            return Result.Success;
        }

        [CmifCommand(4)]
        public Result Initialize(out uint requestId, Module module, uint fgmPriority, uint autoClearEvent)
        {
            bool isAutoClearEvent = autoClearEvent != 0;

            Logger.Stub?.PrintStub(LogClass.ServiceMm, new { module, fgmPriority, isAutoClearEvent });

            requestId = Register(module, fgmPriority, isAutoClearEvent);

            return Result.Success;
        }

        [CmifCommand(5)]
        public Result Finalize(uint requestId)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceMm, new { requestId });

            lock (_sessionList)
            {
                _sessionList.Remove(GetSessionById(requestId));
            }

            return Result.Success;
        }

        [CmifCommand(6)]
        public Result SetAndWait(uint requestId, uint clockRateMin, int clockRateMax)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceMm, new { requestId, clockRateMin, clockRateMax });

            lock (_sessionList)
            {
                GetSessionById(requestId)?.SetAndWait(clockRateMin, clockRateMax);
            }

            return Result.Success;
        }

        [CmifCommand(7)]
        public Result Get(out uint clockRateActual, uint requestId)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceMm, new { requestId });

            lock (_sessionList)
            {
                Session session = GetSessionById(requestId);

                clockRateActual = session == null ? 0 : session.ClockRateMin;
            }

            return Result.Success;
        }

        private Session GetSessionById(uint id)
        {
            foreach (Session session in _sessionList)
            {
                if (session.Id == id)
                {
                    return session;
                }
            }

            return null;
        }

        private Session GetSessionByModule(Module module)
        {
            foreach (Session session in _sessionList)
            {
                if (session.Module == module)
                {
                    return session;
                }
            }

            return null;
        }

        private uint Register(Module module, uint fgmPriority, bool isAutoClearEvent)
        {
            lock (_sessionList)
            {
                // Nintendo ignores the fgm priority as the other services were deprecated.
                Session session = new(_uniqueId++, module, isAutoClearEvent);

                _sessionList.Add(session);

                return session.Id;
            }
        }
    }
}

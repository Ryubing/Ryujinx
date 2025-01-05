using Ryujinx.HLE.HOS.Kernel.Threading;
using System.Collections.Generic;
using System.Threading;

namespace Ryujinx.HLE.HOS.Services.Time.Clock
{
    abstract class SystemClockContextUpdateCallback
    {
        private readonly List<KWritableEvent> _operationEventList;
        protected SystemClockContext Context;
        private bool _hasContext;

        public SystemClockContextUpdateCallback()
        {
            _operationEventList = [];
            Context = new SystemClockContext();
            _hasContext = false;
        }

        private bool NeedUpdate(SystemClockContext context)
        {
            if (_hasContext)
            {
                return Context.Offset != context.Offset || Context.SteadyTimePoint.ClockSourceId != context.SteadyTimePoint.ClockSourceId;
            }

            return true;
        }

        public void RegisterOperationEvent(KWritableEvent writableEvent)
        {
            Monitor.Enter(_operationEventList);
            _operationEventList.Add(writableEvent);
            Monitor.Exit(_operationEventList);
        }

        private void BroadcastOperationEvent()
        {
            Monitor.Enter(_operationEventList);

            foreach (KWritableEvent e in _operationEventList)
            {
                e.Signal();
            }

            Monitor.Exit(_operationEventList);
        }

        protected abstract ResultCode Update();

        public ResultCode Update(SystemClockContext context)
        {
            ResultCode result = ResultCode.Success;

            if (NeedUpdate(context))
            {
                Context = context;
                _hasContext = true;

                result = Update();

                if (result == ResultCode.Success)
                {
                    BroadcastOperationEvent();
                }
            }

            return result;
        }
    }
}

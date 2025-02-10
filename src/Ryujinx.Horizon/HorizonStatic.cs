using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Prepo.Types;
using Ryujinx.Memory;
using System;
using System.Threading;

namespace Ryujinx.Horizon
{
    public static class HorizonStatic
    {
        internal static void HandlePlayReport(PlayReport report) => 
            new Thread(() => PlayReport?.Invoke(report))
            {
                Name = "HLE.PlayReportEvent", 
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal
            }.Start();
        
        public static event Action<PlayReport> PlayReport;

        [field: ThreadStatic]
        public static HorizonOptions Options { get; private set; }

        [field: ThreadStatic]
        public static ISyscallApi Syscall { get; private set; }

        [field: ThreadStatic]
        public static IVirtualMemoryManager AddressSpace { get; private set; }

        [field: ThreadStatic]
        public static IThreadContext ThreadContext { get; private set; }

        [field: ThreadStatic]
        public static int CurrentThreadHandle { get; private set; }

        public static void Register(
            HorizonOptions options,
            ISyscallApi syscallApi,
            IVirtualMemoryManager addressSpace,
            IThreadContext threadContext,
            int threadHandle)
        {
            Options = options;
            Syscall = syscallApi;
            AddressSpace = addressSpace;
            ThreadContext = threadContext;
            CurrentThreadHandle = threadHandle;
        }
    }
}

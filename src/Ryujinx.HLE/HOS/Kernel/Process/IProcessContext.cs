using Ryujinx.Cpu;
using Ryujinx.Memory;
using System;

namespace Ryujinx.HLE.HOS.Kernel.Process
{
    interface IProcessContext : IDisposable
    {
        IVirtualMemoryManagerTracked AddressSpace { get; }

        ulong AddressSpaceSize { get; }

        IExecutionContext CreateExecutionContext(ExceptionCallbacks exceptionCallbacks);
        void Execute(IExecutionContext context, ulong codeAddress);
        void InvalidateCacheRegion(ulong address, ulong size);
    }
}

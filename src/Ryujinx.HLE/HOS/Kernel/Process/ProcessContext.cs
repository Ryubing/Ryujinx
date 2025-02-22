using Ryujinx.Cpu;
using Ryujinx.Memory;
using System;

namespace Ryujinx.HLE.HOS.Kernel.Process
{
    class ProcessContext : IProcessContext
    {
        public IVirtualMemoryManagerTracked AddressSpace { get; }

        public ulong AddressSpaceSize { get; }

        public ProcessContext(IVirtualMemoryManagerTracked asManager, ulong addressSpaceSize)
        {
            AddressSpace = asManager;
            AddressSpaceSize = addressSpaceSize;
        }

        public IExecutionContext CreateExecutionContext(ExceptionCallbacks exceptionCallbacks)
        {
            return new ProcessExecutionContext();
        }

        public void Execute(IExecutionContext context, ulong codeAddress)
        {
            throw new NotSupportedException();
        }

        public void InvalidateCacheRegion(ulong address, ulong size)
        {
        }

        public void Dispose()
        {
        }
    }
}

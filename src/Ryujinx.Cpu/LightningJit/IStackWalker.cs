using System.Collections.Generic;

namespace Ryujinx.Cpu.LightningJit
{
    interface IStackWalker
    {
        IEnumerable<ulong> GetCallStack(nint framePointer, nint codeRegionStart, int codeRegionSize, nint codeRegion2Start, int codeRegion2Size);
    }
}

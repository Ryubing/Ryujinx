using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Ryujinx.Cpu.LightningJit.CodeGen.Arm64
{
    class StackWalker : IStackWalker
    {
        public IEnumerable<ulong> GetCallStack(nint framePointer, nint codeRegionStart, int codeRegionSize, nint codeRegion2Start, int codeRegion2Size)
        {
            while (true)
            {
                nint functionPointer = Marshal.ReadIntPtr(framePointer, nint.Size);

                if ((functionPointer < codeRegionStart || functionPointer >= codeRegionStart + codeRegionSize) &&
                    (functionPointer < codeRegion2Start || functionPointer >= codeRegion2Start + codeRegion2Size))
                {
                    break;
                }

                yield return (ulong)functionPointer - 4;
                framePointer = Marshal.ReadIntPtr(framePointer);
            }
        }
    }
}

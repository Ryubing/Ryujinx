using System;

namespace ARMeilleure.Translation
{
    class DelegateInfo
    {
        public nint FuncPtr { get; private set; }
        public DelegateInfo(nint funcPtr)
        {
            FuncPtr = funcPtr;
        }
    }
}

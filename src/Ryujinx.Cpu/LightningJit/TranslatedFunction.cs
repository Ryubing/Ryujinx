namespace Ryujinx.Cpu.LightningJit
{
    class TranslatedFunction
    {
        public nint FuncPointer { get; }
        public ulong GuestSize { get; }

        public TranslatedFunction(nint funcPointer, ulong guestSize)
        {
            FuncPointer = funcPointer;
            GuestSize = guestSize;
        }
    }
}

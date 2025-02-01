namespace ARMeilleure.Translation
{
    delegate void DispatcherFunction(nint nativeContext, ulong startAddress);
    delegate ulong WrapperFunction(nint nativeContext, ulong startAddress);
}

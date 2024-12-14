namespace Ryujinx.HLE.HOS.Applets.SoftwareKeyboard
{
    /// <summary>
    /// Wraps a type in a class so it gets stored in the GC managed heap. This is used as communication mechanism
    /// between classes that need to be disposed and, thus, can't share their references.
    /// </summary>
    /// <typeparam name="T">The internal type.</typeparam>
    class TRef<T>
    {
        public T Value;

        public TRef() { }

        public TRef(T value)
        {
            Value = value;
        }
    }
}

namespace Ryujinx.Horizon.Common
{
    public interface IExternalEvent
    {
        void Signal();
        void Clear();
    }
}

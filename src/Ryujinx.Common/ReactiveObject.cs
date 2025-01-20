using Ryujinx.Common.Logging;
using System;
using System.Globalization;
using System.Threading;

namespace Ryujinx.Common
{
    public class ReactiveObject<T>
    {
        private readonly ReaderWriterLockSlim _rwLock = new();
        private bool _isInitialized;
        private T _value;

        public event EventHandler<ReactiveEventArgs<T>> Event;

        public T Value
        {
            get
            {
                _rwLock.EnterReadLock();
                T value = _value;
                _rwLock.ExitReadLock();

                return value;
            }
            set
            {
                _rwLock.EnterWriteLock();

                T oldValue = _value;

                bool oldIsInitialized = _isInitialized;

                _isInitialized = true;
                _value = value;

                _rwLock.ExitWriteLock();

                if (!oldIsInitialized || oldValue == null || !oldValue.Equals(_value))
                {
                    Event?.Invoke(this, new ReactiveEventArgs<T>(oldValue, value));
                }
            }
        }
        
        public void LogChangesToValue(string valueName, LogClass logClass = LogClass.Configuration) 
            => Event += (_, e) => ReactiveObjectHelper.LogValueChange(logClass, e, valueName);

        public static implicit operator T(ReactiveObject<T> obj) => obj.Value;
    }

    public static class ReactiveObjectHelper
    {
        public static void LogValueChange<T>(LogClass logClass, ReactiveEventArgs<T> eventArgs, string valueName)
        {
            if (eventArgs.AreValuesEqual)
                return;
            
            string message = string.Create(CultureInfo.InvariantCulture, $"{valueName} set to: {eventArgs.NewValue}");

            Logger.Info?.Print(logClass, message);
        }
        
        public static void Toggle(this ReactiveObject<bool> rBoolean) => rBoolean.Value = !rBoolean.Value;
    }

    public class ReactiveEventArgs<T>(T oldValue, T newValue)
    {
        public T OldValue { get; } = oldValue;
        public T NewValue { get; } = newValue;

        public bool AreValuesEqual
        {
            get
            {
                if (OldValue == null && NewValue == null)
                    return true;

                if (OldValue == null && NewValue != null)
                    return false;
                
                if (OldValue != null && NewValue == null)
                    return false;

                return OldValue!.Equals(NewValue);
            }
        }
    }
}

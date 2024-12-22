using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnRyu
{
    class NetworkTimeout : IDisposable
    {
        private readonly int _idleTimeout;
        private readonly Action _timeoutCallback;
        private CancellationTokenSource _cancel;

        private readonly Lock _lock = new();

        public NetworkTimeout(int idleTimeout, Action timeoutCallback)
        {
            _idleTimeout = idleTimeout;
            _timeoutCallback = timeoutCallback;
        }

        private async Task TimeoutTask()
        {
            CancellationTokenSource cts;

            lock (_lock)
            {
                cts = _cancel;
            }

            if (cts == null)
            {
                return;
            }

            try
            {
                await Task.Delay(_idleTimeout, cts.Token);
            }
            catch (TaskCanceledException)
            {
                return; // Timeout cancelled.
            }

            lock (_lock)
            {
                // Run the timeout callback. If the cancel token source has been replaced, we have _just_ been cancelled.
                if (cts == _cancel)
                {
                    _timeoutCallback();
                }
            }
        }

        public bool RefreshTimeout()
        {
            lock (_lock)
            {
                _cancel?.Cancel();

                _cancel = new CancellationTokenSource();

                Task.Run(TimeoutTask);
            }

            return true;
        }

        public void DisableTimeout()
        {
            lock (_lock)
            {
                _cancel?.Cancel();

                _cancel = new CancellationTokenSource();
            }
        }

        public void Dispose()
        {
            DisableTimeout();
        }
    }
}

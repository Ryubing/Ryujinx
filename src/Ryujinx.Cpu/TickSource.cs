using System;
using System.Diagnostics;

namespace Ryujinx.Cpu
{
    public class TickSource : ITickSource
    {
        private static Stopwatch _tickCounter;

        private static double _hostTickFreq;

        /// <inheritdoc/>
        public ulong Frequency { get; }

        /// <inheritdoc/>
        public ulong Counter => (ulong)(ElapsedSeconds * Frequency);
        
        
        public long TickScalar { get; set; }


        private static long _acumElapsedTicks;


        private static long _lastElapsedTicks;


        private long ElapsedTicks
        {
            get
            {
                long elapsedTicks = _tickCounter.ElapsedTicks;
                
                _acumElapsedTicks += (elapsedTicks - _lastElapsedTicks) * TickScalar / 100;

                _lastElapsedTicks = elapsedTicks;
                
                return _acumElapsedTicks;
            }
        }

        /// <inheritdoc/>
        
        public TimeSpan ElapsedTime => Stopwatch.GetElapsedTime(0, ElapsedTicks);

        /// <inheritdoc/>
        public double ElapsedSeconds => ElapsedTicks * _hostTickFreq;

        public TickSource(ulong frequency)
        {
            Frequency = frequency;
            _hostTickFreq = 1.0 / Stopwatch.Frequency;

            _tickCounter = new Stopwatch();
            _tickCounter.Start();
        }

        /// <inheritdoc/>
        public void Suspend()
        {
            _tickCounter.Stop();
        }

        /// <inheritdoc/>
        public void Resume()
        {
            _tickCounter.Start();
        }
    }
}

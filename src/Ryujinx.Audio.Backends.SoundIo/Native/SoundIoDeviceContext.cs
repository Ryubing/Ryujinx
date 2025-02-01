using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ryujinx.Audio.Backends.SoundIo.Native.SoundIo;

namespace Ryujinx.Audio.Backends.SoundIo.Native
{
    public class SoundIoDeviceContext
    {
        private readonly nint _context;

        public nint Context => _context;

        internal SoundIoDeviceContext(nint context)
        {
            _context = context;
        }

        private ref SoundIoDevice GetDeviceContext()
        {
            unsafe
            {
                return ref Unsafe.AsRef<SoundIoDevice>((SoundIoDevice*)_context);
            }
        }

        public bool IsRaw => GetDeviceContext().IsRaw;

        public string Id => Marshal.PtrToStringAnsi(GetDeviceContext().Id);

        public bool SupportsSampleRate(int sampleRate) => soundio_device_supports_sample_rate(_context, sampleRate);

        public bool SupportsFormat(SoundIoFormat format) => soundio_device_supports_format(_context, format);

        public bool SupportsChannelCount(int channelCount) => soundio_device_supports_layout(_context, SoundIoChannelLayout.GetDefault(channelCount));

        public SoundIoOutStreamContext CreateOutStream()
        {
            nint context = soundio_outstream_create(_context);

            if (context == nint.Zero)
            {
                return null;
            }

            return new SoundIoOutStreamContext(context);
        }
    }
}

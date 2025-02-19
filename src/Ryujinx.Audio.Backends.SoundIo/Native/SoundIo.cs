using Ryujinx.Common.Memory;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.Audio.Backends.SoundIo.Native
{
    public static partial class SoundIo
    {
        private const string LibraryName = "libsoundio";

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void OnDeviceChangeNativeDelegate(nint ctx);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void OnBackendDisconnectedDelegate(nint ctx, SoundIoError err);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void OnEventsSignalDelegate(nint ctx);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void EmitRtPrioWarningDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void JackCallbackDelegate(nint msg);

        [StructLayout(LayoutKind.Sequential)]
        public struct SoundIoStruct
        {
            public nint UserData;
            public nint OnDeviceChange;
            public nint OnBackendDisconnected;
            public nint OnEventsSignal;
            public SoundIoBackend CurrentBackend;
            public nint ApplicationName;
            public nint EmitRtPrioWarning;
            public nint JackInfoCallback;
            public nint JackErrorCallback;
        }

        public struct SoundIoChannelLayout
        {
            public nint Name;
            public int ChannelCount;
            public Array24<SoundIoChannelId> Channels;

            public static nint GetDefault(int channelCount)
            {
                return soundio_channel_layout_get_default(channelCount);
            }

            public static unsafe SoundIoChannelLayout GetDefaultValue(int channelCount)
            {
                return Unsafe.AsRef<SoundIoChannelLayout>((SoundIoChannelLayout*)GetDefault(channelCount));
            }
        }

        public struct SoundIoSampleRateRange
        {
            public int Min;
            public int Max;
        }

        public struct SoundIoDevice
        {
            public nint SoundIo;
            public nint Id;
            public nint Name;
            public SoundIoDeviceAim Aim;
            public nint Layouts;
            public int LayoutCount;
            public SoundIoChannelLayout CurrentLayout;
            public nint Formats;
            public int FormatCount;
            public SoundIoFormat CurrentFormat;
            public nint SampleRates;
            public int SampleRateCount;
            public int SampleRateCurrent;
            public double SoftwareLatencyMin;
            public double SoftwareLatencyMax;
            public double SoftwareLatencyCurrent;
            public bool IsRaw;
            public int RefCount;
            public SoundIoError ProbeError;
        }

        public struct SoundIoOutStream
        {
            public nint Device;
            public SoundIoFormat Format;
            public int SampleRate;
            public SoundIoChannelLayout Layout;
            public double SoftwareLatency;
            public float Volume;
            public nint UserData;
            public nint WriteCallback;
            public nint UnderflowCallback;
            public nint ErrorCallback;
            public nint Name;
            public bool NonTerminalHint;
            public int BytesPerFrame;
            public int BytesPerSample;
            public SoundIoError LayoutError;
        }

        public struct SoundIoChannelArea
        {
            public nint Pointer;
            public int Step;
        }

        [LibraryImport(LibraryName)]
        internal static partial nint soundio_create();

        [LibraryImport(LibraryName)]
        internal static partial SoundIoError soundio_connect(nint ctx);

        [LibraryImport(LibraryName)]
        internal static partial void soundio_disconnect(nint ctx);

        [LibraryImport(LibraryName)]
        internal static partial void soundio_flush_events(nint ctx);

        [LibraryImport(LibraryName)]
        internal static partial int soundio_output_device_count(nint ctx);

        [LibraryImport(LibraryName)]
        internal static partial int soundio_default_output_device_index(nint ctx);

        [LibraryImport(LibraryName)]
        internal static partial nint soundio_get_output_device(nint ctx, int index);

        [LibraryImport(LibraryName)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool soundio_device_supports_format(nint devCtx, SoundIoFormat format);

        [LibraryImport(LibraryName)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool soundio_device_supports_layout(nint devCtx, nint layout);

        [LibraryImport(LibraryName)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool soundio_device_supports_sample_rate(nint devCtx, int sampleRate);

        [LibraryImport(LibraryName)]
        internal static partial nint soundio_outstream_create(nint devCtx);

        [LibraryImport(LibraryName)]
        internal static partial SoundIoError soundio_outstream_open(nint outStreamCtx);

        [LibraryImport(LibraryName)]
        internal static partial SoundIoError soundio_outstream_start(nint outStreamCtx);

        [LibraryImport(LibraryName)]
        internal static partial SoundIoError soundio_outstream_begin_write(nint outStreamCtx, nint areas, nint frameCount);

        [LibraryImport(LibraryName)]
        internal static partial SoundIoError soundio_outstream_end_write(nint outStreamCtx);

        [LibraryImport(LibraryName)]
        internal static partial SoundIoError soundio_outstream_pause(nint devCtx, [MarshalAs(UnmanagedType.Bool)] bool pause);

        [LibraryImport(LibraryName)]
        internal static partial SoundIoError soundio_outstream_set_volume(nint devCtx, double volume);

        [LibraryImport(LibraryName)]
        internal static partial void soundio_outstream_destroy(nint streamCtx);

        [LibraryImport(LibraryName)]
        internal static partial void soundio_destroy(nint ctx);

        [LibraryImport(LibraryName)]
        internal static partial nint soundio_channel_layout_get_default(int channelCount);

        [LibraryImport(LibraryName)]
        internal static partial nint soundio_strerror(SoundIoError err);
    }
}

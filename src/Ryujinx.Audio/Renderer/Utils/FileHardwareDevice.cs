using Ryujinx.Audio.Integration;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Ryujinx.Audio.Renderer.Utils
{
    /// <summary>
    /// A <see cref="IHardwareDevice"/> that outputs to a wav file.
    /// </summary>
    public class FileHardwareDevice : IHardwareDevice
    {
        private FileStream _stream;
        private readonly uint _channelCount;
        private readonly uint _sampleRate;

        private const int HeaderSize = 44;

        public FileHardwareDevice(string path, uint channelCount, uint sampleRate)
        {
            _stream = File.OpenWrite(path);
            _channelCount = channelCount;
            _sampleRate = sampleRate;

            _stream.Seek(HeaderSize, SeekOrigin.Begin);
        }

        private void UpdateHeader()
        {
            BinaryWriter writer = new(_stream);

            long currentPos = writer.Seek(0, SeekOrigin.Current);

            writer.Seek(0, SeekOrigin.Begin);

            writer.Write("RIFF"u8);
            writer.Write((int)(writer.BaseStream.Length - 8));
            writer.Write("WAVE"u8);
            writer.Write("fmt "u8);
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)GetChannelCount());
            writer.Write(GetSampleRate());
            writer.Write(GetSampleRate() * GetChannelCount() * sizeof(short));
            writer.Write((short)(GetChannelCount() * sizeof(short)));
            writer.Write((short)(sizeof(short) * 8));
            writer.Write("data"u8);
            writer.Write((int)(writer.BaseStream.Length - HeaderSize));

            writer.Seek((int)currentPos, SeekOrigin.Begin);
        }

        public void AppendBuffer(ReadOnlySpan<short> data, uint channelCount)
        {
            _stream.Write(MemoryMarshal.Cast<short, byte>(data));

            UpdateHeader();
            _stream.Flush();
        }

        public void SetVolume(float volume)
        {
            // Do nothing, volume is not used for FileHardwareDevice at the moment.
        }

        public float GetVolume()
        {
            // FileHardwareDevice does not incorporate volume.
            return 0;
        }

        public uint GetChannelCount()
        {
            return _channelCount;
        }

        public uint GetSampleRate()
        {
            return _sampleRate;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stream?.Flush();
                _stream?.Dispose();

                _stream = null;
            }
        }
    }
}

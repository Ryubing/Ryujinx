using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;

namespace Ryujinx.Common
{
    public static class StreamExtensions
    {
        /// <summary>
        /// Writes an int span to this stream.
        ///
        /// This default implementation converts each buffer value to a stack-allocated
        /// byte array, then writes it to the Stream using <see cref="Stream.Write(ReadOnlySpan{byte})" />.
        /// </summary>
        /// <param name="stream">The stream to be written to</param>
        /// <param name="buffer">The buffer of values to be written</param>
        public static void Write(this Stream stream, ReadOnlySpan<int> buffer)
        {
            if (buffer.Length == 0)
            {
                return;
            }

            if (BitConverter.IsLittleEndian)
            {
                ReadOnlySpan<byte> byteBuffer = MemoryMarshal.Cast<int, byte>(buffer);
                stream.Write(byteBuffer);
            }
            else
            {
                Span<byte> byteBuffer = stackalloc byte[sizeof(int)];

                foreach (int value in buffer)
                {
                    BinaryPrimitives.WriteInt32LittleEndian(byteBuffer, value);
                    stream.Write(byteBuffer);
                }
            }
        }

        /// <summary>
        /// Writes a four-byte signed integer to this stream. The current position
        /// of the stream is advanced by four.
        /// </summary>
        /// <param name="stream">The stream to be written to</param>
        /// <param name="value">The value to be written</param>
        public static void Write(this Stream stream, int value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
            stream.Write(buffer);
        }

        /// <summary>
        /// Writes an eight-byte signed integer to this stream. The current position
        /// of the stream is advanced by eight.
        /// </summary>
        /// <param name="stream">The stream to be written to</param>
        /// <param name="value">The value to be written</param>
        public static void Write(this Stream stream, long value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(long)];
            BinaryPrimitives.WriteInt64LittleEndian(buffer, value);
            stream.Write(buffer);
        }

        /// <summary>
        /// Writes a four-byte unsigned integer to this stream. The current position
        /// of the stream is advanced by four.
        /// </summary>
        /// <param name="stream">The stream to be written to</param>
        /// <param name="value">The value to be written</param>
        public static void Write(this Stream stream, uint value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
            stream.Write(buffer);
        }

        /// <summary>
        /// Writes an eight-byte unsigned integer to this stream. The current
        /// position of the stream is advanced by eight.
        /// </summary>
        /// <param name="stream">The stream to be written to</param>
        /// <param name="value">The value to be written</param>
        public static void Write(this Stream stream, ulong value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ulong)];
            BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
            stream.Write(buffer);
        }

        /// <summary>
        /// Writes the contents of source to stream by calling source.CopyTo(stream).
        /// Provides consistency with other Stream.Write methods.
        /// </summary>
        /// <param name="stream">The stream to be written to</param>
        /// <param name="source">The stream to be read from</param>
        public static void Write(this Stream stream, Stream source)
        {
            source.CopyTo(stream);
        }

        /// <summary>
        /// Writes a sequence of bytes to the Stream.
        /// </summary>
        /// <param name="stream">The stream to be written to.</param>
        /// <param name="value">The byte to be written</param>
        /// <param name="count">The number of times the value should be written</param>
        public static void WriteByte(this Stream stream, byte value, int count)
        {
            if (count <= 0)
            {
                return;
            }

            const int BlockSize = 16;

            int blockCount = count / BlockSize;
            if (blockCount > 0)
            {
                Span<byte> span = stackalloc byte[BlockSize];
                span.Fill(value);
                for (int x = 0; x < blockCount; x++)
                {
                    stream.Write(span);
                }
            }

            int nonBlockBytes = count % BlockSize;
            for (int x = 0; x < nonBlockBytes; x++)
            {
                stream.WriteByte(value);
            }
        }
    }
}

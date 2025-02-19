using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.Common.Extensions
{
    public static class SequenceReaderExtensions
    {
        /// <summary>
        /// Dumps the entire <see cref="SequenceReader{byte}"/> to a file, restoring its previous location afterward.
        /// Useful for debugging purposes.
        /// </summary>
        /// <param name="reader">The <see cref="SequenceReader{Byte}"/> to write to a file</param>
        /// <param name="fileFullName">The path and name of the file to create and dump to</param>
        public static void DumpToFile(this ref SequenceReader<byte> reader, string fileFullName)
        {
            long initialConsumed = reader.Consumed;

            reader.Rewind(initialConsumed);

            using (FileStream fileStream = System.IO.File.Create(fileFullName, 4096, System.IO.FileOptions.None))
            {
                while (reader.End == false)
                {
                    ReadOnlySpan<byte> span = reader.CurrentSpan;
                    fileStream.Write(span);
                    reader.Advance(span.Length);
                }
            }

            reader.SetConsumed(initialConsumed);
        }

        /// <summary>
        /// Returns a reference to the desired value. This ref should always be used. The argument passed in <paramref name="copyDestinationIfRequiredDoNotUse"/> should never be used, as this is only used for storage if the value
        /// must be copied from multiple <see cref="ReadOnlyMemory{Byte}"/> segments held by the <see cref="SequenceReader{Byte}"/>.
        /// </summary>
        /// <typeparam name="T">Type to get</typeparam>
        /// <param name="reader">The <see cref="SequenceReader{Byte}"/> to read from</param>
        /// <param name="copyDestinationIfRequiredDoNotUse">A location used as storage if (and only if) the value to be read spans multiple <see cref="ReadOnlyMemory{Byte}"/> segments</param>
        /// <returns>A reference to the desired value, either directly to memory in the <see cref="SequenceReader{Byte}"/>, or to <paramref name="copyDestinationIfRequiredDoNotUse"/> if it has been used for copying the value in to</returns>
        /// <remarks>
        /// DO NOT use <paramref name="copyDestinationIfRequiredDoNotUse"/> after calling this method, as it will only
        /// contain a value if the value couldn't be referenced directly because it spans multiple <see cref="ReadOnlyMemory{Byte}"/> segments.
        /// To discourage use, it is recommended to call this method like the following:
        /// <c>
        ///     ref readonly MyStruct value = ref sequenceReader.GetRefOrRefToCopy{MyStruct}(out _);
        /// </c>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">The <see cref="SequenceReader{Byte}"/> does not contain enough data to read a value of type <typeparamref name="T"/></exception>
        public static ref readonly T GetRefOrRefToCopy<T>(this scoped ref SequenceReader<byte> reader, out T copyDestinationIfRequiredDoNotUse) where T : unmanaged
        {
            int lengthRequired = Unsafe.SizeOf<T>();

            ReadOnlySpan<byte> span = reader.UnreadSpan;
            if (lengthRequired <= span.Length)
            {
                reader.Advance(lengthRequired);

                copyDestinationIfRequiredDoNotUse = default;

                ReadOnlySpan<T> spanOfT = MemoryMarshal.Cast<byte, T>(span);

                return ref spanOfT[0];
            }
            else
            {
                copyDestinationIfRequiredDoNotUse = default;

                Span<T> valueSpan = MemoryMarshal.CreateSpan(ref copyDestinationIfRequiredDoNotUse, 1);

                Span<byte> valueBytesSpan = MemoryMarshal.AsBytes(valueSpan);

                if (!reader.TryCopyTo(valueBytesSpan))
                {
                    throw new ArgumentOutOfRangeException(nameof(reader), "The sequence is not long enough to read the desired value.");
                }

                reader.Advance(lengthRequired);

                return ref valueSpan[0];
            }
        }

        /// <summary>
        /// Reads an <see cref="int"/> as little endian.
        /// </summary>
        /// <param name="reader">The <see cref="SequenceReader{Byte}"/> to read from</param>
        /// <param name="value">A location to receive the read value</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if there wasn't enough data for an <see cref="int"/></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadLittleEndian(this ref SequenceReader<byte> reader, out int value)
        {
            if (!reader.TryReadLittleEndian(out value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), "The sequence is not long enough to read the desired value.");
            }
        }

        /// <summary>
        /// Reads the desired unmanaged value by copying it to the specified <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="T">Type to read</typeparam>
        /// <param name="reader">The <see cref="SequenceReader{Byte}"/> to read from</param>
        /// <param name="value">The target that will receive the read value</param>
        /// <exception cref="ArgumentOutOfRangeException">The <see cref="SequenceReader{Byte}"/> does not contain enough data to read a value of type <typeparamref name="T"/></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadUnmanaged<T>(this ref SequenceReader<byte> reader, out T value) where T : unmanaged
        {
            if (!reader.TryReadUnmanaged(out value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), "The sequence is not long enough to read the desired value.");
            }
        }

        /// <summary>
        /// Sets the reader's position as bytes consumed.
        /// </summary>
        /// <param name="reader">The <see cref="SequenceReader{Byte}"/> to set the position</param>
        /// <param name="consumed">The number of bytes consumed</param>
        public static void SetConsumed(ref this SequenceReader<byte> reader, long consumed)
        {
            reader.Rewind(reader.Consumed);
            reader.Advance(consumed);
        }

        /// <summary>
        /// Try to read the given type out of the buffer if possible. Warning: this is dangerous to use with arbitrary
        /// structs - see remarks for full details.
        /// </summary>
        /// <typeparam name="T">Type to read</typeparam>
        /// <remarks>
        /// IMPORTANT: The read is a straight copy of bits. If a struct depends on specific state of it's members to
        /// behave correctly this can lead to exceptions, etc. If reading endian specific integers, use the explicit
        /// overloads such as <see cref="SequenceReader{T}.TryReadLittleEndian"/>
        /// </remarks>
        /// <returns>
        /// True if successful. <paramref name="value"/> will be default if failed (due to lack of space).
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool TryReadUnmanaged<T>(ref this SequenceReader<byte> reader, out T value) where T : unmanaged
        {
            ReadOnlySpan<byte> span = reader.UnreadSpan;

            if (span.Length < sizeof(T))
            {
                return TryReadUnmanagedMultiSegment(ref reader, out value);
            }

            value = Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(span));

            reader.Advance(sizeof(T));

            return true;
        }

        private static unsafe bool TryReadUnmanagedMultiSegment<T>(ref SequenceReader<byte> reader, out T value) where T : unmanaged
        {
            Debug.Assert(reader.UnreadSpan.Length < sizeof(T));

            // Not enough data in the current segment, try to peek for the data we need.
            T buffer = default;

            Span<byte> tempSpan = new(&buffer, sizeof(T));

            if (!reader.TryCopyTo(tempSpan))
            {
                value = default;
                return false;
            }

            value = Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(tempSpan));

            reader.Advance(sizeof(T));

            return true;
        }
    }
}

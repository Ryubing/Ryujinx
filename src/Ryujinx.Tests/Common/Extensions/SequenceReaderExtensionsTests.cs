using NUnit.Framework;
using Ryujinx.Common.Extensions;
using Ryujinx.Memory;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.Tests.Common.Extensions
{
    public class SequenceReaderExtensionsTests
    {
        [TestCase(null)]
        [TestCase(sizeof(int) + 1)]
        public void GetRefOrRefToCopy_ReadsMultiSegmentedSequenceSuccessfully(int? maxSegmentSize)
        {
            // Arrange
            MyUnmanagedStruct[] originalStructs = EnumerateNewUnmanagedStructs().Take(3).ToArray();

            ReadOnlySequence<byte> sequence =
                CreateSegmentedByteSequence(originalStructs, maxSegmentSize ?? Unsafe.SizeOf<MyUnmanagedStruct>());

            SequenceReader<byte> sequenceReader = new(sequence);

            foreach (MyUnmanagedStruct original in originalStructs)
            {
                // Act
                ref readonly MyUnmanagedStruct read = ref sequenceReader.GetRefOrRefToCopy<MyUnmanagedStruct>(out _);

                // Assert
                MyUnmanagedStruct.Assert(Assert.AreEqual, original, read);
            }
        }

        [Test]
        public void GetRefOrRefToCopy_FragmentedSequenceReturnsRefToCopy()
        {
            // Arrange
            MyUnmanagedStruct[] originalStructs = EnumerateNewUnmanagedStructs().Take(1).ToArray();

            ReadOnlySequence<byte> sequence = CreateSegmentedByteSequence(originalStructs, 3);

            SequenceReader<byte> sequenceReader = new(sequence);

            foreach (MyUnmanagedStruct original in originalStructs)
            {
                // Act
                ref readonly MyUnmanagedStruct read = ref sequenceReader.GetRefOrRefToCopy<MyUnmanagedStruct>(out MyUnmanagedStruct copy);

                // Assert
                MyUnmanagedStruct.Assert(Assert.AreEqual, original, read);
                MyUnmanagedStruct.Assert(Assert.AreEqual, read, copy);
            }
        }

        [Test]
        public void GetRefOrRefToCopy_ContiguousSequenceReturnsRefToBuffer()
        {
            // Arrange
            MyUnmanagedStruct[] originalStructs = EnumerateNewUnmanagedStructs().Take(1).ToArray();

            ReadOnlySequence<byte> sequence = CreateSegmentedByteSequence(originalStructs, int.MaxValue);

            SequenceReader<byte> sequenceReader = new(sequence);

            foreach (MyUnmanagedStruct original in originalStructs)
            {
                // Act
                ref readonly MyUnmanagedStruct read = ref sequenceReader.GetRefOrRefToCopy<MyUnmanagedStruct>(out MyUnmanagedStruct copy);

                // Assert
                MyUnmanagedStruct.Assert(Assert.AreEqual, original, read);
                MyUnmanagedStruct.Assert(Assert.AreNotEqual, read, copy);
            }
        }

        [Test]
        public void GetRefOrRefToCopy_ThrowsWhenNotEnoughData()
        {
            // Arrange
            MyUnmanagedStruct[] originalStructs = EnumerateNewUnmanagedStructs().Take(1).ToArray();

            ReadOnlySequence<byte> sequence = CreateSegmentedByteSequence(originalStructs, int.MaxValue);

            // Act/Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                SequenceReader<byte> sequenceReader = new(sequence);

                sequenceReader.Advance(1);

                ref readonly MyUnmanagedStruct result = ref sequenceReader.GetRefOrRefToCopy<MyUnmanagedStruct>(out _);
            });
        }

        [Test]
        public void ReadLittleEndian_Int32_RoundTripsSuccessfully()
        {
            // Arrange
            const int TestValue = 0x1234abcd;

            byte[] buffer = new byte[sizeof(int)];

            BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(), TestValue);

            SequenceReader<byte> sequenceReader = new(new ReadOnlySequence<byte>(buffer));

            // Act
            sequenceReader.ReadLittleEndian(out int roundTrippedValue);

            // Assert
            Assert.AreEqual(TestValue, roundTrippedValue);
        }

        [Test]
        public void ReadLittleEndian_Int32_ResultIsNotBigEndian()
        {
            // Arrange
            const int TestValue = 0x1234abcd;

            byte[] buffer = new byte[sizeof(int)];

            BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(), TestValue);

            SequenceReader<byte> sequenceReader = new(new ReadOnlySequence<byte>(buffer));

            // Act
            sequenceReader.ReadLittleEndian(out int roundTrippedValue);

            // Assert
            Assert.AreNotEqual(TestValue, roundTrippedValue);
        }

        [Test]
        public void ReadLittleEndian_Int32_ThrowsWhenNotEnoughData()
        {
            // Arrange
            const int TestValue = 0x1234abcd;

            byte[] buffer = new byte[sizeof(int)];

            BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(), TestValue);

            // Act/Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                SequenceReader<byte> sequenceReader = new(new ReadOnlySequence<byte>(buffer));
                sequenceReader.Advance(1);

                sequenceReader.ReadLittleEndian(out int roundTrippedValue);
            });
        }

        [Test]
        public void ReadUnmanaged_ContiguousSequence_Succeeds()
            => ReadUnmanaged_Succeeds(int.MaxValue);

        [Test]
        public void ReadUnmanaged_FragmentedSequence_Succeeds()
            => ReadUnmanaged_Succeeds(sizeof(int) + 1);

        [Test]
        public void ReadUnmanaged_ThrowsWhenNotEnoughData()
        {
            // Arrange
            MyUnmanagedStruct[] originalStructs = EnumerateNewUnmanagedStructs().Take(1).ToArray();

            ReadOnlySequence<byte> sequence = CreateSegmentedByteSequence(originalStructs, int.MaxValue);

            // Act/Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                SequenceReader<byte> sequenceReader = new(sequence);

                sequenceReader.Advance(1);

                sequenceReader.ReadUnmanaged(out MyUnmanagedStruct read);
            });
        }

        [Test]
        public void SetConsumed_ContiguousSequence_SucceedsWhenValid()
            => SetConsumed_SucceedsWhenValid(int.MaxValue);

        [Test]
        public void SetConsumed_FragmentedSequence_SucceedsWhenValid()
            => SetConsumed_SucceedsWhenValid(sizeof(int) + 1);

        [Test]
        public void SetConsumed_ThrowsWhenBeyondActualLength()
        {
            const int StructCount = 2;
            // Arrange
            MyUnmanagedStruct[] originalStructs = EnumerateNewUnmanagedStructs().Take(StructCount).ToArray();

            ReadOnlySequence<byte> sequence = CreateSegmentedByteSequence(originalStructs, MyUnmanagedStruct.SizeOf);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                SequenceReader<byte> sequenceReader = new(sequence);

                sequenceReader.SetConsumed(MyUnmanagedStruct.SizeOf * StructCount + 1);
            });
        }

        private static void ReadUnmanaged_Succeeds(int maxSegmentLength)
        {
            // Arrange
            MyUnmanagedStruct[] originalStructs = EnumerateNewUnmanagedStructs().Take(3).ToArray();

            ReadOnlySequence<byte> sequence = CreateSegmentedByteSequence(originalStructs, maxSegmentLength);

            SequenceReader<byte> sequenceReader = new(sequence);

            foreach (MyUnmanagedStruct original in originalStructs)
            {
                // Act
                sequenceReader.ReadUnmanaged(out MyUnmanagedStruct read);

                // Assert
                MyUnmanagedStruct.Assert(Assert.AreEqual, original, read);
            }
        }

        private static void SetConsumed_SucceedsWhenValid(int maxSegmentLength)
        {
            // Arrange
            MyUnmanagedStruct[] originalStructs = EnumerateNewUnmanagedStructs().Take(2).ToArray();

            ReadOnlySequence<byte> sequence = CreateSegmentedByteSequence(originalStructs, maxSegmentLength);

            SequenceReader<byte> sequenceReader = new(sequence);

            static void SetConsumedAndAssert(scoped ref SequenceReader<byte> sequenceReader, long consumed)
            {
                sequenceReader.SetConsumed(consumed);
                Assert.AreEqual(consumed, sequenceReader.Consumed);
            }

            // Act/Assert
            ref readonly MyUnmanagedStruct struct0A = ref sequenceReader.GetRefOrRefToCopy<MyUnmanagedStruct>(out _);

            Assert.AreEqual(sequenceReader.Consumed, MyUnmanagedStruct.SizeOf);

            SetConsumedAndAssert(ref sequenceReader, 0);

            ref readonly MyUnmanagedStruct struct0B = ref sequenceReader.GetRefOrRefToCopy<MyUnmanagedStruct>(out _);

            MyUnmanagedStruct.Assert(Assert.AreEqual, struct0A, struct0B);

            SetConsumedAndAssert(ref sequenceReader, 1);

            SetConsumedAndAssert(ref sequenceReader, MyUnmanagedStruct.SizeOf);

            ref readonly MyUnmanagedStruct struct1A = ref sequenceReader.GetRefOrRefToCopy<MyUnmanagedStruct>(out _);

            SetConsumedAndAssert(ref sequenceReader, MyUnmanagedStruct.SizeOf);

            ref readonly MyUnmanagedStruct struct1B = ref sequenceReader.GetRefOrRefToCopy<MyUnmanagedStruct>(out _);

            MyUnmanagedStruct.Assert(Assert.AreEqual, struct1A, struct1B);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct MyUnmanagedStruct
        {
            public int BehaviourSize;
            public int MemoryPoolsSize;
            public short VoicesSize;
            public int VoiceResourcesSize;
            public short EffectsSize;
            public int RenderInfoSize;

            public unsafe fixed byte Reserved[16];

            public static readonly int SizeOf = Unsafe.SizeOf<MyUnmanagedStruct>();

            public static unsafe MyUnmanagedStruct Generate(Random rng)
            {
                const int BaseInt32Value = 0x1234abcd;
                const short BaseInt16Value = 0x5678;

                MyUnmanagedStruct result = new()
                {
                    BehaviourSize = BaseInt32Value ^ rng.Next(),
                    MemoryPoolsSize = BaseInt32Value ^ rng.Next(),
                    VoicesSize = (short)(BaseInt16Value ^ rng.Next()),
                    VoiceResourcesSize = BaseInt32Value ^ rng.Next(),
                    EffectsSize = (short)(BaseInt16Value ^ rng.Next()),
                    RenderInfoSize = BaseInt32Value ^ rng.Next(),
                };

                Unsafe.Write(result.Reserved, rng.NextInt64());

                return result;
            }

            public static unsafe void Assert(Action<object, object> assert, in MyUnmanagedStruct expected, in MyUnmanagedStruct actual)
            {
                assert(expected.BehaviourSize, actual.BehaviourSize);
                assert(expected.MemoryPoolsSize, actual.MemoryPoolsSize);
                assert(expected.VoicesSize, actual.VoicesSize);
                assert(expected.VoiceResourcesSize, actual.VoiceResourcesSize);
                assert(expected.EffectsSize, actual.EffectsSize);
                assert(expected.RenderInfoSize, actual.RenderInfoSize);

                fixed (void* expectedReservedPtr = expected.Reserved)
                fixed (void* actualReservedPtr = actual.Reserved)
                {
                    long expectedReservedLong = Unsafe.Read<long>(expectedReservedPtr);
                    long actualReservedLong = Unsafe.Read<long>(actualReservedPtr);

                    assert(expectedReservedLong, actualReservedLong);
                }
            }
        }

        private static IEnumerable<MyUnmanagedStruct> EnumerateNewUnmanagedStructs()
        {
            Random rng = new(0);

            while (true)
            {
                yield return MyUnmanagedStruct.Generate(rng);
            }
        }

        private static ReadOnlySequence<byte> CreateSegmentedByteSequence<T>(T[] array, int maxSegmentLength) where T : unmanaged
        {
            byte[] arrayBytes = MemoryMarshal.AsBytes(array.AsSpan()).ToArray();
            Memory<byte> memory = new(arrayBytes);
            int index = 0;

            BytesReadOnlySequenceSegment first = null, last = null;

            while (index < memory.Length)
            {
                int nextSegmentLength = Math.Min(maxSegmentLength, memory.Length - index);
                Memory<byte> nextSegment = memory.Slice(index, nextSegmentLength);

                if (first == null)
                {
                    first = last = new BytesReadOnlySequenceSegment(nextSegment);
                }
                else
                {
                    last = last.Append(nextSegment);
                }

                index += nextSegmentLength;
            }

            return new ReadOnlySequence<byte>(first, 0, last, (int)(memory.Length - last.RunningIndex));
        }
    }
}

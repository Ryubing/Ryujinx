using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ARMeilleure.IntermediateRepresentation
{
    unsafe struct Operation : IEquatable<Operation>, IIntrusiveListNode<Operation>
    {
        internal struct Data
        {
            public ushort Instruction;
            public ushort Intrinsic;
            public ushort SourcesCount;
            public ushort DestinationsCount;
            public Operation ListPrevious;
            public Operation ListNext;
            public Operand* Destinations;
            public Operand* Sources;
        }

        private Data* _data;

        public readonly Instruction Instruction
        {
            get => (Instruction)_data->Instruction;
            private set => _data->Instruction = (ushort)value;
        }

        public readonly Intrinsic Intrinsic
        {
            get => (Intrinsic)_data->Intrinsic;
            private set => _data->Intrinsic = (ushort)value;
        }

        public readonly Operation ListPrevious
        {
            get => _data->ListPrevious;
            set => _data->ListPrevious = value;
        }

        public readonly Operation ListNext
        {
            get => _data->ListNext;
            set => _data->ListNext = value;
        }

        public readonly Operand Destination
        {
            get => _data->DestinationsCount != 0 ? GetDestination(0) : default;
            set => SetDestination(value);
        }

        public readonly int DestinationsCount => _data->DestinationsCount;
        public readonly int SourcesCount => _data->SourcesCount;

        internal readonly Span<Operand> DestinationsUnsafe => new(_data->Destinations, _data->DestinationsCount);
        internal readonly Span<Operand> SourcesUnsafe => new(_data->Sources, _data->SourcesCount);

        public readonly PhiOperation AsPhi()
        {
            Debug.Assert(Instruction == Instruction.Phi);

            return new PhiOperation(this);
        }

        public readonly Operand GetDestination(int index)
        {
            return DestinationsUnsafe[index];
        }

        public readonly Operand GetSource(int index)
        {
            return SourcesUnsafe[index];
        }

        public readonly void SetDestination(int index, Operand dest)
        {
            ref Operand curDest = ref DestinationsUnsafe[index];

            RemoveAssignment(curDest);
            AddAssignment(dest);

            curDest = dest;
        }

        public readonly void SetSource(int index, Operand src)
        {
            ref Operand curSrc = ref SourcesUnsafe[index];

            RemoveUse(curSrc);
            AddUse(src);

            curSrc = src;
        }

        private readonly void RemoveOldDestinations()
        {
            for (int i = 0; i < _data->DestinationsCount; i++)
            {
                RemoveAssignment(_data->Destinations[i]);
            }
        }

        public readonly void SetDestination(Operand dest)
        {
            RemoveOldDestinations();

            if (dest == default)
            {
                _data->DestinationsCount = 0;
            }
            else
            {
                EnsureCapacity(ref _data->Destinations, ref _data->DestinationsCount, 1);

                _data->Destinations[0] = dest;

                AddAssignment(dest);
            }
        }

        public readonly void SetDestinations(Operand[] dests)
        {
            RemoveOldDestinations();

            EnsureCapacity(ref _data->Destinations, ref _data->DestinationsCount, dests.Length);

            for (int index = 0; index < dests.Length; index++)
            {
                Operand newOp = dests[index];

                _data->Destinations[index] = newOp;

                AddAssignment(newOp);
            }
        }

        private readonly void RemoveOldSources()
        {
            for (int index = 0; index < _data->SourcesCount; index++)
            {
                RemoveUse(_data->Sources[index]);
            }
        }

        public readonly void SetSource(Operand src)
        {
            RemoveOldSources();

            if (src == default)
            {
                _data->SourcesCount = 0;
            }
            else
            {
                EnsureCapacity(ref _data->Sources, ref _data->SourcesCount, 1);

                _data->Sources[0] = src;

                AddUse(src);
            }
        }

        public readonly void SetSources(Operand[] srcs)
        {
            RemoveOldSources();

            EnsureCapacity(ref _data->Sources, ref _data->SourcesCount, srcs.Length);

            for (int index = 0; index < srcs.Length; index++)
            {
                Operand newOp = srcs[index];

                _data->Sources[index] = newOp;

                AddUse(newOp);
            }
        }

        public void TurnIntoCopy(Operand source)
        {
            Instruction = Instruction.Copy;

            SetSource(source);
        }

        private readonly void AddAssignment(Operand op)
        {
            if (op != default)
            {
                op.AddAssignment(this);
            }
        }

        private readonly void RemoveAssignment(Operand op)
        {
            if (op != default)
            {
                op.RemoveAssignment(this);
            }
        }

        private readonly void AddUse(Operand op)
        {
            if (op != default)
            {
                op.AddUse(this);
            }
        }

        private readonly void RemoveUse(Operand op)
        {
            if (op != default)
            {
                op.RemoveUse(this);
            }
        }

        public readonly bool Equals(Operation operation)
        {
            return operation._data == _data;
        }

        public readonly override bool Equals(object obj)
        {
            return obj is Operation operation && Equals(operation);
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine((nint)_data);
        }

        public static bool operator ==(Operation a, Operation b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Operation a, Operation b)
        {
            return !a.Equals(b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EnsureCapacity(ref Operand* list, ref ushort capacity, int newCapacity)
        {
            if (newCapacity > ushort.MaxValue)
            {
                ThrowOverflow(newCapacity);
            }
            // We only need to allocate a new buffer if we're increasing the size.
            else if (newCapacity > capacity)
            {
                list = Allocators.References.Allocate<Operand>((uint)newCapacity);
            }

            capacity = (ushort)newCapacity;
        }

        private static void ThrowOverflow(int count) =>
            throw new OverflowException($"Exceeded maximum size for Source or Destinations. Required {count}.");

        public static class Factory
        {
            private static Operation Make(Instruction inst, int destCount, int srcCount)
            {
                Data* data = Allocators.Operations.Allocate<Data>();
                *data = default;

                Operation result = new()
                {
                    _data = data,
                    Instruction = inst,
                };

                EnsureCapacity(ref result._data->Destinations, ref result._data->DestinationsCount, destCount);
                EnsureCapacity(ref result._data->Sources, ref result._data->SourcesCount, srcCount);

                result.DestinationsUnsafe.Clear();
                result.SourcesUnsafe.Clear();

                return result;
            }

            public static Operation Operation(Instruction inst, Operand dest)
            {
                Operation result = Make(inst, 0, 0);
                result.SetDestination(dest);
                return result;
            }

            public static Operation Operation(Instruction inst, Operand dest, Operand src0)
            {
                Operation result = Make(inst, 0, 1);
                result.SetDestination(dest);
                result.SetSource(0, src0);
                return result;
            }

            public static Operation Operation(Instruction inst, Operand dest, Operand src0, Operand src1)
            {
                Operation result = Make(inst, 0, 2);
                result.SetDestination(dest);
                result.SetSource(0, src0);
                result.SetSource(1, src1);
                return result;
            }

            public static Operation Operation(Instruction inst, Operand dest, Operand src0, Operand src1, Operand src2)
            {
                Operation result = Make(inst, 0, 3);
                result.SetDestination(dest);
                result.SetSource(0, src0);
                result.SetSource(1, src1);
                result.SetSource(2, src2);
                return result;
            }

            public static Operation Operation(Instruction inst, Operand dest, int srcCount)
            {
                Operation result = Make(inst, 0, srcCount);
                result.SetDestination(dest);
                return result;
            }

            public static Operation Operation(Instruction inst, Operand dest, Operand[] srcs)
            {
                Operation result = Make(inst, 0, srcs.Length);

                result.SetDestination(dest);

                for (int index = 0; index < srcs.Length; index++)
                {
                    result.SetSource(index, srcs[index]);
                }

                return result;
            }

            public static Operation Operation(Intrinsic intrin, Operand dest, params ReadOnlySpan<Operand> srcs)
            {
                Operation result = Make(Instruction.Extended, 0, srcs.Length);

                result.Intrinsic = intrin;
                result.SetDestination(dest);

                for (int index = 0; index < srcs.Length; index++)
                {
                    result.SetSource(index, srcs[index]);
                }

                return result;
            }

            public static Operation Operation(Instruction inst, Operand[] dests, Operand[] srcs)
            {
                Operation result = Make(inst, dests.Length, srcs.Length);

                for (int index = 0; index < dests.Length; index++)
                {
                    result.SetDestination(index, dests[index]);
                }

                for (int index = 0; index < srcs.Length; index++)
                {
                    result.SetSource(index, srcs[index]);
                }

                return result;
            }

            public static Operation PhiOperation(Operand dest, int srcCount)
            {
                return Operation(Instruction.Phi, dest, srcCount * 2);
            }
        }
    }
}

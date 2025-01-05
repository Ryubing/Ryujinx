using System;

namespace Ryujinx.Graphics.Vic.Image
{
    ref struct RentedBuffer
    {
        public static RentedBuffer Empty => new([], -1);

        public Span<byte> Data;
        public int Index;

        public RentedBuffer(Span<byte> data, int index)
        {
            Data = data;
            Index = index;
        }

        public readonly void Return(BufferPool<byte> pool)
        {
            if (Index != -1)
            {
                pool.Return(Index);
            }
        }
    }

    ref struct InputSurface
    {
        public ReadOnlySpan<byte> Buffer0;
        public ReadOnlySpan<byte> Buffer1;
        public ReadOnlySpan<byte> Buffer2;

        public int Buffer0Index;
        public int Buffer1Index;
        public int Buffer2Index;

        public int Width;
        public int Height;

        public int UvWidth;
        public int UvHeight;

        public void Initialize()
        {
            Buffer0Index = -1;
            Buffer1Index = -1;
            Buffer2Index = -1;
        }

        public void SetBuffer0(RentedBuffer buffer)
        {
            Buffer0 = buffer.Data;
            Buffer0Index = buffer.Index;
        }

        public void SetBuffer1(RentedBuffer buffer)
        {
            Buffer1 = buffer.Data;
            Buffer1Index = buffer.Index;
        }

        public void SetBuffer2(RentedBuffer buffer)
        {
            Buffer2 = buffer.Data;
            Buffer2Index = buffer.Index;
        }

        public readonly void Return(BufferPool<byte> pool)
        {
            if (Buffer0Index != -1)
            {
                pool.Return(Buffer0Index);
            }

            if (Buffer1Index != -1)
            {
                pool.Return(Buffer1Index);
            }

            if (Buffer2Index != -1)
            {
                pool.Return(Buffer2Index);
            }
        }
    }
}

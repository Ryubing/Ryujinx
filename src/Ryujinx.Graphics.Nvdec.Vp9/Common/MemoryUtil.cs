using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Nvdec.Vp9.Common
{
    internal static class MemoryUtil
    {
        public static unsafe void Copy<T>(T* dest, T* source, int length) where T : unmanaged
        {
            new Span<T>(source, length).CopyTo(new Span<T>(dest, length));
        }

        public static void Copy<T>(ref T dest, ref T source) where T : unmanaged
        {
            MemoryMarshal.CreateSpan(ref source, 1).CopyTo(MemoryMarshal.CreateSpan(ref dest, 1));
        }

        public static unsafe void Fill<T>(T* ptr, T value, int length) where T : unmanaged
        {
            new Span<T>(ptr, length).Fill(value);
        }
    }
}
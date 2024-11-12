using System;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;

#pragma warning disable CS0169, IDE0051 // Remove unused private member
namespace Ryujinx.Common.Memory
{
    public struct Array1<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        public readonly int Length => 1;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array2<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array1<T> _other;
        public readonly int Length => 2;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array3<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array2<T> _other;
        public readonly int Length => 3;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array4<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array3<T> _other;
        public readonly int Length => 4;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array5<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array4<T> _other;
        public readonly int Length => 5;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array6<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array5<T> _other;
        public readonly int Length => 6;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array7<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array6<T> _other;
        public readonly int Length => 7;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array8<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array7<T> _other;
        public readonly int Length => 8;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array9<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array8<T> _other;
        public readonly int Length => 9;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array10<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array9<T> _other;
        public readonly int Length => 10;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array11<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array10<T> _other;
        public readonly int Length => 11;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array12<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array11<T> _other;
        public readonly int Length => 12;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array13<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array12<T> _other;
        public readonly int Length => 13;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array14<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array13<T> _other;
        public readonly int Length => 14;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array15<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array14<T> _other;
        public readonly int Length => 15;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array16<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array15<T> _other;
        public readonly int Length => 16;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array17<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array16<T> _other;
        public readonly int Length => 17;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array18<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array17<T> _other;
        public readonly int Length => 18;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array19<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array18<T> _other;
        public readonly int Length => 19;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array20<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array19<T> _other;
        public readonly int Length => 20;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array21<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array20<T> _other;
        public readonly int Length => 21;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array22<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array21<T> _other;
        public readonly int Length => 22;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array23<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array22<T> _other;
        public readonly int Length => 23;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array24<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array23<T> _other;

        public readonly int Length => 24;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array25<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array24<T> _other;

        public readonly int Length => 25;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array26<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array25<T> _other;

        public readonly int Length => 26;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array27<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array26<T> _other;

        public readonly int Length => 27;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array28<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array27<T> _other;

        public readonly int Length => 28;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array29<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array28<T> _other;

        public readonly int Length => 29;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array30<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array29<T> _other;

        public readonly int Length => 30;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array31<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array30<T> _other;

        public readonly int Length => 31;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array32<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array31<T> _other;

        public readonly int Length => 32;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array33<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array32<T> _other;

        public readonly int Length => 33;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array34<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array33<T> _other;

        public readonly int Length => 34;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array35<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array34<T> _other;

        public readonly int Length => 35;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array36<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array35<T> _other;

        public readonly int Length => 36;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array37<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array36<T> _other;

        public readonly int Length => 37;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array38<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array37<T> _other;

        public readonly int Length => 38;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array39<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array38<T> _other;

        public readonly int Length => 39;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array40<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array39<T> _other;

        public readonly int Length => 40;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array41<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array40<T> _other;

        public readonly int Length => 41;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array42<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array41<T> _other;

        public readonly int Length => 42;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array43<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array42<T> _other;

        public readonly int Length => 43;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array44<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array43<T> _other;

        public readonly int Length => 44;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array45<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array44<T> _other;

        public readonly int Length => 45;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array46<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array45<T> _other;

        public readonly int Length => 46;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array47<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array46<T> _other;

        public readonly int Length => 47;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array48<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array47<T> _other;

        public readonly int Length => 48;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array49<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array48<T> _other;

        public readonly int Length => 49;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array50<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array49<T> _other;

        public readonly int Length => 50;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array51<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array50<T> _other;

        public readonly int Length => 51;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array52<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array51<T> _other;

        public readonly int Length => 52;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array53<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array52<T> _other;

        public readonly int Length => 53;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array54<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array53<T> _other;

        public readonly int Length => 54;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array55<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array54<T> _other;

        public readonly int Length => 55;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array56<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array55<T> _other;

        public readonly int Length => 56;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array57<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array56<T> _other;

        public readonly int Length => 57;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array58<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array57<T> _other;

        public readonly int Length => 58;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array59<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array58<T> _other;

        public readonly int Length => 59;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array60<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array59<T> _other;
        public readonly int Length => 60;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array61<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array60<T> _other;
        public readonly int Length => 61;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array62<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array61<T> _other;
        public readonly int Length => 62;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array63<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array62<T> _other;
        public readonly int Length => 63;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array64<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array63<T> _other;
        public readonly int Length => 64;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array65<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array64<T> _other;
        public readonly int Length => 65;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array73<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array64<T> _other;
        Array8<T> _other2;
        public readonly int Length => 73;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array96<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array64<T> _other;
        Array31<T> _other2;
        public readonly int Length => 96;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array127<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array64<T> _other;
        Array62<T> _other2;
        public readonly int Length => 127;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array128<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array64<T> _other;
        Array63<T> _other2;
        public readonly int Length => 128;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array140<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array64<T> _other;
        Array64<T> _other2;
        Array11<T> _other3;
        public readonly int Length => 140;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array256<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array128<T> _other;
        Array127<T> _other2;
        public readonly int Length => 256;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }

    public struct Array384<T> : IArray<T> where T : unmanaged
    {
        T _e0;
        Array64<T> _other;
        Array64<T> _other2;
        Array64<T> _other3;
        Array64<T> _other4;
        Array64<T> _other5;
        Array63<T> _other6;
        public readonly int Length => 384;
        public ref T this[int index] => ref AsSpan()[index];

        [Pure]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _e0, Length);
    }
}
#pragma warning restore CS0169, IDE0051

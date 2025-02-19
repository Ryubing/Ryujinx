using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ARMeilleure.Translation.PTC
{
    static class PtcFormatter
    {
        #region "Deserialize"
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Dictionary<TKey, TValue> DeserializeDictionary<TKey, TValue>(Stream stream, Func<Stream, TValue> valueFunc) where TKey : struct
        {
            Dictionary<TKey, TValue> dictionary = new();

            int count = DeserializeStructure<int>(stream);

            for (int i = 0; i < count; i++)
            {
                TKey key = DeserializeStructure<TKey>(stream);
                TValue value = valueFunc(stream);

                dictionary.Add(key, value);
            }

            return dictionary;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Dictionary<TKey, TValue> DeserializeAndUpdateDictionary<TKey, TValue>(Stream stream, Func<Stream, TValue> valueFunc, Func<TKey, TValue, (TKey, TValue)> updateFunc) where TKey : struct
        {
            Dictionary<TKey, TValue> dictionary = new();

            int count = DeserializeStructure<int>(stream);

            for (int i = 0; i < count; i++)
            {
                TKey key = DeserializeStructure<TKey>(stream);
                TValue value = valueFunc(stream);

                (key, value) = updateFunc(key, value);

                dictionary.Add(key, value);
            }

            return dictionary;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T> DeserializeList<T>(Stream stream) where T : struct
        {
            List<T> list = [];

            int count = DeserializeStructure<int>(stream);

            for (int i = 0; i < count; i++)
            {
                T item = DeserializeStructure<T>(stream);

                list.Add(item);
            }

            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T DeserializeStructure<T>(Stream stream) where T : struct
        {
            T structure = default;

            Span<T> spanT = MemoryMarshal.CreateSpan(ref structure, 1);
            int bytesCount = stream.Read(MemoryMarshal.AsBytes(spanT));

            if (bytesCount != Unsafe.SizeOf<T>())
            {
                throw new EndOfStreamException();
            }

            return structure;
        }
        #endregion

        #region "GetSerializeSize"
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetSerializeSizeDictionary<TKey, TValue>(Dictionary<TKey, TValue> dictionary, Func<TValue, int> valueFunc) where TKey : struct
        {
            int size = 0;

            size += Unsafe.SizeOf<int>();

            foreach ((_, TValue value) in dictionary)
            {
                size += Unsafe.SizeOf<TKey>();
                size += valueFunc(value);
            }

            return size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetSerializeSizeList<T>(List<T> list) where T : struct
        {
            int size = 0;

            size += Unsafe.SizeOf<int>();

            size += list.Count * Unsafe.SizeOf<T>();

            return size;
        }
        #endregion

        #region "Serialize"
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SerializeDictionary<TKey, TValue>(Stream stream, Dictionary<TKey, TValue> dictionary, Action<Stream, TValue> valueAction) where TKey : struct
        {
            SerializeStructure<int>(stream, dictionary.Count);

            foreach ((TKey key, TValue value) in dictionary)
            {
                SerializeStructure<TKey>(stream, key);
                valueAction(stream, value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SerializeList<T>(Stream stream, List<T> list) where T : struct
        {
            SerializeStructure<int>(stream, list.Count);

            foreach (T item in list)
            {
                SerializeStructure<T>(stream, item);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SerializeStructure<T>(Stream stream, T structure) where T : struct
        {
            Span<T> spanT = MemoryMarshal.CreateSpan(ref structure, 1);
            stream.Write(MemoryMarshal.AsBytes(spanT));
        }
        #endregion

        #region "Extension methods"
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadFrom<T>(this List<T[]> list, Stream stream) where T : struct
        {
            int count = DeserializeStructure<int>(stream);

            for (int i = 0; i < count; i++)
            {
                int itemLength = DeserializeStructure<int>(stream);

                T[] item = new T[itemLength];

                int bytesCount = stream.Read(MemoryMarshal.AsBytes(item.AsSpan()));

                if (bytesCount != itemLength)
                {
                    throw new EndOfStreamException();
                }

                list.Add(item);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Length<T>(this List<T[]> list) where T : struct
        {
            long size = 0L;

            size += Unsafe.SizeOf<int>();

            foreach (T[] item in list)
            {
                size += Unsafe.SizeOf<int>();
                size += item.Length;
            }

            return size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteTo<T>(this List<T[]> list, Stream stream) where T : struct
        {
            SerializeStructure<int>(stream, list.Count);

            foreach (T[] item in list)
            {
                SerializeStructure<int>(stream, item.Length);

                stream.Write(MemoryMarshal.AsBytes(item.AsSpan()));
            }
        }
        #endregion
    }
}

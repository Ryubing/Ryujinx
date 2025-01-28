using System;
using System.Text;

namespace Ryujinx.Horizon.Sdk.Ngc.Detail
{
    readonly struct Utf8Text
    {
        private readonly byte[] _text;
        private readonly int[] _charOffsets;

        public int CharacterCount => _charOffsets.Length - 1;

        public Utf8Text()
        {
            _text = [];
            _charOffsets = [];
        }

        public Utf8Text(byte[] text)
        {
            _text = text;

            UTF8Encoding encoding = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

            string str = encoding.GetString(text);

            _charOffsets = new int[str.Length + 1];

            int offset = 0;

            for (int index = 0; index < str.Length; index++)
            {
                _charOffsets[index] = offset;
                offset += encoding.GetByteCount(str.AsSpan().Slice(index, 1));
            }

            _charOffsets[str.Length] = offset;
        }

        public Utf8Text(ReadOnlySpan<byte> text) : this(text.ToArray())
        {
        }

        public static Utf8ParseResult Create(out Utf8Text utf8Text, ReadOnlySpan<byte> text)
        {
            try
            {
                utf8Text = new(text);
            }
            catch (ArgumentException)
            {
                utf8Text = default;

                return Utf8ParseResult.InvalidCharacter;
            }

            return Utf8ParseResult.Success;
        }

        public ReadOnlySpan<byte> AsSubstring(int startCharIndex, int endCharIndex)
        {
            int startOffset = _charOffsets[startCharIndex];
            int endOffset = _charOffsets[endCharIndex];

            return _text.AsSpan()[startOffset..endOffset];
        }

        public Utf8Text AppendNullTerminated(ReadOnlySpan<byte> toAppend)
        {
            int length = toAppend.IndexOf((byte)0);
            if (length >= 0)
            {
                toAppend = toAppend[..length];
            }

            return Append(toAppend);
        }

        public Utf8Text Append(ReadOnlySpan<byte> toAppend)
        {
            byte[] combined = new byte[_text.Length + toAppend.Length];

            _text.AsSpan().CopyTo(combined.AsSpan()[.._text.Length]);
            toAppend.CopyTo(combined.AsSpan()[_text.Length..]);

            return new(combined);
        }

        public void CopyTo(Span<byte> destination)
        {
            _text.CopyTo(destination[.._text.Length]);

            if (destination.Length > _text.Length)
            {
                destination[_text.Length] = 0;
            }
        }

        public ReadOnlySpan<byte> AsSpan()
        {
            return _text;
        }
    }
}

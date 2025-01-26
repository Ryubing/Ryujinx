using Ryujinx.Horizon.Common;
using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace Ryujinx.Horizon.Sdk.Ngc.Detail
{
    abstract class ProfanityFilterBase
    {
#pragma warning disable IDE0230 // Use UTF-8 string literal
        private static readonly byte[][] _wordSeparators =
        [
            [0x0D],
            [0x0A],
            [0xC2, 0x85],
            [0xE2, 0x80, 0xA8],
            [0xE2, 0x80, 0xA9],
            [0x09],
            [0x0B],
            [0x0C],
            [0x20],
            [0xEF, 0xBD, 0xA1],
            [0xEF, 0xBD, 0xA4],
            [0x2E],
            [0x2C],
            [0x5B],
            [0x21],
            [0x22],
            [0x23],
            [0x24],
            [0x25],
            [0x26],
            [0x27],
            [0x28],
            [0x29],
            [0x2A],
            [0x2B],
            [0x2F],
            [0x3A],
            [0x3B],
            [0x3C],
            [0x3D],
            [0x3E],
            [0x3F],
            [0x5C],
            [0x40],
            [0x5E],
            [0x5F],
            [0x60],
            [0x7B],
            [0x7C],
            [0x7D],
            [0x7E],
            [0x2D],
            [0x5D],
            [0xE3, 0x80, 0x80],
            [0xE3, 0x80, 0x82],
            [0xE3, 0x80, 0x81],
            [0xEF, 0xBC, 0x8E],
            [0xEF, 0xBC, 0x8C],
            [0xEF, 0xBC, 0xBB],
            [0xEF, 0xBC, 0x81],
            [0xE2, 0x80, 0x9C],
            [0xE2, 0x80, 0x9D],
            [0xEF, 0xBC, 0x83],
            [0xEF, 0xBC, 0x84],
            [0xEF, 0xBC, 0x85],
            [0xEF, 0xBC, 0x86],
            [0xE2, 0x80, 0x98],
            [0xE2, 0x80, 0x99],
            [0xEF, 0xBC, 0x88],
            [0xEF, 0xBC, 0x89],
            [0xEF, 0xBC, 0x8A],
            [0xEF, 0xBC, 0x8B],
            [0xEF, 0xBC, 0x8F],
            [0xEF, 0xBC, 0x9A],
            [0xEF, 0xBC, 0x9B],
            [0xEF, 0xBC, 0x9C],
            [0xEF, 0xBC, 0x9D],
            [0xEF, 0xBC, 0x9E],
            [0xEF, 0xBC, 0x9F],
            [0xEF, 0xBC, 0xA0],
            [0xEF, 0xBF, 0xA5],
            [0xEF, 0xBC, 0xBE],
            [0xEF, 0xBC, 0xBF],
            [0xEF, 0xBD, 0x80],
            [0xEF, 0xBD, 0x9B],
            [0xEF, 0xBD, 0x9C],
            [0xEF, 0xBD, 0x9D],
            [0xEF, 0xBD, 0x9E],
            [0xEF, 0xBC, 0x8D],
            [0xEF, 0xBC, 0xBD]
        ];
#pragma warning restore IDE0230

        private enum SignFilterStep
        {
            DetectEmailStart,
            DetectEmailUserAtSign,
            DetectEmailDomain,
            DetectEmailEnd,
        }

        public abstract Result GetContentVersion(out uint version);
        public abstract Result CheckProfanityWords(out uint checkMask, ReadOnlySpan<byte> word, uint regionMask, ProfanityFilterOption option);
        public abstract Result MaskProfanityWordsInText(out int maskedWordsCount, Span<byte> text, uint regionMask, ProfanityFilterOption option);
        public abstract Result Reload();

        protected static bool IsIncludesAtSign(string word)
        {
            for (int index = 0; index < word.Length; index++)
            {
                if (word[index] == '\0')
                {
                    break;
                }
                else if (word[index] == '@' || word[index] == '\uFF20')
                {
                    return true;
                }
            }

            return false;
        }

        protected static int FilterAtSign(Span<byte> text, MaskMode maskMode)
        {
            SignFilterStep step = SignFilterStep.DetectEmailStart;
            int matchStart = 0;
            int matchCount = 0;

            for (int index = 0; index < text.Length; index++)
            {
                byte character = text[index];

                switch (step)
                {
                    case SignFilterStep.DetectEmailStart:
                        if (char.IsAsciiLetterOrDigit((char)character))
                        {
                            step = SignFilterStep.DetectEmailUserAtSign;
                            matchStart = index;
                        }
                        break;
                    case SignFilterStep.DetectEmailUserAtSign:
                        bool hasMatch = false;

                        while (IsValidEmailAddressCharacter(character))
                        {
                            hasMatch = true;

                            if (index + 1 >= text.Length)
                            {
                                break;
                            }

                            character = text[++index];
                        }

                        step = hasMatch && character == '@' ? SignFilterStep.DetectEmailDomain : SignFilterStep.DetectEmailStart;
                        break;
                    case SignFilterStep.DetectEmailDomain:
                        step = char.IsAsciiLetterOrDigit((char)character) ? SignFilterStep.DetectEmailEnd : SignFilterStep.DetectEmailStart;
                        break;
                    case SignFilterStep.DetectEmailEnd:
                        int domainIndex = index;

                        while (index + 1 < text.Length && IsValidEmailAddressCharacter(text[++index]))
                        {
                        }

                        int addressLastIndex = index - 1;
                        int lastIndex = 0;
                        bool lastIndexSet = false;

                        while (matchStart < addressLastIndex)
                        {
                            character = text[addressLastIndex];

                            if (char.IsAsciiLetterOrDigit((char)character))
                            {
                                if (!lastIndexSet)
                                {
                                    lastIndexSet = true;
                                    lastIndex = addressLastIndex;
                                }
                            }
                            else if (lastIndexSet)
                            {
                                break;
                            }

                            addressLastIndex--;
                        }

                        step = SignFilterStep.DetectEmailStart;

                        if (domainIndex < addressLastIndex && character == '.')
                        {
                            PreMaskCharacterRange(text, matchStart, lastIndex + 1, maskMode, (lastIndex - matchStart) + 1);
                            matchCount++;
                        }
                        else
                        {
                            index = domainIndex - 1;
                        }
                        break;
                }
            }

            return matchCount;
        }

        private static bool IsValidEmailAddressCharacter(byte character)
        {
            return char.IsAsciiLetterOrDigit((char)character) || character == '-' || character == '.' || character == '_';
        }

        protected static void PreMaskCharacterRange(Span<byte> text, int startOffset, int endOffset, MaskMode maskMode, int characterCount)
        {
            int byteLength = endOffset - startOffset;

            if (byteLength == 1)
            {
                text[startOffset] = 0xc1;
            }
            else if (byteLength == 2)
            {
                if (maskMode == MaskMode.Overwrite && Encoding.UTF8.GetCharCount(text.Slice(startOffset, 2)) != 1)
                {
                    text[startOffset] = 0xc1;
                    text[startOffset + 1] = 0xc1;
                }
                else if (maskMode == MaskMode.Overwrite || maskMode == MaskMode.ReplaceByOneCharacter)
                {
                    text[startOffset] = 0xc0;
                    text[startOffset + 1] = 0xc0;
                }
            }
            else
            {
                text[startOffset++] = 0;

                if (byteLength >= 0xff)
                {
                    int fillLength = (byteLength - 0xff) / 0xff + 1;

                    text.Slice(startOffset++, fillLength).Fill(0xff);

                    byteLength -= fillLength * 0xff;
                    startOffset += fillLength;
                }

                text[startOffset++] = (byte)byteLength;

                if (maskMode == MaskMode.ReplaceByOneCharacter)
                {
                    text[startOffset++] = 1;
                }
                else if (maskMode == MaskMode.Overwrite)
                {
                    if (characterCount >= 0xff)
                    {
                        int fillLength = (characterCount - 0xff) / 0xff + 1;

                        text.Slice(startOffset, fillLength).Fill(0xff);

                        characterCount -= fillLength * 0xff;
                        startOffset += fillLength;
                    }

                    text[startOffset++] = (byte)characterCount;
                }

                if (startOffset < endOffset)
                {
                    text[startOffset..endOffset].Fill(0xc1);
                }
            }
        }

        protected static void ConvertUserInputForWord(out string outputText, string inputText)
        {
            outputText = inputText.ToLowerInvariant();
        }

        protected static void ConvertUserInputForText(Span<byte> outputText, Span<sbyte> deltaTable, ReadOnlySpan<byte> inputText)
        {
            int outputIndex = 0;
            int deltaTableIndex = 0;

            for (int index = 0; index < inputText.Length;)
            {
                byte character = inputText[index];
                bool isInvalid = false;
                int characterByteLength = 1;

                if (character == 0xef && index + 4 < inputText.Length)
                {
                    if (((inputText[index + 1] == 0xbd && inputText[index + 2] >= 0xa6 && inputText[index + 2] < 0xe6) ||
                        (inputText[index + 1] == 0xbe && inputText[index + 2] >= 0x80 && inputText[index + 2] < 0xa0)) &&
                        inputText[index + 3] == 0xef &&
                        inputText[index + 4] == 0xbe)
                    {
                        characterByteLength = 6;
                    }
                    else
                    {
                        characterByteLength = 3;
                    }
                }
                else if ((character & 0x80) != 0)
                {
                    if (character >= 0xc2 && character < 0xe0)
                    {
                        characterByteLength = 2;
                    }
                    else if ((character & 0xf0) == 0xe0)
                    {
                        characterByteLength = 3;
                    }
                    else if ((character & 0xf8) == 0xf0)
                    {
                        characterByteLength = 4;
                    }
                    else
                    {
                        isInvalid = true;
                    }
                }

                isInvalid |= index + characterByteLength > inputText.Length;

                string str = null;

                if (!isInvalid)
                {
                    str = Encoding.UTF8.GetString(inputText.Slice(index, characterByteLength));

                    foreach (char chr in str)
                    {
                        if (chr == '\uFFFD')
                        {
                            isInvalid = true;
                            break;
                        }
                    }
                }

                int convertedByteLength = 1;

                if (isInvalid)
                {
                    characterByteLength = 1;
                    outputText[outputIndex++] = inputText[index];
                }
                else
                {
                    convertedByteLength = Encoding.UTF8.GetBytes(str.ToLowerInvariant().AsSpan(), outputText[outputIndex..]);
                    outputIndex += convertedByteLength;
                }

                if (deltaTable.Length != 0 && convertedByteLength != 0)
                {
                    // Calculate how many bytes we need to advance for each converted byte to match
                    // the character on the original text.
                    // The official service does this as part of the conversion (to lower case) process,
                    // but since we use .NET for that here, this is done separately.

                    int distribution = characterByteLength / convertedByteLength;

                    deltaTable[deltaTableIndex++] = (sbyte)(characterByteLength - distribution * convertedByteLength + distribution);

                    for (int byteIndex = 1; byteIndex < convertedByteLength; byteIndex++)
                    {
                        deltaTable[deltaTableIndex++] = (sbyte)distribution;
                    }
                }

                index += characterByteLength;
            }

            if (outputIndex < outputText.Length)
            {
                outputText[outputIndex] = 0;
            }
        }

        protected static Span<byte> MaskText(Span<byte> text)
        {
            if (text.Length == 0)
            {
                return text;
            }

            for (int index = 0; index < text.Length; index++)
            {
                byte character = text[index];

                if (character == 0xc1)
                {
                    text[index] = (byte)'*';
                }
                else if (character == 0xc0)
                {
                    if (index + 1 < text.Length && text[index + 1] == 0xc0)
                    {
                        text[index++] = (byte)'*';
                        text[index] = 0;
                    }
                }
                else if (character == 0 && index + 1 < text.Length)
                {
                    // There are two sequences of 0xFF followed by another value.
                    // The first indicates the length of the sub-string to replace in bytes.
                    // The second indicates the character count.

                    int lengthSequenceIndex = index + 1;
                    int byteLength = CountMaskLengthBytes(text, ref lengthSequenceIndex);
                    int characterCount = CountMaskLengthBytes(text, ref lengthSequenceIndex);

                    if (byteLength != 0)
                    {
                        for (int replaceIndex = 0; replaceIndex < byteLength; replaceIndex++)
                        {
                            text[index++] = (byte)(replaceIndex < characterCount ? '*' : '\0');
                        }

                        index--;
                    }
                }
            }

            // Move null-terminators to the end.
            MoveZeroValuesToEnd(text);

            // Find new length of the text.
            int length = text.IndexOf((byte)0);

            if (length >= 0)
            {
                return text[..length];
            }

            return text;
        }

        protected static void UpdateDeltaTable(Span<sbyte> deltaTable, ReadOnlySpan<byte> text)
        {
            if (text.Length == 0)
            {
                return;
            }

            // Update values to account for the characters that will be removed.
            for (int index = 0; index < text.Length; index++)
            {
                byte character = text[index];

                if (character == 0 && index + 1 < text.Length)
                {
                    // There are two sequences of 0xFF followed by another value.
                    // The first indicates the length of the sub-string to replace in bytes.
                    // The second indicates the character count.

                    int lengthSequenceIndex = index + 1;
                    int byteLength = CountMaskLengthBytes(text, ref lengthSequenceIndex);
                    int characterCount = CountMaskLengthBytes(text, ref lengthSequenceIndex);

                    if (byteLength != 0)
                    {
                        for (int replaceIndex = 0; replaceIndex < byteLength; replaceIndex++)
                        {
                            deltaTable[index++] = (sbyte)(replaceIndex < characterCount ? 1 : 0);
                        }
                    }
                }
            }

            // Move zero values of the removed bytes to the end.
            MoveZeroValuesToEnd(MemoryMarshal.Cast<sbyte, byte>(deltaTable));
        }

        private static int CountMaskLengthBytes(ReadOnlySpan<byte> text, ref int index)
        {
            int totalLength = 0;

            for (; index < text.Length; index++)
            {
                int length = text[index];
                totalLength += length;

                if (length != 0xff)
                {
                    index++;
                    break;
                }
            }

            return totalLength;
        }

        private static void MoveZeroValuesToEnd(Span<byte> text)
        {
            for (int index = 0; index < text.Length; index++)
            {
                int nullCount = 0;

                for (; index + nullCount < text.Length; nullCount++)
                {
                    byte character = text[index + nullCount];
                    if (character != 0)
                    {
                        break;
                    }
                }

                if (nullCount != 0)
                {
                    int fillLength = text.Length - (index + nullCount);

                    text[(index + nullCount)..].CopyTo(text.Slice(index, fillLength));
                    text.Slice(index + fillLength, nullCount).Clear();
                }
            }
        }

        protected static Span<byte> RemoveWordSeparators(Span<byte> output, ReadOnlySpan<byte> input, Sbv map)
        {
            int outputIndex = 0;

            if (map.Set.BitVector.BitLength != 0)
            {
                for (int index = 0; index < input.Length; index++)
                {
                    bool isWordSeparator = false;

                    for (int separatorIndex = 0; separatorIndex < _wordSeparators.Length; separatorIndex++)
                    {
                        ReadOnlySpan<byte> separator = _wordSeparators[separatorIndex];

                        if (index + separator.Length < input.Length && input.Slice(index, separator.Length).SequenceEqual(separator))
                        {
                            map.Set.TurnOn(index, separator.Length);

                            index += separator.Length - 1;
                            isWordSeparator = true;
                            break;
                        }
                    }

                    if (!isWordSeparator)
                    {
                        output[outputIndex++] = input[index];
                    }
                }
            }

            map.Build();

            return output[..outputIndex];
        }

        protected static int TrimEnd(ReadOnlySpan<byte> text, int offset)
        {
            for (int separatorIndex = 0; separatorIndex < _wordSeparators.Length; separatorIndex++)
            {
                ReadOnlySpan<byte> separator = _wordSeparators[separatorIndex];

                if (offset >= separator.Length && text.Slice(offset - separator.Length, separator.Length).SequenceEqual(separator))
                {
                    offset -= separator.Length;
                    separatorIndex = -1;
                }
            }

            return offset;
        }

        protected static bool IsPrefixedByWordSeparator(ReadOnlySpan<byte> text, int offset)
        {
            for (int separatorIndex = 0; separatorIndex < _wordSeparators.Length; separatorIndex++)
            {
                ReadOnlySpan<byte> separator = _wordSeparators[separatorIndex];

                if (offset >= separator.Length && text.Slice(offset - separator.Length, separator.Length).SequenceEqual(separator))
                {
                    return true;
                }
            }

            return false;
        }

        protected static bool IsWordSeparator(ReadOnlySpan<byte> text, int offset)
        {
            for (int separatorIndex = 0; separatorIndex < _wordSeparators.Length; separatorIndex++)
            {
                ReadOnlySpan<byte> separator = _wordSeparators[separatorIndex];

                if (offset + separator.Length <= text.Length && text.Slice(offset, separator.Length).SequenceEqual(separator))
                {
                    return true;
                }
            }

            return false;
        }

        protected static Span<byte> RemoveWordSeparators(Span<byte> output, ReadOnlySpan<byte> input, Sbv map, AhoCorasick notSeparatorTrie)
        {
            int outputIndex = 0;

            if (map.Set.BitVector.BitLength != 0)
            {
                for (int index = 0; index < input.Length;)
                {
                    byte character = input[index];
                    int characterByteLength = 1;

                    if ((character & 0x80) != 0)
                    {
                        if (character >= 0xc2 && character < 0xe0)
                        {
                            characterByteLength = 2;
                        }
                        else if ((character & 0xf0) == 0xe0)
                        {
                            characterByteLength = 3;
                        }
                        else if ((character & 0xf8) == 0xf0)
                        {
                            characterByteLength = 4;
                        }
                    }

                    characterByteLength = Math.Min(characterByteLength, input.Length - index);

                    bool isWordSeparator = IsWordSeparator(input.Slice(index, characterByteLength), notSeparatorTrie);
                    if (isWordSeparator)
                    {
                        map.Set.TurnOn(index, characterByteLength);
                    }
                    else
                    {
                        output[outputIndex++] = input[index];
                    }

                    index += characterByteLength;
                }
            }

            map.Build();

            return output[..outputIndex];
        }

        protected static bool IsWordSeparator(ReadOnlySpan<byte> text, AhoCorasick notSeparatorTrie)
        {
            string str = Encoding.UTF8.GetString(text);

            if (str.Length == 0)
            {
                return false;
            }

            char character = str[0];

            switch (character)
            {
                case '\0':
                case '\uD800':
                case '\uDB7F':
                case '\uDB80':
                case '\uDBFF':
                case '\uDC00':
                case '\uDFFF':
                    return false;
                case '\u02E4':
                case '\u02EC':
                case '\u02EE':
                case '\u0374':
                case '\u037A':
                case '\u0559':
                case '\u0640':
                case '\u06E5':
                case '\u06E6':
                case '\u07F4':
                case '\u07F5':
                case '\u07FA':
                case '\u1C78':
                case '\u1C79':
                case '\u1C7A':
                case '\u1C7B':
                case '\u1C7C':
                case '\uA4F8':
                case '\uA4F9':
                case '\uA4FA':
                case '\uA4FB':
                case '\uA4FC':
                case '\uA4FD':
                case '\uFF70':
                case '\uFF9A':
                case '\uFF9B':
                    return true;
            }

            bool matched = false;

            notSeparatorTrie.Match(text, MatchSimple, ref matched);

            if (!matched)
            {
                switch (char.GetUnicodeCategory(character))
                {
                    case UnicodeCategory.NonSpacingMark:
                    case UnicodeCategory.SpacingCombiningMark:
                    case UnicodeCategory.EnclosingMark:
                    case UnicodeCategory.SpaceSeparator:
                    case UnicodeCategory.LineSeparator:
                    case UnicodeCategory.ParagraphSeparator:
                    case UnicodeCategory.Control:
                    case UnicodeCategory.Format:
                    case UnicodeCategory.Surrogate:
                    case UnicodeCategory.PrivateUse:
                    case UnicodeCategory.ConnectorPunctuation:
                    case UnicodeCategory.DashPunctuation:
                    case UnicodeCategory.OpenPunctuation:
                    case UnicodeCategory.ClosePunctuation:
                    case UnicodeCategory.InitialQuotePunctuation:
                    case UnicodeCategory.FinalQuotePunctuation:
                    case UnicodeCategory.OtherPunctuation:
                    case UnicodeCategory.MathSymbol:
                    case UnicodeCategory.CurrencySymbol:
                        return true;
                }
            }

            return false;
        }

        protected static int GetUtf8Length(out int characterCount, ReadOnlySpan<byte> text, int maxCharacters)
        {
            int index;

            for (index = 0, characterCount = 0; index < text.Length && characterCount < maxCharacters; characterCount++)
            {
                byte character = text[index];
                int characterByteLength;

                if ((character & 0x80) != 0 || character == 0)
                {
                    if (character >= 0xc2 && character < 0xe0)
                    {
                        characterByteLength = 2;
                    }
                    else if ((character & 0xf0) == 0xe0)
                    {
                        characterByteLength = 3;
                    }
                    else if ((character & 0xf8) == 0xf0)
                    {
                        characterByteLength = 4;
                    }
                    else
                    {
                        index = 0;
                        break;
                    }
                }
                else
                {
                    characterByteLength = 1;
                }

                index += characterByteLength;
            }

            return index;
        }

        protected static bool MatchSimple(ReadOnlySpan<byte> text, int matchStartOffset, int matchEndOffset, int nodeId, ref bool matched)
        {
            matched = true;

            return false;
        }
    }
}

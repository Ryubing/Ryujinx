using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Fs;
using System;
using System.Buffers.Binary;
using System.Numerics;
using System.Text;

namespace Ryujinx.Horizon.Sdk.Ngc.Detail
{
    class ProfanityFilter : ProfanityFilterBase, IDisposable
    {
        private const int MaxBufferLength = 0x800;
        private const int MaxUtf8CharacterLength = 4;
        private const int MaxUtf8Characters = MaxBufferLength / MaxUtf8CharacterLength;
        private const int RegionsCount = 16;
        private const int MountCacheSize = 0x2000;

        private readonly ContentsReader _contentsReader;

        public ProfanityFilter(IFsClient fsClient)
        {
            _contentsReader = new(fsClient);
        }

        public Result Initialize()
        {
            return _contentsReader.Initialize(MountCacheSize);
        }

        public override Result Reload()
        {
            return _contentsReader.Reload();
        }

        public override Result GetContentVersion(out uint version)
        {
            version = 0;

            Result result = _contentsReader.GetVersionDataSize(out long size);
            if (result.IsFailure && size != 4)
            {
                return Result.Success;
            }

            Span<byte> data = stackalloc byte[4];
            result = _contentsReader.GetVersionData(data);
            if (result.IsFailure)
            {
                return Result.Success;
            }

            version = BinaryPrimitives.ReadUInt32BigEndian(data);

            return Result.Success;
        }

        public override Result CheckProfanityWords(out uint checkMask, ReadOnlySpan<byte> word, uint regionMask, ProfanityFilterOption option)
        {
            checkMask = 0;

            int length = word.IndexOf((byte)0);
            if (length >= 0)
            {
                word = word[..length];
            }

            UTF8Encoding encoding = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

            string decodedWord;

            try
            {
                decodedWord = encoding.GetString(word);
            }
            catch (ArgumentException)
            {
                return NgcResult.InvalidUtf8Encoding;
            }

            return CheckProfanityWordsMultiRegionImpl(ref checkMask, decodedWord, regionMask, option);
        }

        private Result CheckProfanityWordsMultiRegionImpl(ref uint checkMask, string word, uint regionMask, ProfanityFilterOption option)
        {
            // Check using common dictionary.
            Result result = CheckProfanityWordsImpl(ref checkMask, word, 0, option);
            if (result.IsFailure)
            {
                return result;
            }

            if (checkMask != 0)
            {
                checkMask = (ushort)(regionMask | option.SystemRegionMask);
            }

            // Check using region specific dictionaries if needed.
            for (int regionIndex = 0; regionIndex < RegionsCount; regionIndex++)
            {
                if (((regionMask | option.SystemRegionMask) & (1 << regionIndex)) != 0)
                {
                    result = CheckProfanityWordsImpl(ref checkMask, word, 1u << regionIndex, option);
                    if (result.IsFailure)
                    {
                        return result;
                    }
                }
            }

            return Result.Success;
        }

        private Result CheckProfanityWordsImpl(ref uint checkMask, string word, uint regionMask, ProfanityFilterOption option)
        {
            ConvertUserInputForWord(out string convertedWord, word);

            if (IsIncludesAtSign(convertedWord))
            {
                checkMask |= regionMask != 0 ? regionMask : option.SystemRegionMask;
            }

            byte[] utf8Text = Encoding.UTF8.GetBytes(convertedWord);
            byte[] convertedText = new byte[utf8Text.Length + 5];

            utf8Text.CopyTo(convertedText.AsSpan().Slice(2, utf8Text.Length));

            convertedText[0] = (byte)'\\';
            convertedText[1] = (byte)'b';
            convertedText[2 + utf8Text.Length] = (byte)'\\';
            convertedText[3 + utf8Text.Length] = (byte)'b';
            convertedText[4 + utf8Text.Length] = 0;

            int regionIndex = (ushort)regionMask != 0 ? BitOperations.TrailingZeroCount(regionMask) : -1;

            Result result = _contentsReader.ReadDictionaries(out AhoCorasick partialWordsTrie, out _, out AhoCorasick delimitedWordsTrie, regionIndex);
            if (result.IsFailure)
            {
                return result;
            }

            if ((checkMask & regionMask) == 0)
            {
                MatchCheckState state = new(checkMask, regionMask, option);

                partialWordsTrie.Match(convertedText, MatchCheck, ref state);
                delimitedWordsTrie.Match(convertedText, MatchCheck, ref state);

                checkMask = state.CheckMask;
            }

            return Result.Success;
        }

        public override Result MaskProfanityWordsInText(out int maskedWordsCount, Span<byte> text, uint regionMask, ProfanityFilterOption option)
        {
            maskedWordsCount = 0;

            Span<byte> output = text;
            Span<byte> convertedText = new byte[MaxBufferLength];
            Span<sbyte> deltaTable = new sbyte[MaxBufferLength];

            int nullTerminatorIndex = GetUtf8Length(out _, text, MaxUtf8Characters);

            // Ensure that the text has a null terminator if we can.
            // If the text is too long, it will be truncated.
            byte replacedCharacter = 0;

            if (nullTerminatorIndex > 0 && nullTerminatorIndex < text.Length)
            {
                replacedCharacter = text[nullTerminatorIndex];
                text[nullTerminatorIndex] = 0;
            }

            // Truncate the text if needed.
            int length = text.IndexOf((byte)0);
            if (length >= 0)
            {
                text = text[..length];
            }

            // If requested, mask e-mail addresses.
            if (option.SkipAtSignCheck == SkipMode.DoNotSkip)
            {
                maskedWordsCount += FilterAtSign(text, option.MaskMode);
                text = MaskText(text);
            }

            // Convert the text to lower case, required for string matching.
            ConvertUserInputForText(convertedText, deltaTable, text);

            // Mask words for common and requested regions.
            Result result = MaskProfanityWordsInTextMultiRegion(ref maskedWordsCount, ref text, ref convertedText, deltaTable, regionMask, option);
            if (result.IsFailure)
            {
                return result;
            }

            // If requested, also try to match and mask the canonicalized string.
            if (option.Flags != ProfanityFilterFlags.None)
            {
                result = MaskProfanityWordsInTextCanonicalizedMultiRegion(ref maskedWordsCount, text, regionMask, option);
                if (result.IsFailure)
                {
                    return result;
                }
            }

            // If we received more text than we can process, copy unprocessed portion to the end of the new text.
            if (replacedCharacter != 0)
            {
                length = text.IndexOf((byte)0);

                if (length < 0)
                {
                    length = text.Length;
                }

                output[length++] = replacedCharacter;
                int unprocessedLength = output.Length - nullTerminatorIndex - 1;
                output.Slice(nullTerminatorIndex + 1, unprocessedLength).CopyTo(output.Slice(length, unprocessedLength));
            }

            return Result.Success;
        }

        private Result MaskProfanityWordsInTextMultiRegion(
            ref int maskedWordsCount,
            ref Span<byte> originalText,
            ref Span<byte> convertedText,
            Span<sbyte> deltaTable,
            uint regionMask,
            ProfanityFilterOption option)
        {
            // Filter using common dictionary.
            Result result = MaskProfanityWordsInTextImpl(ref maskedWordsCount, ref originalText, ref convertedText, deltaTable, -1, option);
            if (result.IsFailure)
            {
                return result;
            }

            // Filter using region specific dictionaries if needed.
            for (int regionIndex = 0; regionIndex < RegionsCount; regionIndex++)
            {
                if (((regionMask | option.SystemRegionMask) & (1 << regionIndex)) != 0)
                {
                    result = MaskProfanityWordsInTextImpl(ref maskedWordsCount, ref originalText, ref convertedText, deltaTable, regionIndex, option);
                    if (result.IsFailure)
                    {
                        return result;
                    }
                }
            }

            return Result.Success;
        }

        private Result MaskProfanityWordsInTextImpl(
            ref int maskedWordsCount,
            ref Span<byte> originalText,
            ref Span<byte> convertedText,
            Span<sbyte> deltaTable,
            int regionIndex,
            ProfanityFilterOption option)
        {
            Result result = _contentsReader.ReadDictionaries(
                out AhoCorasick partialWordsTrie,
                out AhoCorasick completeWordsTrie,
                out AhoCorasick delimitedWordsTrie,
                regionIndex);

            if (result.IsFailure)
            {
                return result;
            }

            // Match single words.

            MatchState state = new(originalText, convertedText, deltaTable, ref maskedWordsCount, option.MaskMode);

            partialWordsTrie.Match(convertedText, MatchSingleWord, ref state);

            MaskText(ref originalText, ref convertedText, deltaTable);

            // Match single words and phrases.
            // We remove word separators on the string used for the match.

            Span<byte> noSeparatorText = new byte[originalText.Length];
            Sbv noSeparatorMap = new(convertedText.Length);
            noSeparatorText = RemoveWordSeparators(noSeparatorText, convertedText, noSeparatorMap);

            state = new(
                originalText,
                convertedText,
                deltaTable,
                ref maskedWordsCount,
                option.MaskMode,
                noSeparatorMap,
                delimitedWordsTrie);

            partialWordsTrie.Match(noSeparatorText, MatchMultiWord, ref state);

            MaskText(ref originalText, ref convertedText, deltaTable);

            // Match whole words, which must be surrounded by word separators.

            noSeparatorText = new byte[originalText.Length];
            noSeparatorMap = new(convertedText.Length);
            noSeparatorText = RemoveWordSeparators(noSeparatorText, convertedText, noSeparatorMap);

            state = new(
                originalText,
                convertedText,
                deltaTable,
                ref maskedWordsCount,
                option.MaskMode,
                noSeparatorMap,
                delimitedWordsTrie);

            completeWordsTrie.Match(noSeparatorText, MatchDelimitedWord, ref state);

            MaskText(ref originalText, ref convertedText, deltaTable);

            return Result.Success;
        }

        private static void MaskText(ref Span<byte> originalText, ref Span<byte> convertedText, Span<sbyte> deltaTable)
        {
            originalText = MaskText(originalText);
            UpdateDeltaTable(deltaTable, convertedText);
            convertedText = MaskText(convertedText);
        }

        private Result MaskProfanityWordsInTextCanonicalizedMultiRegion(ref int maskedWordsCount, Span<byte> text, uint regionMask, ProfanityFilterOption option)
        {
            // Filter using common dictionary.
            Result result = MaskProfanityWordsInTextCanonicalized(ref maskedWordsCount, text, 0, option);
            if (result.IsFailure)
            {
                return result;
            }

            // Filter using region specific dictionaries if needed.
            for (int index = 0; index < RegionsCount; index++)
            {
                if ((((regionMask | option.SystemRegionMask) >> index) & 1) != 0)
                {
                    result = MaskProfanityWordsInTextCanonicalized(ref maskedWordsCount, text, 1u << index, option);
                    if (result.IsFailure)
                    {
                        return result;
                    }
                }
            }

            return Result.Success;
        }

        private Result MaskProfanityWordsInTextCanonicalized(ref int maskedWordsCount, Span<byte> text, uint regionMask, ProfanityFilterOption option)
        {
            Utf8Text maskedText = new();
            Utf8ParseResult parseResult = Utf8Text.Create(out Utf8Text inputText, text);
            if (parseResult != Utf8ParseResult.Success)
            {
                return NgcResult.InvalidUtf8Encoding;
            }

            ReadOnlySpan<byte> prevCharacter = [];

            int charStartIndex = 0;

            for (int charEndIndex = 1; charStartIndex < inputText.CharacterCount;)
            {
                ReadOnlySpan<byte> nextCharacter = charEndIndex < inputText.CharacterCount
                    ? inputText.AsSubstring(charEndIndex, charEndIndex + 1)
                    : [];

                Result result = CheckProfanityWordsInTextCanonicalized(
                    out bool matched,
                    inputText.AsSubstring(charStartIndex, charEndIndex),
                    prevCharacter,
                    nextCharacter,
                    regionMask,
                    option);

                if (result.IsFailure && result != NgcResult.InvalidSize)
                {
                    return result;
                }

                if (matched)
                {
                    // We had a match, we know where it ends, now we need to find where it starts.

                    int previousCharStartIndex = charStartIndex;

                    for (; charStartIndex < charEndIndex; charStartIndex++)
                    {
                        result = CheckProfanityWordsInTextCanonicalized(
                            out matched,
                            inputText.AsSubstring(charStartIndex, charEndIndex),
                            prevCharacter,
                            nextCharacter,
                            regionMask,
                            option);

                        if (result.IsFailure && result != NgcResult.InvalidSize)
                        {
                            return result;
                        }

                        // When we get past the start of the matched substring, the match will fail,
                        // so that's when we know we found the start.
                        if (!matched)
                        {
                            break;
                        }
                    }

                    // Append substring before the match start.
                    maskedText = maskedText.Append(inputText.AsSubstring(previousCharStartIndex, charStartIndex - 1));

                    // Mask matched substring with asterisks.
                    if (option.MaskMode == MaskMode.ReplaceByOneCharacter)
                    {
                        maskedText = maskedText.Append("*"u8);
                        prevCharacter = "*"u8;
                    }
                    else if (option.MaskMode == MaskMode.Overwrite && charStartIndex <= charEndIndex)
                    {
                        int maskLength = charEndIndex - charStartIndex + 1;

                        while (maskLength-- > 0)
                        {
                            maskedText = maskedText.Append("*"u8);
                        }

                        prevCharacter = "*"u8;
                    }

                    charStartIndex = charEndIndex;
                    maskedWordsCount++;
                }

                if (charEndIndex < inputText.CharacterCount)
                {
                    charEndIndex++;
                }
                else if (charStartIndex < inputText.CharacterCount)
                {
                    prevCharacter = inputText.AsSubstring(charStartIndex, charStartIndex + 1);
                    maskedText = maskedText.Append(prevCharacter);
                    charStartIndex++;
                }
            }

            // Replace text with the masked text.
            maskedText.CopyTo(text);

            return Result.Success;
        }

        private Result CheckProfanityWordsInTextCanonicalized(
            out bool matched,
            ReadOnlySpan<byte> text,
            ReadOnlySpan<byte> prevCharacter,
            ReadOnlySpan<byte> nextCharacter,
            uint regionMask,
            ProfanityFilterOption option)
        {
            matched = false;

            Span<byte> convertedText = new byte[MaxBufferLength + 1];
            text.CopyTo(convertedText[..text.Length]);

            Result result;

            if (text.Length > 0)
            {
                // If requested, normalize.
                // This will convert different encodings for the same character in their canonical encodings.
                if (option.Flags.HasFlag(ProfanityFilterFlags.MatchNormalizedFormKC))
                {
                    Utf8ParseResult parseResult = Utf8Util.NormalizeFormKC(convertedText, convertedText);

                    if (parseResult != Utf8ParseResult.Success)
                    {
                        return NgcResult.InvalidUtf8Encoding;
                    }
                }

                // Convert to lower case.
                ConvertUserInputForText(convertedText, [], convertedText);

                // If requested, also try to replace similar characters with their canonical form.
                // For example, vv is similar to w, and 1 or | is similar to i.
                if (option.Flags.HasFlag(ProfanityFilterFlags.MatchSimilarForm))
                {
                    result = ConvertInputTextFromSimilarForm(convertedText, convertedText);
                    if (result.IsFailure)
                    {
                        return result;
                    }
                }

                int length = convertedText.IndexOf((byte)0);
                if (length >= 0)
                {
                    convertedText = convertedText[..length];
                }
            }

            int regionIndex = (ushort)regionMask != 0 ? BitOperations.TrailingZeroCount(regionMask) : -1;

            result = _contentsReader.ReadDictionaries(
                out AhoCorasick partialWordsTrie,
                out AhoCorasick completeWordsTrie,
                out AhoCorasick delimitedWordsTrie,
                regionIndex);

            if (result.IsFailure)
            {
                return result;
            }

            result = ContentsReader.ReadNotSeparatorDictionary(out AhoCorasick notSeparatorTrie);
            if (result.IsFailure)
            {
                return result;
            }

            // Match single words.

            bool trieMatched = false;

            partialWordsTrie.Match(convertedText, MatchSimple, ref trieMatched);

            if (trieMatched)
            {
                matched = true;

                return Result.Success;
            }

            // Match single words and phrases.
            // We remove word separators on the string used for the match.

            Span<byte> noSeparatorText = new byte[text.Length];
            Sbv noSeparatorMap = new(convertedText.Length);
            noSeparatorText = RemoveWordSeparators(noSeparatorText, convertedText, noSeparatorMap, notSeparatorTrie);

            trieMatched = false;

            partialWordsTrie.Match(noSeparatorText, MatchSimple, ref trieMatched);

            if (trieMatched)
            {
                matched = true;

                return Result.Success;
            }

            // Match whole words, which must be surrounded by word separators.

            bool prevCharIsWordSeparator = prevCharacter.Length == 0 || IsWordSeparator(prevCharacter, notSeparatorTrie);
            bool nextCharIsWordSeparator = nextCharacter.Length == 0 || IsWordSeparator(nextCharacter, notSeparatorTrie);

            MatchDelimitedState state = new(prevCharIsWordSeparator, nextCharIsWordSeparator, noSeparatorMap, delimitedWordsTrie);

            completeWordsTrie.Match(noSeparatorText, MatchDelimitedWordSimple, ref state);

            if (state.Matched)
            {
                matched = true;
            }

            return Result.Success;
        }

        private Result ConvertInputTextFromSimilarForm(Span<byte> convertedText, ReadOnlySpan<byte> text)
        {
            int length = text.IndexOf((byte)0);
            if (length >= 0)
            {
                text = text[..length];
            }

            Result result = _contentsReader.ReadSimilarFormDictionary(out AhoCorasick similarFormTrie);
            if (result.IsFailure)
            {
                return result;
            }

            result = _contentsReader.ReadSimilarFormTable(out SimilarFormTable similarFormTable);
            if (result.IsFailure)
            {
                return result;
            }

            // Find all characters that have a similar form.
            MatchRangeListState listState = new();

            similarFormTrie.Match(text, MatchRangeListState.AddMatch, ref listState);

            // Filter found match ranges.
            // Because some similar form strings are a subset of others, we need to remove overlapping matches.
            // For example, | can be replaced with i, but |-| can be replaced with h.
            // We prefer the latter match (|-|) because it is more specific.
            MatchRangeList deduplicatedMatches = listState.MatchRanges.Deduplicate();

            MatchSimilarFormState state = new(deduplicatedMatches, similarFormTable);

            similarFormTrie.Match(text, MatchAndReplace, ref state);

            // Append remaining characters.
            state.CanonicalText = state.CanonicalText.Append(text[state.ReplaceEndOffset..]);

            // Set canonical text to output.
            ReadOnlySpan<byte> canonicalText = state.CanonicalText.AsSpan();
            canonicalText.CopyTo(convertedText[..canonicalText.Length]);
            convertedText[canonicalText.Length] = 0;

            return Result.Success;
        }

        private static bool MatchCheck(ReadOnlySpan<byte> text, int matchStartOffset, int matchEndOffset, int nodeId, ref MatchCheckState state)
        {
            state.CheckMask |= state.RegionMask != 0 ? state.RegionMask : state.Option.SystemRegionMask;

            return true;
        }

        private static bool MatchSingleWord(ReadOnlySpan<byte> text, int matchStartOffset, int matchEndOffset, int nodeId, ref MatchState state)
        {
            MatchCommon(ref state, matchStartOffset, matchEndOffset);

            return true;
        }

        private static bool MatchMultiWord(ReadOnlySpan<byte> text, int matchStartOffset, int matchEndOffset, int nodeId, ref MatchState state)
        {
            int convertedStartOffset = state.NoSeparatorMap.Set.Select0(matchStartOffset);
            int convertedEndOffset = state.NoSeparatorMap.Set.Select0(matchEndOffset);

            if (convertedEndOffset < 0)
            {
                convertedEndOffset = state.NoSeparatorMap.Set.BitVector.BitLength;
            }

            int endOffsetBeforeSeparator = TrimEnd(state.ConvertedText, convertedEndOffset);

            MatchCommon(ref state, convertedStartOffset, endOffsetBeforeSeparator);

            return true;
        }

        private static bool MatchDelimitedWord(ReadOnlySpan<byte> text, int matchStartOffset, int matchEndOffset, int nodeId, ref MatchState state)
        {
            int convertedStartOffset = state.NoSeparatorMap.Set.Select0(matchStartOffset);
            int convertedEndOffset = state.NoSeparatorMap.Set.Select0(matchEndOffset);

            if (convertedEndOffset < 0)
            {
                convertedEndOffset = state.NoSeparatorMap.Set.BitVector.BitLength;
            }

            int endOffsetBeforeSeparator = TrimEnd(state.ConvertedText, convertedEndOffset);

            Span<byte> delimitedText = new byte[64];

            // If the word is prefixed by a word separator, insert "\b" delimiter, otherwise insert "a" delimitar.
            // The start of the string is also considered a "word separator".

            bool startIsPrefixedByWordSeparator =
                convertedStartOffset == 0 ||
                IsPrefixedByWordSeparator(state.ConvertedText, convertedStartOffset);

            int delimitedTextOffset = 0;

            if (startIsPrefixedByWordSeparator)
            {
                delimitedText[delimitedTextOffset++] = (byte)'\\';
                delimitedText[delimitedTextOffset++] = (byte)'b';
            }
            else
            {
                delimitedText[delimitedTextOffset++] = (byte)'a';
            }

            // Copy the word to our temporary buffer used for the next match.

            int matchLength = matchEndOffset - matchStartOffset;

            text.Slice(matchStartOffset, matchLength).CopyTo(delimitedText.Slice(delimitedTextOffset, matchLength));

            delimitedTextOffset += matchLength;

            // If the word is suffixed by a word separator, insert "\b" delimiter, otherwise insert "a" delimiter.
            // The end of the string is also considered a "word separator".

            bool endIsSuffixedByWordSeparator =
                endOffsetBeforeSeparator == state.NoSeparatorMap.Set.BitVector.BitLength ||
                state.ConvertedText[endOffsetBeforeSeparator] == 0 ||
                IsWordSeparator(state.ConvertedText, endOffsetBeforeSeparator);

            if (endIsSuffixedByWordSeparator)
            {
                delimitedText[delimitedTextOffset++] = (byte)'\\';
                delimitedText[delimitedTextOffset++] = (byte)'b';
            }
            else
            {
                delimitedText[delimitedTextOffset++] = (byte)'a';
            }

            // Create our temporary match state for the next match.
            bool matched = false;

            // Insert the null terminator.
            delimitedText[delimitedTextOffset] = 0;

            // Check if the delimited word is on the dictionary.
            state.DelimitedWordsTrie.Match(delimitedText, MatchSimple, ref matched);

            // If we have a match, mask the word.
            if (matched)
            {
                MatchCommon(ref state, convertedStartOffset, endOffsetBeforeSeparator);
            }

            return true;
        }

        private static void MatchCommon(ref MatchState state, int matchStartOffset, int matchEndOffset)
        {
            // If length is zero or negative, there was no match.
            if (matchStartOffset >= matchEndOffset)
            {
                return;
            }

            Span<byte> convertedText = state.ConvertedText;
            Span<byte> originalText = state.OriginalText;

            int matchLength = matchEndOffset - matchStartOffset;
            int characterCount = Encoding.UTF8.GetCharCount(state.ConvertedText.Slice(matchStartOffset, matchLength));

            // Exit early if there are no character, or if we matched past the end of the string.
            if (characterCount == 0 ||
                (matchStartOffset > 0 && convertedText[matchStartOffset - 1] == 0) ||
                (matchStartOffset > 1 && convertedText[matchStartOffset - 2] == 0))
            {
                return;
            }

            state.MaskedCount++;

            (int originalStartOffset, int originalEndOffset) = state.GetOriginalRange(matchStartOffset, matchEndOffset);

            PreMaskCharacterRange(convertedText, matchStartOffset, matchEndOffset, state.MaskMode, characterCount);
            PreMaskCharacterRange(originalText, originalStartOffset, originalEndOffset, state.MaskMode, characterCount);
        }

        private static bool MatchDelimitedWordSimple(ReadOnlySpan<byte> text, int matchStartOffset, int matchEndOffset, int nodeId, ref MatchDelimitedState state)
        {
            int convertedStartOffset = state.NoSeparatorMap.Set.Select0(matchStartOffset);

            Span<byte> delimitedText = new byte[64];

            // If the word is prefixed by a word separator, insert "\b" delimiter, otherwise insert "a" delimitar.
            // The start of the string is also considered a "word separator".

            bool startIsPrefixedByWordSeparator =
                (convertedStartOffset == 0 && state.PrevCharIsWordSeparator) ||
                state.NoSeparatorMap.Set.Has(convertedStartOffset - 1);

            int delimitedTextOffset = 0;

            if (startIsPrefixedByWordSeparator)
            {
                delimitedText[delimitedTextOffset++] = (byte)'\\';
                delimitedText[delimitedTextOffset++] = (byte)'b';
            }
            else
            {
                delimitedText[delimitedTextOffset++] = (byte)'a';
            }

            // Copy the word to our temporary buffer used for the next match.

            int matchLength = matchEndOffset - matchStartOffset;

            text.Slice(matchStartOffset, matchLength).CopyTo(delimitedText.Slice(delimitedTextOffset, matchLength));

            delimitedTextOffset += matchLength;

            // If the word is suffixed by a word separator, insert "\b" delimiter, otherwise insert "a" delimiter.
            // The end of the string is also considered a "word separator".

            int convertedEndOffset = state.NoSeparatorMap.Set.Select0(matchEndOffset);

            bool endIsSuffixedByWordSeparator =
                (convertedEndOffset < 0 && state.NextCharIsWordSeparator) ||
                state.NoSeparatorMap.Set.Has(convertedEndOffset - 1);

            if (endIsSuffixedByWordSeparator)
            {
                delimitedText[delimitedTextOffset++] = (byte)'\\';
                delimitedText[delimitedTextOffset++] = (byte)'b';
            }
            else
            {
                delimitedText[delimitedTextOffset++] = (byte)'a';
            }

            // Create our temporary match state for the next match.
            bool matched = false;

            // Insert the null terminator.
            delimitedText[delimitedTextOffset] = 0;

            // Check if the delimited word is on the dictionary.
            state.DelimitedWordsTrie.Match(delimitedText, MatchSimple, ref matched);

            // If we have a match, mask the word.
            if (matched)
            {
                state.Matched = true;
            }

            return !matched;
        }

        private static bool MatchAndReplace(ReadOnlySpan<byte> text, int matchStartOffset, int matchEndOffset, int nodeId, ref MatchSimilarFormState state)
        {
            if (matchStartOffset < state.ReplaceEndOffset || state.MatchRanges.Count == 0)
            {
                return true;
            }

            // Check if the match range exists on our list of ranges.
            int rangeIndex = state.MatchRanges.Find(matchStartOffset, matchEndOffset);

            if ((uint)rangeIndex >= (uint)state.MatchRanges.Count)
            {
                return true;
            }

            MatchRange range = state.MatchRanges[rangeIndex];

            // We only replace if the match has the same size or is larger than an existing match on the list.
            if (range.StartOffset <= matchStartOffset &&
                (range.StartOffset != matchStartOffset || range.EndOffset <= matchEndOffset))
            {
                // Copy all characters since the last match to the output.
                int endOffset = state.ReplaceEndOffset;

                if (endOffset < matchStartOffset)
                {
                    state.CanonicalText = state.CanonicalText.Append(text[endOffset..matchStartOffset]);
                }

                // Get canonical character from the similar one, and append it.
                // For example, |-| is replaced with h, vv is replaced with w, etc.
                ReadOnlySpan<byte> matchText = text[matchStartOffset..matchEndOffset];
                state.CanonicalText = state.CanonicalText.AppendNullTerminated(state.SimilarFormTable.FindCanonicalString(matchText));
                state.ReplaceEndOffset = matchEndOffset;
            }

            return true;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _contentsReader.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

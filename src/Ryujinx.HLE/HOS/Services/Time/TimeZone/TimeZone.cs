using Ryujinx.Common;
using Ryujinx.Common.Memory;
using Ryujinx.Common.Utilities;
using Ryujinx.HLE.Utilities;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static Ryujinx.HLE.HOS.Services.Time.TimeZone.TimeZoneRule;

namespace Ryujinx.HLE.HOS.Services.Time.TimeZone
{
    public class TimeZone
    {
        private const int TimeTypeSize = 8;
        private const int EpochYear = 1970;
        private const int YearBase = 1900;
        private const int EpochWeekDay = 4;
        private const int SecondsPerMinute = 60;
        private const int MinutesPerHour = 60;
        private const int HoursPerDays = 24;
        private const int DaysPerWeek = 7;
        private const int DaysPerNYear = 365;
        private const int DaysPerLYear = 366;
        private const int MonthsPerYear = 12;
        private const int SecondsPerHour = SecondsPerMinute * MinutesPerHour;
        private const int SecondsPerDay = SecondsPerHour * HoursPerDays;

        private const int YearsPerRepeat = 400;
        private const long AverageSecondsPerYear = 31556952;
        private const long SecondsPerRepeat = YearsPerRepeat * AverageSecondsPerYear;

        private static readonly int[] _yearLengths = [DaysPerNYear, DaysPerLYear];
        private static readonly int[][] _monthsLengths =
        [
            [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31],
            [31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31]
        ];

        private static ReadOnlySpan<byte> TimeZoneDefaultRule => ",M4.1.0,M10.5.0"u8;

        [StructLayout(LayoutKind.Sequential, Pack = 0x4, Size = 0x10)]
        private struct CalendarTimeInternal
        {
            // NOTE: On the IPC side this is supposed to be a 16 bits value but internally this need to be a 64 bits value for ToPosixTime.
            public long Year;
            public sbyte Month;
            public sbyte Day;
            public sbyte Hour;
            public sbyte Minute;
            public sbyte Second;

            public readonly int CompareTo(CalendarTimeInternal other)
            {
                if (Year != other.Year)
                {
                    if (Year < other.Year)
                    {
                        return -1;
                    }

                    return 1;
                }

                if (Month != other.Month)
                {
                    return Month - other.Month;
                }

                if (Day != other.Day)
                {
                    return Day - other.Day;
                }

                if (Hour != other.Hour)
                {
                    return Hour - other.Hour;
                }

                if (Minute != other.Minute)
                {
                    return Minute - other.Minute;
                }

                if (Second != other.Second)
                {
                    return Second - other.Second;
                }

                return 0;
            }
        }

        private enum RuleType
        {
            JulianDay,
            DayOfYear,
            MonthNthDayOfWeek,
        }

        private struct Rule
        {
            public RuleType Type;
            public int Day;
            public int Week;
            public int Month;
            public int TransitionTime;
        }

        private static int Detzcode32(ReadOnlySpan<byte> bytes)
        {
            return BinaryPrimitives.ReadInt32BigEndian(bytes);
        }

        private static int Detzcode32(int value)
        {
            if (BitConverter.IsLittleEndian)
            {
                return BinaryPrimitives.ReverseEndianness(value);
            }

            return value;
        }

        private static long Detzcode64(ReadOnlySpan<byte> bytes)
        {
            return BinaryPrimitives.ReadInt64BigEndian(bytes);
        }

        private static bool DifferByRepeat(long t1, long t0)
        {
            return (t1 - t0) == SecondsPerRepeat;
        }

        private static bool TimeTypeEquals(in TimeZoneRule outRules, byte aIndex, byte bIndex)
        {
            if (aIndex < 0 || aIndex >= outRules.TypeCount || bIndex < 0 || bIndex >= outRules.TypeCount)
            {
                return false;
            }

            TimeTypeInfo a = outRules.Ttis[aIndex];
            TimeTypeInfo b = outRules.Ttis[bIndex];

            return a.GmtOffset == b.GmtOffset &&
                   a.IsDaySavingTime == b.IsDaySavingTime &&
                   a.IsStandardTimeDaylight == b.IsStandardTimeDaylight &&
                   a.IsGMT == b.IsGMT &&
                   StringUtils.CompareCStr(outRules.Chars[a.AbbreviationListIndex..], outRules.Chars[b.AbbreviationListIndex..]) == 0;
        }

        private static int GetQZName(ReadOnlySpan<byte> name, int namePosition, char delimiter)
        {
            int i = namePosition;

            while (name[i] != '\0' && name[i] != delimiter)
            {
                i++;
            }

            return i;
        }

        private static int GetTZName(ReadOnlySpan<byte> name, int namePosition)
        {
            int i = namePosition;

            char c;

            while ((c = (char)name[i]) != '\0' && !char.IsDigit(c) && c != ',' && c != '-' && c != '+')
            {
                i++;
            }

            return i;
        }

        private static bool GetNum(ReadOnlySpan<byte> name, ref int namePosition, out int num, int min, int max)
        {
            num = 0;

            if (namePosition >= name.Length)
            {
                return false;
            }

            char c = (char)name[namePosition];

            if (!char.IsDigit(c))
            {
                return false;
            }

            do
            {
                num = num * 10 + (c - '0');
                if (num > max)
                {
                    return false;
                }

                if (++namePosition >= name.Length)
                {
                    return false;
                }

                c = (char)name[namePosition];
            }
            while (char.IsDigit(c));

            if (num < min)
            {
                return false;
            }

            return true;
        }

        private static bool GetSeconds(ReadOnlySpan<byte> name, ref int namePosition, out int seconds)
        {
            seconds = 0;


            bool isValid = GetNum(name, ref namePosition, out int num, 0, HoursPerDays * DaysPerWeek - 1);
            if (!isValid)
            {
                return false;
            }

            seconds = num * SecondsPerHour;

            if (namePosition >= name.Length)
            {
                return false;
            }

            if (name[namePosition] == ':')
            {
                namePosition++;
                isValid = GetNum(name, ref namePosition, out num, 0, MinutesPerHour - 1);
                if (!isValid)
                {
                    return false;
                }

                seconds += num * SecondsPerMinute;

                if (namePosition >= name.Length)
                {
                    return false;
                }

                if (name[namePosition] == ':')
                {
                    namePosition++;
                    isValid = GetNum(name, ref namePosition, out num, 0, SecondsPerMinute);
                    if (!isValid)
                    {
                        return false;
                    }

                    seconds += num;
                }
            }
            return true;
        }

        private static bool GetOffset(ReadOnlySpan<byte> name, ref int namePosition, ref int offset)
        {
            bool isNegative = false;

            if (namePosition >= name.Length)
            {
                return false;
            }

            if (name[namePosition] == '-')
            {
                isNegative = true;
                namePosition++;
            }
            else if (name[namePosition] == '+')
            {
                namePosition++;
            }

            if (namePosition >= name.Length)
            {
                return false;
            }

            bool isValid = GetSeconds(name, ref namePosition, out offset);
            if (!isValid)
            {
                return false;
            }

            if (isNegative)
            {
                offset = -offset;
            }

            return true;
        }

        private static bool GetRule(ReadOnlySpan<byte> name, ref int namePosition, out Rule rule)
        {
            rule = new Rule();

            bool isValid;

            if (name[namePosition] == 'J')
            {
                namePosition++;

                rule.Type = RuleType.JulianDay;
                isValid = GetNum(name, ref namePosition, out rule.Day, 1, DaysPerNYear);
            }
            else if (name[namePosition] == 'M')
            {
                namePosition++;

                rule.Type = RuleType.MonthNthDayOfWeek;
                isValid = GetNum(name, ref namePosition, out rule.Month, 1, MonthsPerYear);

                if (!isValid)
                {
                    return false;
                }

                if (name[namePosition++] != '.')
                {
                    return false;
                }

                isValid = GetNum(name, ref namePosition, out rule.Week, 1, 5);
                if (!isValid)
                {
                    return false;
                }

                if (name[namePosition++] != '.')
                {
                    return false;
                }

                isValid = GetNum(name, ref namePosition, out rule.Day, 0, DaysPerWeek - 1);
            }
            else if (char.IsDigit((char)name[namePosition]))
            {
                rule.Type = RuleType.DayOfYear;
                isValid = GetNum(name, ref namePosition, out rule.Day, 0, DaysPerLYear - 1);
            }
            else
            {
                return false;
            }

            if (!isValid)
            {
                return false;
            }

            if (name[namePosition] == '/')
            {
                namePosition++;
                return GetOffset(name, ref namePosition, ref rule.TransitionTime);
            }
            else
            {
                rule.TransitionTime = 2 * SecondsPerHour;
            }

            return true;
        }

        private static int IsLeap(int year)
        {
            if (((year) % 4) == 0 && (((year) % 100) != 0 || ((year) % 400) == 0))
            {
                return 1;
            }

            return 0;
        }

        private static bool ParsePosixName(ReadOnlySpan<byte> name, ref TimeZoneRule outRules, bool lastDitch)
        {
            outRules = new TimeZoneRule();

            int stdLen;

            ReadOnlySpan<byte> stdName = name;
            int namePosition = 0;
            int stdOffset = 0;

            if (lastDitch)
            {
                stdLen = 3;
                namePosition += stdLen;
            }
            else
            {
                if (name[namePosition] == '<')
                {
                    namePosition++;

                    stdName = name[namePosition..];

                    int stdNamePosition = namePosition;

                    namePosition = GetQZName(name, namePosition, '>');

                    if (name[namePosition] != '>')
                    {
                        return false;
                    }

                    stdLen = namePosition - stdNamePosition;
                    namePosition++;
                }
                else
                {
                    namePosition = GetTZName(name, namePosition);
                    stdLen = namePosition;
                }

                if (stdLen == 0)
                {
                    return false;
                }

                bool isValid = GetOffset(name.ToArray(), ref namePosition, ref stdOffset);

                if (!isValid)
                {
                    return false;
                }
            }

            int charCount = stdLen + 1;
            int destLen = 0;
            int dstOffset = 0;

            ReadOnlySpan<byte> destName = name[namePosition..];

            if (TzCharsArraySize < charCount)
            {
                return false;
            }

            if (name[namePosition] != '\0')
            {
                if (name[namePosition] == '<')
                {
                    destName = name[++namePosition..];
                    int destNamePosition = namePosition;

                    namePosition = GetQZName(name.ToArray(), namePosition, '>');

                    if (name[namePosition] != '>')
                    {
                        return false;
                    }

                    destLen = namePosition - destNamePosition;
                    namePosition++;
                }
                else
                {
                    destName = name[namePosition..];
                    namePosition = GetTZName(name, namePosition);
                    destLen = namePosition;
                }

                if (destLen == 0)
                {
                    return false;
                }

                charCount += destLen + 1;
                if (TzCharsArraySize < charCount)
                {
                    return false;
                }

                if (name[namePosition] != '\0' && name[namePosition] != ',' && name[namePosition] != ';')
                {
                    bool isValid = GetOffset(name.ToArray(), ref namePosition, ref dstOffset);

                    if (!isValid)
                    {
                        return false;
                    }
                }
                else
                {
                    dstOffset = stdOffset - SecondsPerHour;
                }

                if (name[namePosition] == '\0')
                {
                    name = TimeZoneDefaultRule;
                    namePosition = 0;
                }

                if (name[namePosition] == ',' || name[namePosition] == ';')
                {
                    namePosition++;

                    bool isRuleValid = GetRule(name, ref namePosition, out Rule start);
                    if (!isRuleValid)
                    {
                        return false;
                    }

                    if (name[namePosition++] != ',')
                    {
                        return false;
                    }

                    isRuleValid = GetRule(name, ref namePosition, out Rule end);
                    if (!isRuleValid)
                    {
                        return false;
                    }

                    if (name[namePosition] != '\0')
                    {
                        return false;
                    }

                    outRules.TypeCount = 2;

                    outRules.Ttis[0] = new TimeTypeInfo
                    {
                        GmtOffset = -dstOffset,
                        IsDaySavingTime = true,
                        AbbreviationListIndex = stdLen + 1,
                    };

                    outRules.Ttis[1] = new TimeTypeInfo
                    {
                        GmtOffset = -stdOffset,
                        IsDaySavingTime = false,
                        AbbreviationListIndex = 0,
                    };

                    outRules.DefaultType = 0;

                    int timeCount = 0;
                    long janFirst = 0;
                    int janOffset = 0;
                    int yearBegining = EpochYear;

                    do
                    {
                        int yearSeconds = _yearLengths[IsLeap(yearBegining - 1)] * SecondsPerDay;
                        yearBegining--;
                        if (IncrementOverflow64(ref janFirst, -yearSeconds))
                        {
                            janOffset = -yearSeconds;
                            break;
                        }
                    }
                    while (EpochYear - YearsPerRepeat / 2 < yearBegining);

                    int yearLimit = yearBegining + YearsPerRepeat + 1;
                    int year;
                    for (year = yearBegining; year < yearLimit; year++)
                    {
                        int startTime = TransitionTime(year, start, stdOffset);
                        int endTime = TransitionTime(year, end, dstOffset);

                        int yearSeconds = _yearLengths[IsLeap(year)] * SecondsPerDay;

                        bool isReversed = endTime < startTime;
                        if (isReversed)
                        {
                            (endTime, startTime) = (startTime, endTime);
                        }

                        if (isReversed || (startTime < endTime && (endTime - startTime < (yearSeconds + (stdOffset - dstOffset)))))
                        {
                            if (TzMaxTimes - 2 < timeCount)
                            {
                                break;
                            }

                            outRules.Ats[timeCount] = janFirst;
                            if (!IncrementOverflow64(ref outRules.Ats[timeCount], janOffset + startTime))
                            {
                                outRules.Types[timeCount++] = isReversed ? (byte)1 : (byte)0;
                            }
                            else if (janOffset != 0)
                            {
                                outRules.DefaultType = isReversed ? 1 : 0;
                            }

                            outRules.Ats[timeCount] = janFirst;
                            if (!IncrementOverflow64(ref outRules.Ats[timeCount], janOffset + endTime))
                            {
                                outRules.Types[timeCount++] = isReversed ? (byte)0 : (byte)1;
                                yearLimit = year + YearsPerRepeat + 1;
                            }
                            else if (janOffset != 0)
                            {
                                outRules.DefaultType = isReversed ? 0 : 1;
                            }
                        }

                        if (IncrementOverflow64(ref janFirst, janOffset + yearSeconds))
                        {
                            break;
                        }

                        janOffset = 0;
                    }

                    outRules.TimeCount = timeCount;

                    // There is no time variation, this is then a perpetual DST rule
                    if (timeCount == 0)
                    {
                        outRules.TypeCount = 1;
                    }
                    else if (YearsPerRepeat < year - yearBegining)
                    {
                        outRules.GoBack = true;
                        outRules.GoAhead = true;
                    }
                }
                else
                {
                    if (name[namePosition] == '\0')
                    {
                        return false;
                    }

                    long theirStdOffset = 0;
                    for (int i = 0; i < outRules.TimeCount; i++)
                    {
                        int j = outRules.Types[i];
                        if (outRules.Ttis[j].IsStandardTimeDaylight)
                        {
                            theirStdOffset = -outRules.Ttis[j].GmtOffset;
                        }
                    }

                    long theirDstOffset;
                    for (int i = 0; i < outRules.TimeCount; i++)
                    {
                        int j = outRules.Types[i];
                        if (outRules.Ttis[j].IsDaySavingTime)
                        {
#pragma warning disable IDE0059 // Remove unnecessary value assignment
                            theirDstOffset = -outRules.Ttis[j].GmtOffset;
#pragma warning restore IDE0059
                        }
                    }

                    bool isDaySavingTime = false;
#pragma warning disable IDE0059 // Remove unnecessary value assignment
                    long theirOffset = theirStdOffset;
#pragma warning restore IDE0059
                    for (int i = 0; i < outRules.TimeCount; i++)
                    {
                        int j = outRules.Types[i];
                        outRules.Types[i] = outRules.Ttis[j].IsDaySavingTime ? (byte)1 : (byte)0;
                        if (!outRules.Ttis[j].IsGMT)
                        {
                            if (isDaySavingTime && !outRules.Ttis[j].IsStandardTimeDaylight)
                            {
                                outRules.Ats[i] += dstOffset - theirStdOffset;
                            }
                            else
                            {
                                outRules.Ats[i] += stdOffset - theirStdOffset;
                            }
                        }

                        theirOffset = -outRules.Ttis[j].GmtOffset;
                        if (outRules.Ttis[j].IsDaySavingTime)
                        {
#pragma warning disable IDE0059 // Remove unnecessary value assignment
                            theirDstOffset = theirOffset;
#pragma warning restore IDE0059
                        }
                        else
                        {
                            theirStdOffset = theirOffset;
                        }
                    }

                    outRules.Ttis[0] = new TimeTypeInfo
                    {
                        GmtOffset = -stdOffset,
                        IsDaySavingTime = false,
                        AbbreviationListIndex = 0,
                    };

                    outRules.Ttis[1] = new TimeTypeInfo
                    {
                        GmtOffset = -dstOffset,
                        IsDaySavingTime = true,
                        AbbreviationListIndex = stdLen + 1,
                    };

                    outRules.TypeCount = 2;
                    outRules.DefaultType = 0;
                }
            }
            else
            {
                // default is perpetual standard time
                outRules.TypeCount = 1;
                outRules.TimeCount = 0;
                outRules.DefaultType = 0;
                outRules.Ttis[0] = new TimeTypeInfo
                {
                    GmtOffset = -stdOffset,
                    IsDaySavingTime = false,
                    AbbreviationListIndex = 0,
                };
            }

            outRules.CharCount = charCount;

            int charsPosition = 0;

            for (int i = 0; i < stdLen; i++)
            {
                outRules.Chars[i] = stdName[i];
            }

            charsPosition += stdLen;
            outRules.Chars[charsPosition++] = 0;

            if (destLen != 0)
            {
                for (int i = 0; i < destLen; i++)
                {
                    outRules.Chars[charsPosition + i] = destName[i];
                }
                outRules.Chars[charsPosition + destLen] = 0;
            }

            return true;
        }

        private static int TransitionTime(int year, Rule rule, int offset)
        {
            int leapYear = IsLeap(year);

            int value;
            switch (rule.Type)
            {
                case RuleType.JulianDay:
                    value = (rule.Day - 1) * SecondsPerDay;
                    if (leapYear == 1 && rule.Day >= 60)
                    {
                        value += SecondsPerDay;
                    }
                    break;

                case RuleType.DayOfYear:
                    value = rule.Day * SecondsPerDay;
                    break;

                case RuleType.MonthNthDayOfWeek:
                    // Here we use Zeller's Congruence to get the day of week of the first month.

                    int m1 = (rule.Month + 9) % 12 + 1;
                    int yy0 = (rule.Month <= 2) ? (year - 1) : year;
                    int yy1 = yy0 / 100;
                    int yy2 = yy0 % 100;

                    int dayOfWeek = ((26 * m1 - 2) / 10 + 1 + yy2 + yy2 / 4 + yy1 / 4 - 2 * yy1) % 7;

                    if (dayOfWeek < 0)
                    {
                        dayOfWeek += DaysPerWeek;
                    }

                    // Get the zero origin
                    int d = rule.Day - dayOfWeek;

                    if (d < 0)
                    {
                        d += DaysPerWeek;
                    }

                    for (int i = 1; i < rule.Week; i++)
                    {
                        if (d + DaysPerWeek >= _monthsLengths[leapYear][rule.Month - 1])
                        {
                            break;
                        }

                        d += DaysPerWeek;
                    }

                    value = d * SecondsPerDay;
                    for (int i = 0; i < rule.Month - 1; i++)
                    {
                        value += _monthsLengths[leapYear][i] * SecondsPerDay;
                    }

                    break;
                default:
                    throw new NotImplementedException("Unknown time transition!");
            }

            return value + rule.TransitionTime + offset;
        }

        private static bool NormalizeOverflow32(ref int ip, ref int unit, int baseValue)
        {
            int delta;

            if (unit >= 0)
            {
                delta = unit / baseValue;
            }
            else
            {
                delta = -1 - (-1 - unit) / baseValue;
            }

            unit -= delta * baseValue;

            return IncrementOverflow32(ref ip, delta);
        }

        private static bool NormalizeOverflow64(ref long ip, ref long unit, long baseValue)
        {
            long delta;

            if (unit >= 0)
            {
                delta = unit / baseValue;
            }
            else
            {
                delta = -1 - (-1 - unit) / baseValue;
            }

            unit -= delta * baseValue;

            return IncrementOverflow64(ref ip, delta);
        }

        private static bool IncrementOverflow32(ref int time, int j)
        {
            try
            {
                time = checked(time + j);

                return false;
            }
            catch (OverflowException)
            {
                return true;
            }
        }

        private static bool IncrementOverflow64(ref long time, long j)
        {
            try
            {
                time = checked(time + j);

                return false;
            }
            catch (OverflowException)
            {
                return true;
            }
        }

        internal static bool ParsePosixName(string name, ref TimeZoneRule outRules)
        {
            return ParsePosixName(Encoding.ASCII.GetBytes(name), ref outRules, false);
        }

        internal static bool ParseTimeZoneBinary(ref TimeZoneRule outRules, Stream inputData)
        {
            outRules = new TimeZoneRule();

            BinaryReader reader = new(inputData);

            long streamLength = reader.BaseStream.Length;

            if (streamLength < Unsafe.SizeOf<TzifHeader>())
            {
                return false;
            }

            TzifHeader header = reader.ReadStruct<TzifHeader>();

            streamLength -= Unsafe.SizeOf<TzifHeader>();

            int ttisGMTCount = Detzcode32(header.TtisGMTCount);
            int ttisSTDCount = Detzcode32(header.TtisSTDCount);
            int leapCount = Detzcode32(header.LeapCount);
            int timeCount = Detzcode32(header.TimeCount);
            int typeCount = Detzcode32(header.TypeCount);
            int charCount = Detzcode32(header.CharCount);

            if (!(0 <= leapCount
                && leapCount < TzMaxLeaps
                && 0 < typeCount
                && typeCount < TzMaxTypes
                && 0 <= timeCount
                && timeCount < TzMaxTimes
                && 0 <= charCount
                && charCount < TzMaxChars
                && (ttisSTDCount == typeCount || ttisSTDCount == 0)
                && (ttisGMTCount == typeCount || ttisGMTCount == 0)))
            {
                return false;
            }


            if (streamLength < (timeCount * TimeTypeSize
                                 + timeCount
                                 + typeCount * 6
                                 + charCount
                                 + leapCount * (TimeTypeSize + 4)
                                 + ttisSTDCount
                                 + ttisGMTCount))
            {
                return false;
            }

            outRules.TimeCount = timeCount;
            outRules.TypeCount = typeCount;
            outRules.CharCount = charCount;

            byte[] workBuffer = StreamUtils.StreamToBytes(inputData);

            timeCount = 0;

            {
                Span<byte> p = workBuffer;
                for (int i = 0; i < outRules.TimeCount; i++)
                {
                    long at = Detzcode64(p);
                    outRules.Types[i] = 1;

                    if (timeCount != 0 && at <= outRules.Ats[timeCount - 1])
                    {
                        if (at < outRules.Ats[timeCount - 1])
                        {
                            return false;
                        }

                        outRules.Types[i - 1] = 0;
                        timeCount--;
                    }

                    outRules.Ats[timeCount++] = at;

                    p = p[TimeTypeSize..];
                }

                timeCount = 0;
                for (int i = 0; i < outRules.TimeCount; i++)
                {
                    byte type = p[0];
                    p = p[1..];

                    if (outRules.TypeCount <= type)
                    {
                        return false;
                    }

                    if (outRules.Types[i] != 0)
                    {
                        outRules.Types[timeCount++] = type;
                    }
                }

                outRules.TimeCount = timeCount;

                for (int i = 0; i < outRules.TypeCount; i++)
                {
                    TimeTypeInfo ttis = outRules.Ttis[i];
                    ttis.GmtOffset = Detzcode32(p);
                    p = p[sizeof(int)..];

                    if (p[0] >= 2)
                    {
                        return false;
                    }

                    ttis.IsDaySavingTime = p[0] != 0;
                    p = p[1..];

                    int abbreviationListIndex = p[0];
                    p = p[1..];

                    if (abbreviationListIndex >= outRules.CharCount)
                    {
                        return false;
                    }

                    ttis.AbbreviationListIndex = abbreviationListIndex;

                    outRules.Ttis[i] = ttis;
                }

                p[..outRules.CharCount].CopyTo(outRules.Chars);

                p = p[outRules.CharCount..];
                outRules.Chars[outRules.CharCount] = 0;

                for (int i = 0; i < outRules.TypeCount; i++)
                {
                    if (ttisSTDCount == 0)
                    {
                        outRules.Ttis[i].IsStandardTimeDaylight = false;
                    }
                    else
                    {
                        if (p[0] >= 2)
                        {
                            return false;
                        }

                        outRules.Ttis[i].IsStandardTimeDaylight = p[0] != 0;
                        p = p[1..];
                    }
                }

                for (int i = 0; i < outRules.TypeCount; i++)
                {
                    if (ttisSTDCount == 0)
                    {
                        outRules.Ttis[i].IsGMT = false;
                    }
                    else
                    {
                        if (p[0] >= 2)
                        {
                            return false;
                        }

                        outRules.Ttis[i].IsGMT = p[0] != 0;
                        p = p[1..];
                    }

                }

                long position = (workBuffer.Length - p.Length);
                long nRead = streamLength - position;

                if (nRead < 0)
                {
                    return false;
                }

                // Nintendo abort in case of a TzIf file with a POSIX TZ Name too long to fit inside a TimeZoneRule.
                // As it's impossible in normal usage to achive this, we also force a crash.
                if (nRead > (TzNameMax + 1))
                {
                    throw new InvalidOperationException();
                }

                byte[] tempName = new byte[TzNameMax + 1];
                Array.Copy(workBuffer, position, tempName, 0, nRead);

                if (nRead > 2 && tempName[0] == '\n' && tempName[nRead - 1] == '\n' && outRules.TypeCount + 2 <= TzMaxTypes)
                {
                    tempName[nRead - 1] = 0;

                    byte[] name = new byte[TzNameMax];
                    Array.Copy(tempName, 1, name, 0, nRead - 1);

                    Box<TimeZoneRule> tempRulesBox = new();
                    ref TimeZoneRule tempRules = ref tempRulesBox.Data;

                    if (ParsePosixName(name, ref tempRulesBox.Data, false))
                    {
                        int abbreviationCount = 0;
                        charCount = outRules.CharCount;

                        Span<byte> chars = outRules.Chars;

                        for (int i = 0; i < tempRules.TypeCount; i++)
                        {
                            ReadOnlySpan<byte> tempChars = tempRules.Chars;
                            ReadOnlySpan<byte> tempAbbreviation = tempChars[tempRules.Ttis[i].AbbreviationListIndex..];

                            int j;

                            for (j = 0; j < charCount; j++)
                            {
                                if (StringUtils.CompareCStr(chars[j..], tempAbbreviation) == 0)
                                {
                                    tempRules.Ttis[i].AbbreviationListIndex = j;
                                    abbreviationCount++;
                                    break;
                                }
                            }

                            if (j >= charCount)
                            {
                                int abbreviationLength = StringUtils.LengthCstr(tempAbbreviation);
                                if (j + abbreviationLength < TzMaxChars)
                                {
                                    for (int x = 0; x < abbreviationLength; x++)
                                    {
                                        chars[j + x] = tempAbbreviation[x];
                                    }

                                    charCount = j + abbreviationLength + 1;

                                    tempRules.Ttis[i].AbbreviationListIndex = j;
                                    abbreviationCount++;
                                }
                            }
                        }

                        if (abbreviationCount == tempRules.TypeCount)
                        {
                            outRules.CharCount = charCount;

                            // Remove trailing
                            while (1 < outRules.TimeCount && (outRules.Types[outRules.TimeCount - 1] == outRules.Types[outRules.TimeCount - 2]))
                            {
                                outRules.TimeCount--;
                            }

                            int i;

                            for (i = 0; i < tempRules.TimeCount; i++)
                            {
                                if (outRules.TimeCount == 0 || outRules.Ats[outRules.TimeCount - 1] < tempRules.Ats[i])
                                {
                                    break;
                                }
                            }

                            while (i < tempRules.TimeCount && outRules.TimeCount < TzMaxTimes)
                            {
                                outRules.Ats[outRules.TimeCount] = tempRules.Ats[i];
                                outRules.Types[outRules.TimeCount] = (byte)(outRules.TypeCount + (byte)tempRules.Types[i]);

                                outRules.TimeCount++;
                                i++;
                            }

                            for (i = 0; i < tempRules.TypeCount; i++)
                            {
                                outRules.Ttis[outRules.TypeCount++] = tempRules.Ttis[i];
                            }
                        }
                    }
                }

                if (outRules.TypeCount == 0)
                {
                    return false;
                }

                if (outRules.TimeCount > 1)
                {
                    for (int i = 1; i < outRules.TimeCount; i++)
                    {
                        if (TimeTypeEquals(in outRules, outRules.Types[i], outRules.Types[0]) && DifferByRepeat(outRules.Ats[i], outRules.Ats[0]))
                        {
                            outRules.GoBack = true;
                            break;
                        }
                    }

                    for (int i = outRules.TimeCount - 2; i >= 0; i--)
                    {
                        if (TimeTypeEquals(in outRules, outRules.Types[outRules.TimeCount - 1], outRules.Types[i]) && DifferByRepeat(outRules.Ats[outRules.TimeCount - 1], outRules.Ats[i]))
                        {
                            outRules.GoAhead = true;
                            break;
                        }
                    }
                }

                int defaultType;

                for (defaultType = 0; defaultType < outRules.TimeCount; defaultType++)
                {
                    if (outRules.Types[defaultType] == 0)
                    {
                        break;
                    }
                }

                defaultType = defaultType < outRules.TimeCount ? -1 : 0;

                if (defaultType < 0 && outRules.TimeCount > 0 && outRules.Ttis[outRules.Types[0]].IsDaySavingTime)
                {
                    defaultType = outRules.Types[0];
                    while (--defaultType >= 0)
                    {
                        if (!outRules.Ttis[defaultType].IsDaySavingTime)
                        {
                            break;
                        }
                    }
                }

                if (defaultType < 0)
                {
                    defaultType = 0;
                    while (outRules.Ttis[defaultType].IsDaySavingTime)
                    {
                        if (++defaultType >= outRules.TypeCount)
                        {
                            defaultType = 0;
                            break;
                        }
                    }
                }

                outRules.DefaultType = defaultType;
            }

            return true;
        }

        private static long GetLeapDaysNotNeg(long year)
        {
            return year / 4 - year / 100 + year / 400;
        }

        private static long GetLeapDays(long year)
        {
            if (year < 0)
            {
                return -1 - GetLeapDaysNotNeg(-1 - year);
            }
            else
            {
                return GetLeapDaysNotNeg(year);
            }
        }

        private static ResultCode CreateCalendarTime(long time, int gmtOffset, out CalendarTimeInternal calendarTime, out CalendarAdditionalInfo calendarAdditionalInfo)
        {
            long year = EpochYear;
            long timeDays = time / SecondsPerDay;
            long remainingSeconds = time % SecondsPerDay;

            calendarTime = new CalendarTimeInternal();
            calendarAdditionalInfo = new CalendarAdditionalInfo();

            while (timeDays < 0 || timeDays >= _yearLengths[IsLeap((int)year)])
            {
                long timeDelta = timeDays / DaysPerLYear;
                long delta = timeDelta;

                if (delta == 0)
                {
                    delta = timeDays < 0 ? -1 : 1;
                }

                long newYear = year;

                if (IncrementOverflow64(ref newYear, delta))
                {
                    return ResultCode.OutOfRange;
                }

                long leapDays = GetLeapDays(newYear - 1) - GetLeapDays(year - 1);
                timeDays -= (newYear - year) * DaysPerNYear;
                timeDays -= leapDays;
                year = newYear;
            }

            long dayOfYear = timeDays;
            remainingSeconds += gmtOffset;
            while (remainingSeconds < 0)
            {
                remainingSeconds += SecondsPerDay;
                dayOfYear -= 1;
            }

            while (remainingSeconds >= SecondsPerDay)
            {
                remainingSeconds -= SecondsPerDay;
                dayOfYear += 1;
            }

            while (dayOfYear < 0)
            {
                if (IncrementOverflow64(ref year, -1))
                {
                    return ResultCode.OutOfRange;
                }

                dayOfYear += _yearLengths[IsLeap((int)year)];
            }

            while (dayOfYear >= _yearLengths[IsLeap((int)year)])
            {
                dayOfYear -= _yearLengths[IsLeap((int)year)];

                if (IncrementOverflow64(ref year, 1))
                {
                    return ResultCode.OutOfRange;
                }
            }

            calendarTime.Year = year;
            calendarAdditionalInfo.DayOfYear = (uint)dayOfYear;

            long dayOfWeek = (EpochWeekDay + ((year - EpochYear) % DaysPerWeek) * (DaysPerNYear % DaysPerWeek) + GetLeapDays(year - 1) - GetLeapDays(EpochYear - 1) + dayOfYear) % DaysPerWeek;
            if (dayOfWeek < 0)
            {
                dayOfWeek += DaysPerWeek;
            }

            calendarAdditionalInfo.DayOfWeek = (uint)dayOfWeek;

            calendarTime.Hour = (sbyte)((remainingSeconds / SecondsPerHour) % SecondsPerHour);
            remainingSeconds %= SecondsPerHour;

            calendarTime.Minute = (sbyte)(remainingSeconds / SecondsPerMinute);
            calendarTime.Second = (sbyte)(remainingSeconds % SecondsPerMinute);

            int[] ip = _monthsLengths[IsLeap((int)year)];

            for (calendarTime.Month = 0; dayOfYear >= ip[calendarTime.Month]; ++calendarTime.Month)
            {
                dayOfYear -= ip[calendarTime.Month];
            }

            calendarTime.Day = (sbyte)(dayOfYear + 1);

            calendarAdditionalInfo.IsDaySavingTime = false;
            calendarAdditionalInfo.GmtOffset = gmtOffset;

            return 0;
        }

        private static ResultCode ToCalendarTimeInternal(in TimeZoneRule rules, long time, out CalendarTimeInternal calendarTime, out CalendarAdditionalInfo calendarAdditionalInfo)
        {
            calendarTime = new CalendarTimeInternal();
            calendarAdditionalInfo = new CalendarAdditionalInfo();

            ResultCode result;

            if ((rules.GoAhead && time < rules.Ats[0]) || (rules.GoBack && time > rules.Ats[rules.TimeCount - 1]))
            {
                long newTime = time;

                long seconds;
                long years;

                if (time < rules.Ats[0])
                {
                    seconds = rules.Ats[0] - time;
                }
                else
                {
                    seconds = time - rules.Ats[rules.TimeCount - 1];
                }

                seconds -= 1;

                years = (seconds / SecondsPerRepeat + 1) * YearsPerRepeat;
                seconds = years * AverageSecondsPerYear;

                if (time < rules.Ats[0])
                {
                    newTime += seconds;
                }
                else
                {
                    newTime -= seconds;
                }

                if (newTime < rules.Ats[0] && newTime > rules.Ats[rules.TimeCount - 1])
                {
                    return ResultCode.TimeNotFound;
                }

                result = ToCalendarTimeInternal(in rules, newTime, out calendarTime, out calendarAdditionalInfo);
                if (result != 0)
                {
                    return result;
                }

                if (time < rules.Ats[0])
                {
                    calendarTime.Year -= years;
                }
                else
                {
                    calendarTime.Year += years;
                }

                return ResultCode.Success;
            }

            int ttiIndex;

            if (rules.TimeCount == 0 || time < rules.Ats[0])
            {
                ttiIndex = rules.DefaultType;
            }
            else
            {
                int low = 1;
                int high = rules.TimeCount;

                while (low < high)
                {
                    int mid = (low + high) >> 1;

                    if (time < rules.Ats[mid])
                    {
                        high = mid;
                    }
                    else
                    {
                        low = mid + 1;
                    }
                }

                ttiIndex = rules.Types[low - 1];
            }

            result = CreateCalendarTime(time, rules.Ttis[ttiIndex].GmtOffset, out calendarTime, out calendarAdditionalInfo);

            if (result == 0)
            {
                calendarAdditionalInfo.IsDaySavingTime = rules.Ttis[ttiIndex].IsDaySavingTime;

                ReadOnlySpan<byte> timeZoneAbbreviation = rules.Chars[rules.Ttis[ttiIndex].AbbreviationListIndex..];

                int timeZoneSize = Math.Min(StringUtils.LengthCstr(timeZoneAbbreviation), 8);

                timeZoneAbbreviation[..timeZoneSize].CopyTo(calendarAdditionalInfo.TimezoneName.AsSpan());
            }

            return result;
        }

        private static ResultCode ToPosixTimeInternal(in TimeZoneRule rules, CalendarTimeInternal calendarTime, out long posixTime)
        {
            posixTime = 0;

            int hour = calendarTime.Hour;
            int minute = calendarTime.Minute;

            if (NormalizeOverflow32(ref hour, ref minute, MinutesPerHour))
            {
                return ResultCode.Overflow;
            }

            calendarTime.Minute = (sbyte)minute;

            int day = calendarTime.Day;
            if (NormalizeOverflow32(ref day, ref hour, HoursPerDays))
            {
                return ResultCode.Overflow;
            }

            calendarTime.Day = (sbyte)day;
            calendarTime.Hour = (sbyte)hour;

            long year = calendarTime.Year;
            long month = calendarTime.Month;

            if (NormalizeOverflow64(ref year, ref month, MonthsPerYear))
            {
                return ResultCode.Overflow;
            }

            calendarTime.Month = (sbyte)month;

            if (IncrementOverflow64(ref year, YearBase))
            {
                return ResultCode.Overflow;
            }

            while (day <= 0)
            {
                if (IncrementOverflow64(ref year, -1))
                {
                    return ResultCode.Overflow;
                }

                long li = year;

                if (1 < calendarTime.Month)
                {
                    li++;
                }

                day += _yearLengths[IsLeap((int)li)];
            }

            while (day > DaysPerLYear)
            {
                long li = year;

                if (1 < calendarTime.Month)
                {
                    li++;
                }

                day -= _yearLengths[IsLeap((int)li)];

                if (IncrementOverflow64(ref year, 1))
                {
                    return ResultCode.Overflow;
                }
            }

            while (true)
            {
                int i = _monthsLengths[IsLeap((int)year)][calendarTime.Month];

                if (day <= i)
                {
                    break;
                }

                day -= i;
                calendarTime.Month += 1;

                if (calendarTime.Month >= MonthsPerYear)
                {
                    calendarTime.Month = 0;
                    if (IncrementOverflow64(ref year, 1))
                    {
                        return ResultCode.Overflow;
                    }
                }
            }

            calendarTime.Day = (sbyte)day;

            if (IncrementOverflow64(ref year, -YearBase))
            {
                return ResultCode.Overflow;
            }

            calendarTime.Year = year;

            int savedSeconds;

            if (calendarTime.Second >= 0 && calendarTime.Second < SecondsPerMinute)
            {
                savedSeconds = 0;
            }
            else if (year + YearBase < EpochYear)
            {
                int second = calendarTime.Second;
                if (IncrementOverflow32(ref second, 1 - SecondsPerMinute))
                {
                    return ResultCode.Overflow;
                }

                savedSeconds = second;
                calendarTime.Second = 1 - SecondsPerMinute;
            }
            else
            {
                savedSeconds = calendarTime.Second;
                calendarTime.Second = 0;
            }

            long low = long.MinValue;
            long high = long.MaxValue;

            while (true)
            {
                long pivot = low / 2 + high / 2;

                if (pivot < low)
                {
                    pivot = low;
                }
                else if (pivot > high)
                {
                    pivot = high;
                }

                int direction;

                ResultCode result = ToCalendarTimeInternal(in rules, pivot, out CalendarTimeInternal candidateCalendarTime, out _);
                if (result != 0)
                {
                    if (pivot > 0)
                    {
                        direction = 1;
                    }
                    else
                    {
                        direction = -1;
                    }
                }
                else
                {
                    direction = candidateCalendarTime.CompareTo(calendarTime);
                }

                if (direction == 0)
                {
                    long timeResult = pivot + savedSeconds;

                    if ((timeResult < pivot) != (savedSeconds < 0))
                    {
                        return ResultCode.Overflow;
                    }

                    posixTime = timeResult;
                    break;
                }
                else
                {
                    if (pivot == low)
                    {
                        if (pivot == long.MaxValue)
                        {
                            return ResultCode.TimeNotFound;
                        }

                        pivot += 1;
                        low += 1;
                    }
                    else if (pivot == high)
                    {
                        if (pivot == long.MinValue)
                        {
                            return ResultCode.TimeNotFound;
                        }

                        pivot -= 1;
                        high -= 1;
                    }

                    if (low > high)
                    {
                        return ResultCode.TimeNotFound;
                    }

                    if (direction > 0)
                    {
                        high = pivot;
                    }
                    else
                    {
                        low = pivot;
                    }
                }
            }

            return ResultCode.Success;
        }

        internal static ResultCode ToCalendarTime(in TimeZoneRule rules, long time, out CalendarInfo calendar)
        {
            ResultCode result = ToCalendarTimeInternal(in rules, time, out CalendarTimeInternal calendarTime, out CalendarAdditionalInfo calendarAdditionalInfo);

            calendar = new CalendarInfo()
            {
                Time = new CalendarTime()
                {
                    Year = (short)calendarTime.Year,
                    // NOTE: Nintendo's month range is 1-12, internal range is 0-11.
                    Month = (sbyte)(calendarTime.Month + 1),
                    Day = calendarTime.Day,
                    Hour = calendarTime.Hour,
                    Minute = calendarTime.Minute,
                    Second = calendarTime.Second,
                },
                AdditionalInfo = calendarAdditionalInfo,
            };

            return result;
        }

        internal static ResultCode ToPosixTime(in TimeZoneRule rules, CalendarTime calendarTime, out long posixTime)
        {
            CalendarTimeInternal calendarTimeInternal = new()
            {
                Year = calendarTime.Year,
                // NOTE: Nintendo's month range is 1-12, internal range is 0-11.
                Month = (sbyte)(calendarTime.Month - 1),
                Day = calendarTime.Day,
                Hour = calendarTime.Hour,
                Minute = calendarTime.Minute,
                Second = calendarTime.Second,
            };

            return ToPosixTimeInternal(in rules, calendarTimeInternal, out posixTime);
        }
    }
}

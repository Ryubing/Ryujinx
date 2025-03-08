using MsgPack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Ava.Systems.PlayReport
{
    /// <summary>
    /// The base input data to a ValueFormatter delegate,
    /// and the matched <see cref="MessagePackObject"/> from the Play Report.
    /// </summary>
    public readonly struct Value
    {
        public Value(MessagePackObject packedValue)
        {
            PackedValue = packedValue;
        }

        /// <summary>
        /// The matched value from the Play Report.
        /// </summary>
        public MessagePackObject PackedValue { get; init; }

        /// <summary>
        /// Access the <see cref="PackedValue"/> as its underlying .NET type.<br/>
        /// 
        /// Does not seem to work well with comparing numeric types,
        /// so use XValue properties for that.
        /// </summary>
        public object BoxedValue => PackedValue.ToObject();

        public override string ToString()
        {
            object boxed = BoxedValue;
            return boxed == null
                ? "null"
                : boxed.ToString();
        }

        public static implicit operator Value(MessagePackObject matched) => new(matched);

        public static Value[] ConvertPackedObjects(IEnumerable<MessagePackObject> packObjects)
            => packObjects.Select(packObject => new Value(packObject)).ToArray();

        public static Dictionary<string, Value> ConvertPackedObjectMap(Dictionary<string, MessagePackObject> packObjects)
            => packObjects.ToDictionary(
                x => x.Key,
                x => new Value(x.Value)
            );

        #region AsX accessors

        public bool BooleanValue => PackedValue.AsBoolean();
        public byte ByteValue => PackedValue.AsByte();
        public sbyte SByteValue => PackedValue.AsSByte();
        public short ShortValue => PackedValue.AsInt16();
        public ushort UShortValue => PackedValue.AsUInt16();
        public int IntValue => PackedValue.AsInt32();
        public uint UIntValue => PackedValue.AsUInt32();
        public long LongValue => PackedValue.AsInt64();
        public ulong ULongValue => PackedValue.AsUInt64();
        public float FloatValue => PackedValue.AsSingle();
        public double DoubleValue => PackedValue.AsDouble();
        public string StringValue => PackedValue.AsString();
        public Span<byte> BinaryValue => PackedValue.AsBinary();

        #endregion
    }

    /// <summary>
    /// A potential formatted value returned by a ValueFormatter delegate.
    /// </summary>
    public readonly struct FormattedValue
    {
        /// <summary>
        /// Was any handler able to match anything in the Play Report?
        /// </summary>
        public bool Handled { get; private init; }

        /// <summary>
        /// Did the handler request the caller of the <see cref="Analyzer"/> to reset the existing value?
        /// </summary>
        public bool Reset { get; private init; }

        /// <summary>
        /// The formatted value, only present if <see cref="Handled"/> is true, and <see cref="Reset"/> is false.
        /// </summary>
        public string FormattedString { get; private init; }

        /// <summary>
        /// The intended path of execution for having a string to return: simply return the string.
        /// This implicit conversion will make the struct for you.<br/><br/>
        ///
        /// If the input is null, <see cref="Unhandled"/> is returned.
        /// </summary>
        /// <param name="formattedValue">The formatted string value.</param>
        /// <returns>The automatically constructed <see cref="FormattedValue"/> struct.</returns>
        public static implicit operator FormattedValue(string formattedValue)
            => formattedValue is not null
                ? new FormattedValue { Handled = true, FormattedString = formattedValue }
                : Unhandled;

        public override string ToString()
        {
            if (!Handled)
                return "<Unhandled>";

            if (Reset)
                return "<Reset>";

            return FormattedString;
        }

        /// <summary>
        /// Return this to tell the caller there is no value to return.
        /// </summary>
        public static readonly FormattedValue Unhandled = default;

        /// <summary>
        /// Return this to suggest the caller reset the value it's using the <see cref="Analyzer"/> for.
        /// </summary>
        public static readonly FormattedValue ForceReset = new() { Handled = true, Reset = true };

        /// <summary>
        /// A delegate singleton you can use to always return <see cref="ForceReset"/> in a <see cref="SingleValueFormatter"/>.
        /// </summary>
        public static readonly SingleValueFormatter SingleAlwaysResets = _ => ForceReset;

        /// <summary>
        /// A delegate singleton you can use to always return <see cref="ForceReset"/> in a <see cref="MultiValueFormatter"/>.
        /// </summary>
        public static readonly MultiValueFormatter MultiAlwaysResets = _ => ForceReset;
        
        /// <summary>
        /// A delegate singleton you can use to always return <see cref="ForceReset"/> in a <see cref="SparseMultiValueFormatter"/>.
        /// </summary>
        public static readonly SparseMultiValueFormatter SparseMultiAlwaysResets = _ => ForceReset;

        /// <summary>
        /// A delegate factory you can use to always return the specified
        /// <paramref name="formattedValue"/> in a <see cref="SingleValueFormatter"/>.
        /// </summary>
        /// <param name="formattedValue">The string to always return for this delegate instance.</param>
        public static SingleValueFormatter SingleAlwaysReturns(string formattedValue) => _ => formattedValue;
        
        /// <summary>
        /// A delegate factory you can use to always return the specified
        /// <paramref name="formattedValue"/> in a <see cref="MultiValueFormatter"/>.
        /// </summary>
        /// <param name="formattedValue">The string to always return for this delegate instance.</param>
        public static MultiValueFormatter MultiAlwaysReturns(string formattedValue) => _ => formattedValue;
        
        /// <summary>
        /// A delegate factory you can use to always return the specified
        /// <paramref name="formattedValue"/> in a <see cref="SparseMultiValueFormatter"/>.
        /// </summary>
        /// <param name="formattedValue">The string to always return for this delegate instance.</param>
        public static SparseMultiValueFormatter SparseMultiAlwaysReturns(string formattedValue) => _ => formattedValue;
    }
}

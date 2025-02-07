using MsgPack;
using Ryujinx.Ava.Utilities.AppLibrary;
using System;

namespace Ryujinx.Ava.Utilities.PlayReport
{
    /// <summary>
    /// The input data to a <see cref="ValueFormatter"/>,
    /// containing the currently running application's <see cref="ApplicationMetadata"/>,
    /// and the matched <see cref="MessagePackObject"/> from the Play Report.
    /// </summary>
    public class Value
    {
        /// <summary>
        /// The currently running application's <see cref="ApplicationMetadata"/>.
        /// </summary>
        public ApplicationMetadata Application { get; init; }

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
    /// A potential formatted value returned by a <see cref="ValueFormatter"/>.
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
        public static FormattedValue Unhandled => default;

        /// <summary>
        /// Return this to suggest the caller reset the value it's using the <see cref="Analyzer"/> for.
        /// </summary>
        public static FormattedValue ForceReset => new() { Handled = true, Reset = true };

        /// <summary>
        /// A delegate singleton you can use to always return <see cref="ForceReset"/> in a <see cref="ValueFormatter"/>.
        /// </summary>
        public static readonly ValueFormatter SingleAlwaysResets = _ => ForceReset;

        /// <summary>
        /// A delegate singleton you can use to always return <see cref="ForceReset"/> in a <see cref="MultiValueFormatter"/>.
        /// </summary>
        public static readonly MultiValueFormatter MultiAlwaysResets = _ => ForceReset;

        /// <summary>
        /// A delegate factory you can use to always return the specified
        /// <paramref name="formattedValue"/> in a <see cref="ValueFormatter"/>.
        /// </summary>
        /// <param name="formattedValue">The string to always return for this delegate instance.</param>
        public static ValueFormatter AlwaysReturns(string formattedValue) => _ => formattedValue;
    }
}

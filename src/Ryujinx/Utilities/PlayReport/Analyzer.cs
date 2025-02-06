using Gommon;
using MsgPack;
using Ryujinx.Ava.Utilities.AppLibrary;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Ryujinx.Ava.Utilities.PlayReport
{
    /// <summary>
    /// The entrypoint for the Play Report analysis system.
    /// </summary>
    public class Analyzer
    {
        private readonly List<GameSpec> _specs = [];

        /// <summary>
        /// Add an analysis spec matching a specific game by title ID, with the provided spec configuration.
        /// </summary>
        /// <param name="titleId">The ID of the game to listen to Play Reports in.</param>
        /// <param name="transform">The configuration function for the analysis spec.</param>
        /// <returns>The current <see cref="Analyzer"/>, for chaining convenience.</returns>
        public Analyzer AddSpec(string titleId, Func<GameSpec, GameSpec> transform)
        {
            Guard.Ensure(ulong.TryParse(titleId, NumberStyles.HexNumber, null, out _),
                $"Cannot use a non-hexadecimal string as the Title ID for a {nameof(GameSpec)}.");

            _specs.Add(transform(new GameSpec { TitleIds = [titleId] }));
            return this;
        }

        /// <summary>
        /// Add an analysis spec matching a specific game by title ID, with the provided spec configuration.
        /// </summary>
        /// <param name="titleId">The ID of the game to listen to Play Reports in.</param>
        /// <param name="transform">The configuration function for the analysis spec.</param>
        /// <returns>The current <see cref="Analyzer"/>, for chaining convenience.</returns>
        public Analyzer AddSpec(string titleId, Action<GameSpec> transform)
        {
            Guard.Ensure(ulong.TryParse(titleId, NumberStyles.HexNumber, null, out _),
                $"Cannot use a non-hexadecimal string as the Title ID for a {nameof(GameSpec)}.");

            _specs.Add(new GameSpec { TitleIds = [titleId] }.Apply(transform));
            return this;
        }

        /// <summary>
        /// Add an analysis spec matching a specific set of games by title IDs, with the provided spec configuration.
        /// </summary>
        /// <param name="titleIds">The IDs of the games to listen to Play Reports in.</param>
        /// <param name="transform">The configuration function for the analysis spec.</param>
        /// <returns>The current <see cref="Analyzer"/>, for chaining convenience.</returns>
        public Analyzer AddSpec(IEnumerable<string> titleIds,
            Func<GameSpec, GameSpec> transform)
        {
            string[] tids = titleIds.ToArray();
            Guard.Ensure(tids.All(x => ulong.TryParse(x, NumberStyles.HexNumber, null, out _)),
                $"Cannot use a non-hexadecimal string as the Title ID for a {nameof(GameSpec)}.");

            _specs.Add(transform(new GameSpec { TitleIds = [..tids] }));
            return this;
        }

        /// <summary>
        /// Add an analysis spec matching a specific set of games by title IDs, with the provided spec configuration.
        /// </summary>
        /// <param name="titleIds">The IDs of the games to listen to Play Reports in.</param>
        /// <param name="transform">The configuration function for the analysis spec.</param>
        /// <returns>The current <see cref="Analyzer"/>, for chaining convenience.</returns>
        public Analyzer AddSpec(IEnumerable<string> titleIds, Action<GameSpec> transform)
        {
            string[] tids = titleIds.ToArray();
            Guard.Ensure(tids.All(x => ulong.TryParse(x, NumberStyles.HexNumber, null, out _)),
                $"Cannot use a non-hexadecimal string as the Title ID for a {nameof(GameSpec)}.");

            _specs.Add(new GameSpec { TitleIds = [..tids] }.Apply(transform));
            return this;
        }

        
        /// <summary>
        /// Runs the configured <see cref="GameSpec.FormatterSpec"/> for the specified game title ID.
        /// </summary>
        /// <param name="runningGameId">The game currently running.</param>
        /// <param name="appMeta">The Application metadata information, including localized game name and play time information.</param>
        /// <param name="playReport">The Play Report received from HLE.</param>
        /// <returns>A struct representing a possible formatted value.</returns>
        public FormattedValue Format(
            string runningGameId,
            ApplicationMetadata appMeta,
            MessagePackObject playReport
        )
        {
            if (!playReport.IsDictionary)
                return FormattedValue.Unhandled;

            if (!_specs.TryGetFirst(s => runningGameId.EqualsAnyIgnoreCase(s.TitleIds), out GameSpec spec))
                return FormattedValue.Unhandled;

            foreach (GameSpec.FormatterSpec formatSpec in spec.SimpleValueFormatters.OrderBy(x => x.Priority))
            {
                if (!playReport.AsDictionary().TryGetValue(formatSpec.ReportKey, out MessagePackObject valuePackObject))
                    continue;

                return formatSpec.ValueFormatter(new Value
                {
                    Application = appMeta, PackedValue = valuePackObject
                });
            }
            
            foreach (GameSpec.MultiFormatterSpec formatSpec in spec.MultiValueFormatters.OrderBy(x => x.Priority))
            {
                List<MessagePackObject> packedObjects = [];
                foreach (var reportKey in formatSpec.ReportKeys)
                {
                    if (!playReport.AsDictionary().TryGetValue(reportKey, out MessagePackObject valuePackObject))
                        continue;
                    
                    packedObjects.Add(valuePackObject);
                }
                
                if (packedObjects.Count != formatSpec.ReportKeys.Length)
                    return FormattedValue.Unhandled;
                
                return formatSpec.ValueFormatter(packedObjects
                    .Select(packObject => new Value { Application = appMeta, PackedValue = packObject })
                    .ToArray());
            }

            return FormattedValue.Unhandled;
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
            public static readonly ValueFormatter AlwaysResets = _ => ForceReset;

            /// <summary>
            /// A delegate factory you can use to always return the specified
            /// <paramref name="formattedValue"/> in a <see cref="ValueFormatter"/>.
            /// </summary>
            /// <param name="formattedValue">The string to always return for this delegate instance.</param>
            public static ValueFormatter AlwaysReturns(string formattedValue) => _ => formattedValue;
        }
    }

    /// <summary>
    /// A mapping of title IDs to value formatter specs.
    ///
    /// <remarks>Generally speaking, use the <see cref="Analyzer"/>.AddSpec(...) methods instead of creating this class yourself.</remarks>
    /// </summary>
    public class GameSpec
    {
        public required string[] TitleIds { get; init; }
        public List<FormatterSpec> SimpleValueFormatters { get; } = [];
        public List<MultiFormatterSpec> MultiValueFormatters { get; } = [];

        /// <summary>
        /// Add a value formatter to the current <see cref="GameSpec"/>
        /// matching a specific key that could exist in a Play Report for the previously specified title IDs.
        /// </summary>
        /// <param name="reportKey">The key name to match.</param>
        /// <param name="valueFormatter">The function which can return a potential formatted value.</param>
        /// <returns>The current <see cref="GameSpec"/>, for chaining convenience.</returns>
        public GameSpec AddValueFormatter(string reportKey, ValueFormatter valueFormatter)
        {
            SimpleValueFormatters.Add(new FormatterSpec
            {
                Priority = SimpleValueFormatters.Count, ReportKey = reportKey, ValueFormatter = valueFormatter
            });
            return this;
        }

        /// <summary>
        /// Add a value formatter at a specific priority to the current <see cref="GameSpec"/>
        /// matching a specific key that could exist in a Play Report for the previously specified title IDs.
        /// </summary>
        /// <param name="priority">The resolution priority of this value formatter. Higher resolves sooner.</param>
        /// <param name="reportKey">The key name to match.</param>
        /// <param name="valueFormatter">The function which can return a potential formatted value.</param>
        /// <returns>The current <see cref="GameSpec"/>, for chaining convenience.</returns>
        public GameSpec AddValueFormatter(int priority, string reportKey,
            ValueFormatter valueFormatter)
        {
            SimpleValueFormatters.Add(new FormatterSpec
            {
                Priority = priority, ReportKey = reportKey, ValueFormatter = valueFormatter
            });
            return this;
        }
        
        /// <summary>
        /// Add a multi-value formatter to the current <see cref="GameSpec"/>
        /// matching a specific set of keys that could exist in a Play Report for the previously specified title IDs.
        /// </summary>
        /// <param name="reportKeys">The key names to match.</param>
        /// <param name="valueFormatter">The function which can format the values.</param>
        /// <returns>The current <see cref="GameSpec"/>, for chaining convenience.</returns>
        public GameSpec AddMultiValueFormatter(string[] reportKeys, MultiValueFormatter valueFormatter)
        {
            MultiValueFormatters.Add(new MultiFormatterSpec
            {
                Priority = SimpleValueFormatters.Count, ReportKeys = reportKeys, ValueFormatter = valueFormatter
            });
            return this;
        }
        
        /// <summary>
        /// Add a multi-value formatter at a specific priority to the current <see cref="GameSpec"/>
        /// matching a specific set of keys that could exist in a Play Report for the previously specified title IDs.
        /// </summary>
        /// <param name="priority">The resolution priority of this value formatter. Higher resolves sooner.</param>
        /// <param name="reportKeys">The key names to match.</param>
        /// <param name="valueFormatter">The function which can format the values.</param>
        /// <returns>The current <see cref="GameSpec"/>, for chaining convenience.</returns>
        public GameSpec AddMultiValueFormatter(int priority, string[] reportKeys,
            MultiValueFormatter valueFormatter)
        {
            MultiValueFormatters.Add(new MultiFormatterSpec
            {
                Priority = priority, ReportKeys = reportKeys, ValueFormatter = valueFormatter
            });
            return this;
        }

        /// <summary>
        /// A struct containing the data for a mapping of a key in a Play Report to a formatter for its potential value.
        /// </summary>
        public struct FormatterSpec
        {
            public required int Priority { get; init; }
            public required string ReportKey { get; init; }
            public ValueFormatter ValueFormatter { get; init; }
        }
        
        /// <summary>
        /// A struct containing the data for a mapping of an arbitrary key set in a Play Report to a formatter for their potential values.
        /// </summary>
        public struct MultiFormatterSpec
        {
            public required int Priority { get; init; }
            public required string[] ReportKeys { get; init; }
            public MultiValueFormatter ValueFormatter { get; init; }
        }
    }

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

        #region AsX accessors

        public bool BooleanValue => PackedValue.AsBoolean();
        public byte ByteValye => PackedValue.AsByte();
        public sbyte SByteValye => PackedValue.AsSByte();
        public short ShortValye => PackedValue.AsInt16();
        public ushort UShortValye => PackedValue.AsUInt16();
        public int IntValye => PackedValue.AsInt32();
        public uint UIntValye => PackedValue.AsUInt32();
        public long LongValye => PackedValue.AsInt64();
        public ulong ULongValye => PackedValue.AsUInt64();
        public float FloatValue => PackedValue.AsSingle();
        public double DoubleValue => PackedValue.AsDouble();
        public string StringValue => PackedValue.AsString();
        public Span<byte> BinaryValue => PackedValue.AsBinary();

        #endregion
    }

    /// <summary>
    /// The delegate type that powers single value formatters.<br/>
    /// Takes in the result value from the Play Report, and outputs:
    /// <br/>
    /// a formatted string,
    /// <br/>
    /// a signal that nothing was available to handle it,
    /// <br/>
    /// OR a signal to reset the value that the caller is using the <see cref="Analyzer"/> for. 
    /// </summary>
    public delegate Analyzer.FormattedValue ValueFormatter(Value value);
    
    /// <summary>
    /// The delegate type that powers multiple value formatters.<br/>
    /// Takes in the result value from the Play Report, and outputs:
    /// <br/>
    /// a formatted string,
    /// <br/>
    /// a signal that nothing was available to handle it,
    /// <br/>
    /// OR a signal to reset the value that the caller is using the <see cref="Analyzer"/> for. 
    /// </summary>
    public delegate Analyzer.FormattedValue MultiValueFormatter(Value[] value);
}

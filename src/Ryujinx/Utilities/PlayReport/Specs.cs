using FluentAvalonia.Core;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Ava.Utilities.PlayReport
{
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
        public List<SparseMultiFormatterSpec> SparseMultiValueFormatters { get; } = [];


        /// <summary>
        /// Add a value formatter to the current <see cref="GameSpec"/>
        /// matching a specific key that could exist in a Play Report for the previously specified title IDs.
        /// </summary>
        /// <param name="reportKey">The key name to match.</param>
        /// <param name="valueFormatter">The function which can return a potential formatted value.</param>
        /// <returns>The current <see cref="GameSpec"/>, for chaining convenience.</returns>
        public GameSpec AddValueFormatter(string reportKey, ValueFormatter valueFormatter)
            => AddValueFormatter(SimpleValueFormatters.Count, reportKey, valueFormatter);

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
                Priority = priority, ReportKey = reportKey, Formatter = valueFormatter
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
            => AddMultiValueFormatter(MultiValueFormatters.Count, reportKeys, valueFormatter);

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
                Priority = priority, ReportKeys = reportKeys, Formatter = valueFormatter
            });
            return this;
        }

        /// <summary>
        /// Add a multi-value formatter to the current <see cref="GameSpec"/>
        /// matching a specific set of keys that could exist in a Play Report for the previously specified title IDs.
        /// <br/><br/>
        /// The 'Sparse' multi-value formatters do not require every key to be present.
        /// If you need this requirement, use <see cref="AddMultiValueFormatter(string[], Ryujinx.Ava.Utilities.PlayReport.MultiValueFormatter)"/>.
        /// </summary>
        /// <param name="reportKeys">The key names to match.</param>
        /// <param name="valueFormatter">The function which can format the values.</param>
        /// <returns>The current <see cref="GameSpec"/>, for chaining convenience.</returns>
        public GameSpec AddSparseMultiValueFormatter(string[] reportKeys, SparseMultiValueFormatter valueFormatter)
            => AddSparseMultiValueFormatter(SparseMultiValueFormatters.Count, reportKeys, valueFormatter);

        /// <summary>
        /// Add a multi-value formatter at a specific priority to the current <see cref="GameSpec"/>
        /// matching a specific set of keys that could exist in a Play Report for the previously specified title IDs.
        /// <br/><br/>
        /// The 'Sparse' multi-value formatters do not require every key to be present.
        /// If you need this requirement, use <see cref="AddMultiValueFormatter(int, string[], Ryujinx.Ava.Utilities.PlayReport.MultiValueFormatter)"/>.
        /// </summary>
        /// <param name="priority">The resolution priority of this value formatter. Higher resolves sooner.</param>
        /// <param name="reportKeys">The key names to match.</param>
        /// <param name="valueFormatter">The function which can format the values.</param>
        /// <returns>The current <see cref="GameSpec"/>, for chaining convenience.</returns>
        public GameSpec AddSparseMultiValueFormatter(int priority, string[] reportKeys,
            SparseMultiValueFormatter valueFormatter)
        {
            SparseMultiValueFormatters.Add(new SparseMultiFormatterSpec
            {
                Priority = priority, ReportKeys = reportKeys, Formatter = valueFormatter
            });
            return this;
        }
    }

    /// <summary>
    /// A struct containing the data for a mapping of a key in a Play Report to a formatter for its potential value.
    /// </summary>
    public struct FormatterSpec
    {
        public required int Priority { get; init; }
        public required string ReportKey { get; init; }
        public ValueFormatter Formatter { get; init; }
    }

    /// <summary>
    /// A struct containing the data for a mapping of an arbitrary key set in a Play Report to a formatter for their potential values.
    /// </summary>
    public struct MultiFormatterSpec
    {
        public required int Priority { get; init; }
        public required string[] ReportKeys { get; init; }
        public MultiValueFormatter Formatter { get; init; }
    }

    /// <summary>
    /// A struct containing the data for a mapping of an arbitrary key set in a Play Report to a formatter for their sparsely populated potential values.
    /// </summary>
    public struct SparseMultiFormatterSpec
    {
        public required int Priority { get; init; }
        public required string[] ReportKeys { get; init; }
        public SparseMultiValueFormatter Formatter { get; init; }
    }
}

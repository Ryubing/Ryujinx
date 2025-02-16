using MsgPack;
using Ryujinx.Ava.Utilities.AppLibrary;
using System;
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
        public static GameSpec Create(string requiredTitleId, params IEnumerable<string> otherTitleIds)
            => new() { TitleIds = otherTitleIds.Prepend(requiredTitleId).ToArray() };

        public static GameSpec Create(IEnumerable<string> titleIds)
            => new() { TitleIds = titleIds.ToArray() };

        private int _lastPriority;

        public required string[] TitleIds { get; init; }

        public const string DefaultDescription = "Formats the details on your Discord presence based on logged data from the game.";

        private string _valueDescription;

        public string Description => _valueDescription ?? DefaultDescription;

        public GameSpec WithDescription(string description)
        {
            _valueDescription = description != null
                ? $"Formats the details on your Discord presence {description}"
                : null;
            return this;
        }
        
        public List<FormatterSpecBase> ValueFormatters { get; } = [];


        /// <summary>
        /// Add a value formatter to the current <see cref="GameSpec"/>
        /// matching a specific key that could exist in a Play Report for the previously specified title IDs.
        /// </summary>
        /// <param name="reportKey">The key name to match.</param>
        /// <param name="valueFormatter">The function which can return a potential formatted value.</param>
        /// <returns>The current <see cref="GameSpec"/>, for chaining convenience.</returns>
        public GameSpec AddValueFormatter(
            string reportKey,
            SingleValueFormatter valueFormatter
        ) => AddValueFormatter(_lastPriority++, reportKey, valueFormatter);

        /// <summary>
        /// Add a value formatter at a specific priority to the current <see cref="GameSpec"/>
        /// matching a specific key that could exist in a Play Report for the previously specified title IDs.
        /// </summary>
        /// <param name="priority">The resolution priority of this value formatter. Higher resolves sooner.</param>
        /// <param name="reportKey">The key name to match.</param>
        /// <param name="valueFormatter">The function which can return a potential formatted value.</param>
        /// <returns>The current <see cref="GameSpec"/>, for chaining convenience.</returns>
        public GameSpec AddValueFormatter(
            int priority,
            string reportKey,
            SingleValueFormatter valueFormatter
        ) => AddValueFormatter(new FormatterSpec
        {
            Priority = priority, ReportKeys = [reportKey], Formatter = valueFormatter
        });

        /// <summary>
        /// Add a multi-value formatter to the current <see cref="GameSpec"/>
        /// matching a specific set of keys that could exist in a Play Report for the previously specified title IDs.
        /// </summary>
        /// <param name="reportKeys">The key names to match.</param>
        /// <param name="valueFormatter">The function which can format the values.</param>
        /// <returns>The current <see cref="GameSpec"/>, for chaining convenience.</returns>
        public GameSpec AddMultiValueFormatter(
            string[] reportKeys,
            MultiValueFormatter valueFormatter
        ) => AddMultiValueFormatter(_lastPriority++, reportKeys, valueFormatter);

        /// <summary>
        /// Add a multi-value formatter at a specific priority to the current <see cref="GameSpec"/>
        /// matching a specific set of keys that could exist in a Play Report for the previously specified title IDs.
        /// </summary>
        /// <param name="priority">The resolution priority of this value formatter. Higher resolves sooner.</param>
        /// <param name="reportKeys">The key names to match.</param>
        /// <param name="valueFormatter">The function which can format the values.</param>
        /// <returns>The current <see cref="GameSpec"/>, for chaining convenience.</returns>
        public GameSpec AddMultiValueFormatter(
            int priority,
            string[] reportKeys,
            MultiValueFormatter valueFormatter
        ) => AddValueFormatter(new MultiFormatterSpec
        {
            Priority = priority, ReportKeys = reportKeys, Formatter = valueFormatter
        });

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
        public GameSpec AddSparseMultiValueFormatter(
            string[] reportKeys,
            SparseMultiValueFormatter valueFormatter
        ) => AddSparseMultiValueFormatter(_lastPriority++, reportKeys, valueFormatter);

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
        public GameSpec AddSparseMultiValueFormatter(
            int priority,
            string[] reportKeys,
            SparseMultiValueFormatter valueFormatter
        ) => AddValueFormatter(new SparseMultiFormatterSpec
        {
            Priority = priority, ReportKeys = reportKeys, Formatter = valueFormatter
        });

        private GameSpec AddValueFormatter<T>(T formatterSpec) where T : FormatterSpecBase
        {
            ValueFormatters.Add(formatterSpec);
            return this;
        }
    }

    /// <summary>
    /// A struct containing the data for a mapping of a key in a Play Report to a formatter for its potential value.
    /// </summary>
    public class FormatterSpec : FormatterSpecBase
    {
        public override bool GetData(Horizon.Prepo.Types.PlayReport playReport, out object result)
        {
            if (!playReport.ReportData.AsDictionary().TryGetValue(ReportKeys[0], out MessagePackObject valuePackObject))
            {
                result = null;
                return false;
            }

            result = valuePackObject;
            return true;
        }
    }

    /// <summary>
    /// A struct containing the data for a mapping of an arbitrary key set in a Play Report to a formatter for their potential values.
    /// </summary>
    public class MultiFormatterSpec : FormatterSpecBase
    {
        public override bool GetData(Horizon.Prepo.Types.PlayReport playReport, out object result)
        {
            List<MessagePackObject> packedObjects = [];
            foreach (string reportKey in ReportKeys)
            {
                if (!playReport.ReportData.AsDictionary().TryGetValue(reportKey, out MessagePackObject valuePackObject))
                {
                    result = null;
                    return false;
                }

                packedObjects.Add(valuePackObject);
            }

            result = packedObjects;
            return true;
        }
    }

    /// <summary>
    /// A struct containing the data for a mapping of an arbitrary key set in a Play Report to a formatter for their sparsely populated potential values.
    /// </summary>
    public class SparseMultiFormatterSpec : FormatterSpecBase
    {
        public override bool GetData(Horizon.Prepo.Types.PlayReport playReport, out object result)
        {
            Dictionary<string, MessagePackObject> packedObjects = [];
            foreach (string reportKey in ReportKeys)
            {
                if (!playReport.ReportData.AsDictionary().TryGetValue(reportKey, out MessagePackObject valuePackObject))
                    continue;

                packedObjects.Add(reportKey, valuePackObject);
            }

            result = packedObjects;
            return true;
        }
    }

    public abstract class FormatterSpecBase
    {
        public abstract bool GetData(Horizon.Prepo.Types.PlayReport playReport, out object data);

        public int Priority { get; init; }
        public string[] ReportKeys { get; init; }
        public Delegate Formatter { get; init; }

        public bool TryFormat(ApplicationMetadata appMeta, Horizon.Prepo.Types.PlayReport playReport,
            out FormattedValue formattedValue)
        {
            formattedValue = default;
            if (!GetData(playReport, out object data))
                return false;

            if (data is FormattedValue fv)
            {
                formattedValue = fv;
                return true;
            }

            switch (Formatter)
            {
                case SingleValueFormatter svf when data is MessagePackObject match:
                    formattedValue = svf(
                        new SingleValue(match) { Application = appMeta, PlayReport = playReport }
                    );
                    return true;
                case MultiValueFormatter mvf when data is List<MessagePackObject> matches:
                    formattedValue = mvf(
                        new MultiValue(matches) { Application = appMeta, PlayReport = playReport }
                    );
                    return true;
                case SparseMultiValueFormatter smvf when data is Dictionary<string, MessagePackObject> sparseMatches:
                    formattedValue = smvf(
                        new SparseMultiValue(sparseMatches) { Application = appMeta, PlayReport = playReport }
                    );
                    return true;
                default:
                    throw new InvalidOperationException("Formatter delegate is not of a known type!");
            }
        }
    }
}

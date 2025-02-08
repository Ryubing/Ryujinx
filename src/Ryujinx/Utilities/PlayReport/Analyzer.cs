using Gommon;
using MsgPack;
using Ryujinx.Ava.Utilities.AppLibrary;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public string[] TitleIds => Specs.SelectMany(x => x.TitleIds).ToArray();

        public IReadOnlyList<GameSpec> Specs => new ReadOnlyCollection<GameSpec>(_specs);

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

            foreach (FormatterSpec formatSpec in spec.SimpleValueFormatters.OrderBy(x => x.Priority))
            {
                if (!playReport.AsDictionary().TryGetValue(formatSpec.ReportKey, out MessagePackObject valuePackObject))
                    continue;

                return formatSpec.Formatter(new Value { Application = appMeta, PackedValue = valuePackObject });
            }

            foreach (MultiFormatterSpec formatSpec in spec.MultiValueFormatters.OrderBy(x => x.Priority))
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

                return formatSpec.Formatter(packedObjects
                    .Select(packObject => new Value { Application = appMeta, PackedValue = packObject })
                    .ToArray());
            }

            foreach (SparseMultiFormatterSpec formatSpec in spec.SparseMultiValueFormatters.OrderBy(x => x.Priority))
            {
                Dictionary<string, Value> packedObjects = [];
                foreach (var reportKey in formatSpec.ReportKeys)
                {
                    if (!playReport.AsDictionary().TryGetValue(reportKey, out MessagePackObject valuePackObject))
                        continue;

                    packedObjects.Add(reportKey, new Value { Application = appMeta, PackedValue = valuePackObject });
                }

                return formatSpec.Formatter(packedObjects);
            }

            return FormattedValue.Unhandled;
        }
    }
}

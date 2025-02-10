using Gommon;
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

            return AddSpec(transform(GameSpec.Create(titleId)));
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

            return AddSpec(GameSpec.Create(titleId).Apply(transform));
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

            return AddSpec(transform(GameSpec.Create(tids)));
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

            return AddSpec(GameSpec.Create(tids).Apply(transform));
        }
        
        /// <summary>
        /// Add an analysis spec matching a specific game by title ID, with the provided pre-configured spec.
        /// </summary>
        /// <param name="spec">The <see cref="GameSpec"/> to add.</param>
        /// <returns>The current <see cref="Analyzer"/>, for chaining convenience.</returns>
        public Analyzer AddSpec(GameSpec spec)
        {
            _specs.Add(spec);
            return this;
        }


        /// <summary>
        /// Runs the configured <see cref="FormatterSpec"/> for the specified game title ID.
        /// </summary>
        /// <param name="runningGameId">The game currently running.</param>
        /// <param name="appMeta">The Application metadata information, including localized game name and play time information.</param>
        /// <param name="playReport">The Play Report received from HLE.</param>
        /// <returns>A struct representing a possible formatted value.</returns>
        public FormattedValue Format(
            string runningGameId,
            ApplicationMetadata appMeta,
            Horizon.Prepo.Types.PlayReport playReport
        )
        {
            if (!playReport.ReportData.IsDictionary)
                return FormattedValue.Unhandled;

            if (!_specs.TryGetFirst(s => runningGameId.EqualsAnyIgnoreCase(s.TitleIds), out GameSpec spec))
                return FormattedValue.Unhandled;

            foreach (FormatterSpecBase formatSpec in spec.ValueFormatters.OrderBy(x => x.Priority))
            {
                if (!formatSpec.Format(appMeta, playReport, out FormattedValue value))
                    continue;

                return value;
            }

            return FormattedValue.Unhandled;
        }
    }
}

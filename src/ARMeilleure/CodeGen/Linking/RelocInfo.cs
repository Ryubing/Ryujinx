using System;

namespace ARMeilleure.CodeGen.Linking
{
    /// <summary>
    /// Represents relocation information about a <see cref="CompiledFunction"/>.
    /// </summary>
    readonly struct RelocInfo
    {
        /// <summary>
        /// Gets an empty <see cref="RelocInfo"/>.
        /// </summary>
        public static RelocInfo Empty { get; } = new(null);

        private readonly RelocEntry[] _entries;

        /// <summary>
        /// Gets the set of <see cref="RelocEntry"/>.
        /// </summary>
        public ReadOnlySpan<RelocEntry> Entries => _entries;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelocInfo"/> struct with the specified set of
        /// <see cref="RelocEntry"/>.
        /// </summary>
        /// <param name="entries">Set of <see cref="RelocInfo"/> to use</param>
        public RelocInfo(RelocEntry[] entries)
        {
            _entries = entries;
        }
    }
}

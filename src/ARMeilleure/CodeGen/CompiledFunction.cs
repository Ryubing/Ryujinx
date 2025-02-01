using ARMeilleure.CodeGen.Linking;
using ARMeilleure.CodeGen.Unwinding;
using ARMeilleure.Translation.Cache;
using System.Runtime.InteropServices;

namespace ARMeilleure.CodeGen
{
    /// <summary>
    /// Represents a compiled function.
    /// </summary>
    readonly struct CompiledFunction
    {
        /// <summary>
        /// Gets the machine code of the <see cref="CompiledFunction"/>.
        /// </summary>
        public byte[] Code { get; }

        /// <summary>
        /// Gets the <see cref="Unwinding.UnwindInfo"/> of the <see cref="CompiledFunction"/>.
        /// </summary>
        public UnwindInfo UnwindInfo { get; }

        /// <summary>
        /// Gets the <see cref="Linking.RelocInfo"/> of the <see cref="CompiledFunction"/>.
        /// </summary>
        public RelocInfo RelocInfo { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompiledFunction"/> struct with the specified machine code,
        /// unwind info and relocation info.
        /// </summary>
        /// <param name="code">Machine code</param>
        /// <param name="unwindInfo">Unwind info</param>
        /// <param name="relocInfo">Relocation info</param>
        internal CompiledFunction(byte[] code, UnwindInfo unwindInfo, RelocInfo relocInfo)
        {
            Code = code;
            UnwindInfo = unwindInfo;
            RelocInfo = relocInfo;
        }

        /// <summary>
        /// Maps the <see cref="CompiledFunction"/> onto the <see cref="JitCache"/> and returns a delegate of type
        /// <typeparamref name="T"/> pointing to the mapped function.
        /// </summary>
        /// <typeparam name="T">Type of delegate</typeparam>
        /// <returns>A delegate of type <typeparamref name="T"/> pointing to the mapped function</returns>
        public T Map<T>()
        {
            return MapWithPointer<T>(out _);
        }

        /// <summary>
        /// Maps the <see cref="CompiledFunction"/> onto the <see cref="JitCache"/> and returns a delegate of type
        /// <typeparamref name="T"/> pointing to the mapped function.
        /// </summary>
        /// <typeparam name="T">Type of delegate</typeparam>
        /// <param name="codePointer">Pointer to the function code in memory</param>
        /// <returns>A delegate of type <typeparamref name="T"/> pointing to the mapped function</returns>
        public T MapWithPointer<T>(out nint codePointer)
        {
            codePointer = JitCache.Map(this);

            return Marshal.GetDelegateForFunctionPointer<T>(codePointer);
        }
    }
}

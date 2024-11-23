using System;

namespace Ryujinx.Cpu
{
    /// <summary>
    /// CPU context interface.
    /// </summary>
    public interface ICpuContext : IDisposable
    {
        /// <summary>
        /// Creates a new execution context that will store thread CPU register state when executing guest code.
        /// </summary>
        /// <param name="exceptionCallbacks">Optional functions to be called when the CPU receives an interrupt</param>
        /// <returns>Execution context</returns>
        IExecutionContext CreateExecutionContext(ExceptionCallbacks exceptionCallbacks);

        /// <summary>
        /// Starts executing code at a specified entry point address.
        /// </summary>
        /// <remarks>
        /// This function only returns when the execution is stopped, by calling <see cref="IExecutionContext.StopRunning"/>.
        /// </remarks>
        /// <param name="context">Execution context to be used for this run</param>
        /// <param name="address">Entry point address</param>
        void Execute(IExecutionContext context, ulong address);

        /// <summary>
        /// Invalidates the instruction cache for a given memory region.
        /// </summary>
        /// <remarks>
        /// This should be called if code is modified to make the CPU emulator aware of the modifications,
        /// otherwise it might run stale code which will lead to errors and crashes.
        /// Calling this function is not necessary if the code memory was modified by guest code,
        /// as the expectation is that it will do it on its own using the appropriate cache invalidation instructions,
        /// except on Arm32 where those instructions can't be used in unprivileged mode.
        /// </remarks>
        /// <param name="address">Address of the region to be invalidated</param>
        /// <param name="size">Size of the region to be invalidated</param>
        void InvalidateCacheRegion(ulong address, ulong size);

        /// <summary>
        /// Loads cached code from disk for a given application.
        /// </summary>
        /// <remarks>
        /// If the execution engine is recompiling guest code, this can be used to load cached code from disk.
        /// </remarks>
        /// <param name="titleIdText">Title ID of the application in padded hex form</param>
        /// <param name="displayVersion">Version of the application</param>
        /// <param name="enabled">True if the cache should be loaded from disk if it exists, false otherwise</param>
        /// <returns>Disk cache load progress reporter and manager</returns>
        IDiskCacheLoadState LoadDiskCache(string titleIdText, string displayVersion, bool enabled, string cacheSelector);

        /// <summary>
        /// Indicates that code has been loaded into guest memory, and that it might be executed in the future.
        /// </summary>
        /// <remarks>
        /// Some execution engines might use this information to cache recompiled code on disk or to ensure it can be executed.
        /// </remarks>
        /// <param name="address">CPU virtual address where the code starts</param>
        /// <param name="size">Size of the code range in bytes</param>
        void PrepareCodeRange(ulong address, ulong size);
    }
}

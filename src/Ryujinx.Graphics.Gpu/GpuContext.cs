using Ryujinx.Common;
using Ryujinx.Common.Configuration;
using Ryujinx.Graphics.Device;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Gpu.Engine.GPFifo;
using Ryujinx.Graphics.Gpu.Memory;
using Ryujinx.Graphics.Gpu.Shader;
using Ryujinx.Graphics.Gpu.Synchronization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Ryujinx.Graphics.Gpu
{
    /// <summary>
    /// GPU emulation context.
    /// </summary>
    public sealed class GpuContext : IDisposable
    {
        private const int NsToTicksFractionNumerator = 384;
        private const int NsToTicksFractionDenominator = 625;

        /// <summary>
        /// Event signaled when the host emulation context is ready to be used by the gpu context.
        /// </summary>
        public ManualResetEvent HostInitalized { get; }

        /// <summary>
        /// Host renderer.
        /// </summary>
        public IRenderer Renderer { get; }

        /// <summary>
        /// GPU General Purpose FIFO queue.
        /// </summary>
        public GPFifoDevice GPFifo { get; }

        /// <summary>
        /// GPU synchronization manager.
        /// </summary>
        public SynchronizationManager Synchronization { get; }

        /// <summary>
        /// Presentation window.
        /// </summary>
        public Window Window { get; }

        /// <summary>
        /// Internal sequence number, used to avoid needless resource data updates
        /// in the middle of a command buffer before synchronizations.
        /// </summary>
        internal int SequenceNumber { get; private set; }

        /// <summary>
        /// Internal sync number, used to denote points at which host synchronization can be requested.
        /// </summary>
        internal ulong SyncNumber { get; private set; }

        /// <summary>
        /// Actions to be performed when a CPU waiting syncpoint or barrier is triggered.
        /// If there are more than 0 items when this happens, a host sync object will be generated for the given <see cref="SyncNumber"/>,
        /// and the SyncNumber will be incremented.
        /// </summary>
        internal List<ISyncActionHandler> SyncActions { get; }

        /// <summary>
        /// Actions to be performed when a CPU waiting syncpoint is triggered.
        /// If there are more than 0 items when this happens, a host sync object will be generated for the given <see cref="SyncNumber"/>,
        /// and the SyncNumber will be incremented.
        /// </summary>
        internal List<ISyncActionHandler> SyncpointActions { get; }

        /// <summary>
        /// Buffer migrations that are currently in-flight. These are checked whenever sync is created to determine if buffer migration
        /// copies have completed on the GPU, and their data can be freed.
        /// </summary>
        internal List<BufferMigration> BufferMigrations { get; }

        /// <summary>
        /// Queue with deferred actions that must run on the render thread.
        /// </summary>
        internal Queue<Action> DeferredActions { get; }

        /// <summary>
        /// Registry with physical memories that can be used with this GPU context, keyed by owner process ID.
        /// </summary>
        internal ConcurrentDictionary<ulong, PhysicalMemory> PhysicalMemoryRegistry { get; }

        /// <summary>
        /// Support buffer updater.
        /// </summary>
        internal SupportBufferUpdater SupportBufferUpdater { get; }
        
        /// <summary>
        /// Enabled dirty hacks.
        /// Used for workarounds to emulator bugs we can't fix/don't know how to fix yet.
        /// </summary>
        internal DirtyHacks DirtyHacks { get; }
        

        /// <summary>
        /// Host hardware capabilities.
        /// </summary>
        internal Capabilities Capabilities;

        /// <summary>
        /// Event for signalling shader cache loading progress.
        /// </summary>
        public event Action<ShaderCacheState, int, int> ShaderCacheStateChanged;

        private Thread _gpuThread;
        private bool _pendingSync;

        private long _modifiedSequence;
        private readonly ulong _firstTimestamp;

        private readonly ManualResetEvent _gpuReadyEvent;

        /// <summary>
        /// Creates a new instance of the GPU emulation context.
        /// </summary>
        /// <param name="renderer">Host renderer</param>
        public GpuContext(IRenderer renderer, DirtyHacks hacks)
        {
            Renderer = renderer;

            GPFifo = new GPFifoDevice(this);

            Synchronization = new SynchronizationManager();

            Window = new Window(this);

            HostInitalized = new ManualResetEvent(false);
            _gpuReadyEvent = new ManualResetEvent(false);

            SyncActions = [];
            SyncpointActions = [];
            BufferMigrations = [];

            DeferredActions = new Queue<Action>();

            PhysicalMemoryRegistry = new ConcurrentDictionary<ulong, PhysicalMemory>();

            SupportBufferUpdater = new SupportBufferUpdater(renderer);

            DirtyHacks = hacks;

            _firstTimestamp = ConvertNanosecondsToTicks((ulong)PerformanceCounter.ElapsedNanoseconds);
        }

        /// <summary>
        /// Creates a new GPU channel.
        /// </summary>
        /// <returns>The GPU channel</returns>
        public GpuChannel CreateChannel()
        {
            return new GpuChannel(this);
        }

        /// <summary>
        /// Creates a new GPU memory manager.
        /// </summary>
        /// <param name="pid">ID of the process that owns the memory manager</param>
        /// <param name="cpuMemorySize">The amount of physical CPU Memory Avaiable on the device.</param>
        /// <returns>The memory manager</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="pid"/> is invalid</exception>
        public MemoryManager CreateMemoryManager(ulong pid, ulong cpuMemorySize)
        {
            if (!PhysicalMemoryRegistry.TryGetValue(pid, out PhysicalMemory physicalMemory))
            {
                throw new ArgumentException("The PID is invalid or the process was not registered", nameof(pid));
            }

            return new MemoryManager(physicalMemory, cpuMemorySize);
        }

        /// <summary>
        /// Creates a new device memory manager.
        /// </summary>
        /// <param name="pid">ID of the process that owns the memory manager</param>
        /// <returns>The memory manager</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="pid"/> is invalid</exception>
        public DeviceMemoryManager CreateDeviceMemoryManager(ulong pid)
        {
            if (!PhysicalMemoryRegistry.TryGetValue(pid, out PhysicalMemory physicalMemory))
            {
                throw new ArgumentException("The PID is invalid or the process was not registered", nameof(pid));
            }

            return physicalMemory.CreateDeviceMemoryManager();
        }

        /// <summary>
        /// Registers virtual memory used by a process for GPU memory access, caching and read/write tracking.
        /// </summary>
        /// <param name="pid">ID of the process that owns <paramref name="cpuMemory"/></param>
        /// <param name="cpuMemory">Virtual memory owned by the process</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="pid"/> was already registered</exception>
        public void RegisterProcess(ulong pid, Cpu.IVirtualMemoryManagerTracked cpuMemory)
        {
            PhysicalMemory physicalMemory = new(this, cpuMemory);
            if (!PhysicalMemoryRegistry.TryAdd(pid, physicalMemory))
            {
                throw new ArgumentException("The PID was already registered", nameof(pid));
            }

            physicalMemory.ShaderCache.ShaderCacheStateChanged += ShaderCacheStateUpdate;
        }

        /// <summary>
        /// Unregisters a process, indicating that its memory will no longer be used, and that caches can be freed.
        /// </summary>
        /// <param name="pid">ID of the process</param>
        public void UnregisterProcess(ulong pid)
        {
            if (PhysicalMemoryRegistry.TryRemove(pid, out PhysicalMemory physicalMemory))
            {
                physicalMemory.ShaderCache.ShaderCacheStateChanged -= ShaderCacheStateUpdate;
                physicalMemory.Dispose();
            }
        }

        /// <summary>
        /// Converts a nanoseconds timestamp value to Maxwell time ticks.
        /// </summary>
        /// <remarks>
        /// The frequency is 614400000 Hz.
        /// </remarks>
        /// <param name="nanoseconds">Timestamp in nanoseconds</param>
        /// <returns>Maxwell ticks</returns>
        private static ulong ConvertNanosecondsToTicks(ulong nanoseconds)
        {
            // We need to divide first to avoid overflows.
            // We fix up the result later by calculating the difference and adding
            // that to the result.
            ulong divided = nanoseconds / NsToTicksFractionDenominator;

            ulong rounded = divided * NsToTicksFractionDenominator;

            ulong errorBias = (nanoseconds - rounded) * NsToTicksFractionNumerator / NsToTicksFractionDenominator;

            return divided * NsToTicksFractionNumerator + errorBias;
        }

        /// <summary>
        /// Gets a sequence number for resource modification ordering. This increments on each call.
        /// </summary>
        /// <returns>A sequence number for resource modification ordering</returns>
        internal long GetModifiedSequence()
        {
            return _modifiedSequence++;
        }

        /// <summary>
        /// Gets the value of the GPU timer.
        /// </summary>
        /// <returns>The current GPU timestamp</returns>
        internal ulong GetTimestamp()
        {
            // Guest timestamp will start at 0, instead of host value.
            ulong ticks = ConvertNanosecondsToTicks((ulong)PerformanceCounter.ElapsedNanoseconds) - _firstTimestamp;

            if (GraphicsConfig.FastGpuTime)
            {
                // Divide by some amount to report time as if operations were performed faster than they really are.
                // This can prevent some games from switching to a lower resolution because rendering is too slow.
                ticks /= 256;
            }

            return ticks;
        }

        /// <summary>
        /// Shader cache state update handler.
        /// </summary>
        /// <param name="state">Current state of the shader cache load process</param>
        /// <param name="current">Number of the current shader being processed</param>
        /// <param name="total">Total number of shaders to process</param>
        private void ShaderCacheStateUpdate(ShaderCacheState state, int current, int total)
        {
            ShaderCacheStateChanged?.Invoke(state, current, total);
        }

        /// <summary>
        /// Initialize the GPU shader cache.
        /// </summary>
        public void InitializeShaderCache(CancellationToken cancellationToken)
        {
            HostInitalized.WaitOne();

            foreach (PhysicalMemory physicalMemory in PhysicalMemoryRegistry.Values)
            {
                physicalMemory.ShaderCache.Initialize(cancellationToken);
            }

            _gpuReadyEvent.Set();
        }

        /// <summary>
        /// Waits until the GPU is ready to receive commands.
        /// </summary>
        public void WaitUntilGpuReady()
        {
            _gpuReadyEvent.WaitOne();
        }

        /// <summary>
        /// Sets the current thread as the main GPU thread.
        /// </summary>
        public void SetGpuThread()
        {
            _gpuThread = Thread.CurrentThread;

            Capabilities = Renderer.GetCapabilities();
        }

        /// <summary>
        /// Checks if the current thread is the GPU thread.
        /// </summary>
        /// <returns>True if the thread is the GPU thread, false otherwise</returns>
        public bool IsGpuThread()
        {
            return _gpuThread == Thread.CurrentThread;
        }

        /// <summary>
        /// Processes the queue of shaders that must save their binaries to the disk cache.
        /// </summary>
        public void ProcessShaderCacheQueue()
        {
            foreach (PhysicalMemory physicalMemory in PhysicalMemoryRegistry.Values)
            {
                physicalMemory.ShaderCache.ProcessShaderCacheQueue();
            }
        }

        /// <summary>
        /// Advances internal sequence number.
        /// This forces the update of any modified GPU resource.
        /// </summary>
        internal void AdvanceSequence()
        {
            SequenceNumber++;
        }

        /// <summary>
        /// Registers a buffer migration. These are checked to see if they can be disposed when the sync number increases,
        /// and the migration copy has completed.
        /// </summary>
        /// <param name="migration">The buffer migration</param>
        internal void RegisterBufferMigration(BufferMigration migration)
        {
            BufferMigrations.Add(migration);
            _pendingSync = true;
        }

        /// <summary>
        /// Registers an action to be performed the next time a syncpoint is incremented.
        /// This will also ensure a host sync object is created, and <see cref="SyncNumber"/> is incremented.
        /// </summary>
        /// <param name="action">The resource with action to be performed on sync object creation</param>
        /// <param name="syncpointOnly">True if the sync action should only run when syncpoints are incremented</param>
        internal void RegisterSyncAction(ISyncActionHandler action, bool syncpointOnly = false)
        {
            if (syncpointOnly)
            {
                SyncpointActions.Add(action);
            }
            else
            {
                SyncActions.Add(action);
                _pendingSync = true;
            }
        }

        /// <summary>
        /// Creates a host sync object if there are any pending sync actions. The actions will then be called.
        /// If no actions are present, a host sync object is not created.
        /// </summary>
        /// <param name="flags">Modifiers for how host sync should be created</param>
        internal void CreateHostSyncIfNeeded(HostSyncFlags flags)
        {
            bool syncpoint = flags.HasFlag(HostSyncFlags.Syncpoint);
            bool strict = flags.HasFlag(HostSyncFlags.Strict);
            bool force = flags.HasFlag(HostSyncFlags.Force);

            if (BufferMigrations.Count > 0)
            {
                ulong currentSyncNumber = Renderer.GetCurrentSync();

                for (int i = 0; i < BufferMigrations.Count; i++)
                {
                    BufferMigration migration = BufferMigrations[i];
                    long diff = (long)(currentSyncNumber - migration.SyncNumber);

                    if (diff >= 0)
                    {
                        migration.Dispose();
                        BufferMigrations.RemoveAt(i--);
                    }
                }
            }

            if (force || _pendingSync || (syncpoint && SyncpointActions.Count > 0))
            {
                foreach (ISyncActionHandler action in SyncActions)
                {
                    action.SyncPreAction(syncpoint);
                }

                foreach (ISyncActionHandler action in SyncpointActions)
                {
                    action.SyncPreAction(syncpoint);
                }

                Renderer.CreateSync(SyncNumber, strict);

                SyncNumber++;

                SyncActions.RemoveAll(action => action.SyncAction(syncpoint));
                SyncpointActions.RemoveAll(action => action.SyncAction(syncpoint));
            }

            _pendingSync = false;
        }

        /// <summary>
        /// Performs deferred actions.
        /// This is useful for actions that must run on the render thread, such as resource disposal.
        /// </summary>
        internal void RunDeferredActions()
        {
            while (DeferredActions.TryDequeue(out Action action))
            {
                action();
            }
        }

        /// <summary>
        /// Disposes all GPU resources currently cached.
        /// It's an error to push any GPU commands after disposal.
        /// Additionally, the GPU commands FIFO must be empty for disposal,
        /// and processing of all commands must have finished.
        /// </summary>
        public void Dispose()
        {
            GPFifo.Dispose();
            HostInitalized.Dispose();
            _gpuReadyEvent.Dispose();

            // Has to be disposed before processing deferred actions, as it will produce some.
            foreach (PhysicalMemory physicalMemory in PhysicalMemoryRegistry.Values)
            {
                physicalMemory.Dispose();
            }

            SupportBufferUpdater.Dispose();

            PhysicalMemoryRegistry.Clear();

            RunDeferredActions();

            Renderer.Dispose();
        }
    }
}

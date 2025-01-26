using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Gpu.Engine.GPFifo;
using Ryujinx.Graphics.Gpu.Image;
using Ryujinx.Graphics.Gpu.Memory;
using System;
using System.Threading;

namespace Ryujinx.Graphics.Gpu
{
    /// <summary>
    /// Represents a GPU channel.
    /// </summary>
    public class GpuChannel : IDisposable
    {
        private readonly GpuContext _context;
        private readonly GPFifoDevice _device;
        private readonly GPFifoProcessor _processor;
        private MemoryManager _memoryManager;

        /// <summary>
        /// Channel buffer bindings manager.
        /// </summary>
        internal BufferManager BufferManager { get; }

        /// <summary>
        /// Channel texture bindings manager.
        /// </summary>
        internal TextureManager TextureManager { get; }

        /// <summary>
        /// Current channel memory manager.
        /// </summary>
        internal MemoryManager MemoryManager => _memoryManager;

        /// <summary>
        /// Host hardware capabilities from the GPU context.
        /// </summary>
        internal ref Capabilities Capabilities => ref _context.Capabilities;

        /// <summary>
        /// Creates a new instance of a GPU channel.
        /// </summary>
        /// <param name="context">GPU context that the channel belongs to</param>
        internal GpuChannel(GpuContext context)
        {
            _context = context;
            _device = context.GPFifo;
            _processor = new GPFifoProcessor(context, this);
            BufferManager = new BufferManager(context, this);
            TextureManager = new TextureManager(context, this);
        }

        /// <summary>
        /// Binds a memory manager to the channel.
        /// All submitted and in-flight commands will use the specified memory manager for any memory operations.
        /// </summary>
        /// <param name="memoryManager">The new memory manager to be bound</param>
        public void BindMemory(MemoryManager memoryManager)
        {
            MemoryManager oldMemoryManager = Interlocked.Exchange(ref _memoryManager, memoryManager ?? throw new ArgumentNullException(nameof(memoryManager)));

            memoryManager.Physical.IncrementReferenceCount();

            if (oldMemoryManager != null)
            {
                oldMemoryManager.Physical.BufferCache.NotifyBuffersModified -= BufferManager.Rebind;
                oldMemoryManager.Physical.DecrementReferenceCount();
                oldMemoryManager.MemoryUnmapped -= MemoryUnmappedHandler;
            }

            memoryManager.Physical.BufferCache.NotifyBuffersModified += BufferManager.Rebind;
            memoryManager.MemoryUnmapped += MemoryUnmappedHandler;

            // Since the memory manager changed, make sure we will get pools from addresses of the new memory manager.
            TextureManager.ReloadPools();
            memoryManager.Physical.BufferCache.QueuePrune();
        }

        /// <summary>
        /// Memory mappings change event handler.
        /// </summary>
        /// <param name="sender">Memory manager where the mappings changed</param>
        /// <param name="e">Information about the region that is being changed</param>
        private void MemoryUnmappedHandler(object sender, UnmapEventArgs e)
        {
            TextureManager.ReloadPools();

            MemoryManager memoryManager = Volatile.Read(ref _memoryManager);
            memoryManager?.Physical.BufferCache.QueuePrune();
        }

        /// <summary>
        /// Writes data directly to the state of the specified class.
        /// </summary>
        /// <param name="classId">ID of the class to write the data into</param>
        /// <param name="offset">State offset in bytes</param>
        /// <param name="value">Value to be written</param>
        public void Write(ClassId classId, int offset, uint value)
        {
            _processor.Write(classId, offset, (int)value);
        }

        /// <summary>
        /// Push a GPFIFO entry in the form of a prefetched command buffer.
        /// It is intended to be used by nvservices to handle special cases.
        /// </summary>
        /// <param name="commandBuffer">The command buffer containing the prefetched commands</param>
        public void PushHostCommandBuffer(int[] commandBuffer)
        {
            _device.PushHostCommandBuffer(_processor, commandBuffer);
        }

        /// <summary>
        /// Pushes GPFIFO entries.
        /// </summary>
        /// <param name="entries">GPFIFO entries</param>
        public void PushEntries(ReadOnlySpan<ulong> entries)
        {
            _device.PushEntries(_processor, entries);
        }

        /// <summary>
        /// Disposes the GPU channel.
        /// It's an error to use the GPU channel after disposal.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _context.DeferredActions.Enqueue(Destroy);
        }

        /// <summary>
        /// Performs disposal of the host GPU resources used by this channel, that are not shared.
        /// This must only be called from the render thread.
        /// </summary>
        private void Destroy()
        {
            _processor.Dispose();
            TextureManager.Dispose();

            MemoryManager oldMemoryManager = Interlocked.Exchange(ref _memoryManager, null);
            if (oldMemoryManager != null)
            {
                oldMemoryManager.Physical.BufferCache.NotifyBuffersModified -= BufferManager.Rebind;
                oldMemoryManager.Physical.DecrementReferenceCount();
                oldMemoryManager.MemoryUnmapped -= MemoryUnmappedHandler;
            }
        }
    }
}

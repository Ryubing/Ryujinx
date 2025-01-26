using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Kernel.Threading;
using Ryujinx.HLE.HOS.Services.SurfaceFlinger.Types;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Ryujinx.HLE.HOS.Services.SurfaceFlinger
{
    class BufferQueueCore
    {
        public BufferSlotArray Slots;
        public int OverrideMaxBufferCount;
        public bool UseAsyncBuffer;
        public bool DequeueBufferCannotBlock;
        public PixelFormat DefaultBufferFormat;
        public int DefaultWidth;
        public int DefaultHeight;
        public int DefaultMaxBufferCount;
        public int MaxAcquiredBufferCount;
        public bool BufferHasBeenQueued;
        public ulong FrameCounter;
        public NativeWindowTransform TransformHint;
        public bool IsAbandoned;
        public NativeWindowApi ConnectedApi;
        public bool IsAllocating;
        public IProducerListener ProducerListener;
        public IConsumerListener ConsumerListener;
        public bool ConsumerControlledByApp;
        public uint ConsumerUsageBits;
        public List<BufferItem> Queue;
        public BufferInfo[] BufferHistory;
        public uint BufferHistoryPosition;
        public bool EnableExternalEvent;
        public int MaxBufferCountCached;

        public readonly object Lock = new();

        private readonly KEvent _waitBufferFreeEvent;
        private readonly KEvent _frameAvailableEvent;

        public ulong Owner { get; }

        public bool Active { get; private set; }

        public const int BufferHistoryArraySize = 8;

        public event Action BufferQueued;

        public BufferQueueCore(Switch device, ulong pid)
        {
            Slots = new BufferSlotArray();
            IsAbandoned = false;
            OverrideMaxBufferCount = 0;
            DequeueBufferCannotBlock = false;
            UseAsyncBuffer = false;
            DefaultWidth = 1;
            DefaultHeight = 1;
            DefaultMaxBufferCount = 2;
            MaxAcquiredBufferCount = 1;
            FrameCounter = 0;
            TransformHint = 0;
            DefaultBufferFormat = PixelFormat.Rgba8888;
            IsAllocating = false;
            ProducerListener = null;
            ConsumerListener = null;
            ConsumerUsageBits = 0;

            Queue = [];

            // TODO: CreateGraphicBufferAlloc?

            _waitBufferFreeEvent = new KEvent(device.System.KernelContext);
            _frameAvailableEvent = new KEvent(device.System.KernelContext);

            Owner = pid;

            Active = true;

            BufferHistory = new BufferInfo[BufferHistoryArraySize];
            EnableExternalEvent = true;
            MaxBufferCountCached = 0;
        }

        public int GetMinUndequeuedBufferCountLocked(bool async)
        {
            if (!UseAsyncBuffer)
            {
                return 0;
            }

            if (DequeueBufferCannotBlock || async)
            {
                return MaxAcquiredBufferCount + 1;
            }

            return MaxAcquiredBufferCount;
        }

        public int GetMinMaxBufferCountLocked(bool async)
        {
            return GetMinUndequeuedBufferCountLocked(async);
        }

        public void UpdateMaxBufferCountCachedLocked(int slot)
        {
            if (MaxBufferCountCached <= slot)
            {
                MaxBufferCountCached = slot + 1;
            }
        }

        public int GetMaxBufferCountLocked(bool async)
        {
            int minMaxBufferCount = GetMinMaxBufferCountLocked(async);

            int maxBufferCount = Math.Max(DefaultMaxBufferCount, minMaxBufferCount);

            if (OverrideMaxBufferCount != 0)
            {
                return OverrideMaxBufferCount;
            }

            // Preserve all buffers already in control of the producer and the consumer.
            for (int slot = maxBufferCount; slot < Slots.Length; slot++)
            {
                BufferState state = Slots[slot].BufferState;

                if (state == BufferState.Queued || state == BufferState.Dequeued)
                {
                    maxBufferCount = slot + 1;
                }
            }

            return maxBufferCount;
        }

        public Status SetDefaultMaxBufferCountLocked(int count)
        {
            int minBufferCount = UseAsyncBuffer ? 2 : 1;

            if (count < minBufferCount || count > Slots.Length)
            {
                return Status.BadValue;
            }

            DefaultMaxBufferCount = count;

            SignalDequeueEvent();

            return Status.Success;
        }

        public void SignalWaitBufferFreeEvent()
        {
            if (EnableExternalEvent)
            {
                _waitBufferFreeEvent.WritableEvent.Signal();
            }
        }

        public void SignalFrameAvailableEvent()
        {
            if (EnableExternalEvent)
            {
                _frameAvailableEvent.WritableEvent.Signal();
            }
        }

        public void PrepareForExit()
        {
            lock (Lock)
            {
                Active = false;

                Monitor.PulseAll(Lock);
            }
        }

        // TODO: Find an accurate way to handle a regular condvar here as this will wake up unwanted threads in some edge cases.
        public void SignalDequeueEvent()
        {
            Monitor.PulseAll(Lock);
        }

        public void WaitDequeueEvent()
        {
            WaitForLock();
        }

        public void SignalIsAllocatingEvent()
        {
            Monitor.PulseAll(Lock);
        }

        public void WaitIsAllocatingEvent()
        {
            WaitForLock();
        }

        public void SignalQueueEvent()
        {
            BufferQueued?.Invoke();
        }

        private void WaitForLock()
        {
            if (Active)
            {
                Monitor.Wait(Lock);
            }
        }

        public void FreeBufferLocked(int slot)
        {
            Slots[slot].GraphicBuffer.Reset();

            if (Slots[slot].BufferState == BufferState.Acquired)
            {
                Slots[slot].NeedsCleanupOnRelease = true;
            }

            Slots[slot].BufferState = BufferState.Free;
            Slots[slot].FrameNumber = uint.MaxValue;
            Slots[slot].AcquireCalled = false;
            Slots[slot].Fence.FenceCount = 0;
        }

        public void FreeAllBuffersLocked()
        {
            BufferHasBeenQueued = false;

            for (int slot = 0; slot < Slots.Length; slot++)
            {
                FreeBufferLocked(slot);
            }
        }

        public bool StillTracking(ref BufferItem item)
        {
            BufferSlot slot = Slots[item.Slot];

            // TODO: Check this. On Android, this checks the "handle". I assume NvMapHandle is the handle, but it might not be.
            return !slot.GraphicBuffer.IsNull && slot.GraphicBuffer.Object.Buffer.Surfaces[0].NvMapHandle == item.GraphicBuffer.Object.Buffer.Surfaces[0].NvMapHandle;
        }

        public void WaitWhileAllocatingLocked()
        {
            while (IsAllocating)
            {
                WaitIsAllocatingEvent();
            }
        }

        public void CheckSystemEventsLocked(int maxBufferCount)
        {
            if (!EnableExternalEvent)
            {
                return;
            }

            bool needBufferReleaseSignal = false;
            bool needFrameAvailableSignal = false;

            if (maxBufferCount > 1)
            {
                for (int i = 0; i < maxBufferCount; i++)
                {
                    if (Slots[i].BufferState == BufferState.Queued)
                    {
                        needFrameAvailableSignal = true;
                    }
                    else if (Slots[i].BufferState == BufferState.Free)
                    {
                        needBufferReleaseSignal = true;
                    }
                }
            }

            if (needBufferReleaseSignal)
            {
                SignalWaitBufferFreeEvent();
            }
            else
            {
                _waitBufferFreeEvent.WritableEvent.Clear();
            }

            if (needFrameAvailableSignal)
            {
                SignalFrameAvailableEvent();
            }
            else
            {
                _frameAvailableEvent.WritableEvent.Clear();
            }
        }

        public bool IsProducerConnectedLocked()
        {
            return ConnectedApi != NativeWindowApi.NoApi;
        }

        public bool IsConsumerConnectedLocked()
        {
            return ConsumerListener != null;
        }

        public KReadableEvent GetWaitBufferFreeEvent()
        {
            lock (Lock)
            {
                return _waitBufferFreeEvent.ReadableEvent;
            }
        }

        public bool IsOwnedByConsumerLocked(int slot)
        {
            if (Slots[slot].BufferState != BufferState.Acquired)
            {
                Logger.Error?.Print(LogClass.SurfaceFlinger, $"Slot {slot} is not owned by the consumer (state = {Slots[slot].BufferState})");

                return false;
            }

            return true;
        }

        public bool IsOwnedByProducerLocked(int slot)
        {
            if (Slots[slot].BufferState != BufferState.Dequeued)
            {
                Logger.Error?.Print(LogClass.SurfaceFlinger, $"Slot {slot} is not owned by the producer (state = {Slots[slot].BufferState})");

                return false;
            }

            return true;
        }
    }
}

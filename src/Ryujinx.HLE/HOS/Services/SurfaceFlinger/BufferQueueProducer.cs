using Ryujinx.Common.Logging;
using Ryujinx.Cpu;
using Ryujinx.HLE.HOS.Kernel.Threading;
using Ryujinx.HLE.HOS.Services.Settings;
using Ryujinx.HLE.HOS.Services.SurfaceFlinger.Types;
using Ryujinx.HLE.HOS.Services.Time.Clock;
using System;
using System.Threading;

namespace Ryujinx.HLE.HOS.Services.SurfaceFlinger
{
    class BufferQueueProducer : IGraphicBufferProducer
    {
        public BufferQueueCore Core { get; }

        private readonly ITickSource _tickSource;

#pragma warning disable IDE0052 // Remove unread private member
        private uint _stickyTransform;
#pragma warning restore IDE0052

        private uint _nextCallbackTicket;
        private uint _currentCallbackTicket;
        private uint _callbackTicket;

        private readonly object _callbackLock = new();

        public BufferQueueProducer(BufferQueueCore core, ITickSource tickSource)
        {
            Core = core;
            _tickSource = tickSource;

            _stickyTransform = 0;
            _callbackTicket = 0;
            _nextCallbackTicket = 0;
            _currentCallbackTicket = 0;
        }

        public override Status RequestBuffer(int slot, out AndroidStrongPointer<GraphicBuffer> graphicBuffer)
        {
            graphicBuffer = new AndroidStrongPointer<GraphicBuffer>();

            lock (Core.Lock)
            {
                if (Core.IsAbandoned)
                {
                    return Status.NoInit;
                }

                if (slot < 0 || slot >= Core.Slots.Length || !Core.IsOwnedByProducerLocked(slot))
                {
                    return Status.BadValue;
                }

                graphicBuffer.Set(Core.Slots[slot].GraphicBuffer);

                Core.Slots[slot].RequestBufferCalled = true;

                return Status.Success;
            }
        }

        public override Status SetBufferCount(int bufferCount)
        {
            IConsumerListener listener = null;

            lock (Core.Lock)
            {
                if (Core.IsAbandoned)
                {
                    return Status.NoInit;
                }

                if (bufferCount > BufferSlotArray.NumBufferSlots)
                {
                    return Status.BadValue;
                }

                for (int slot = 0; slot < Core.Slots.Length; slot++)
                {
                    if (Core.Slots[slot].BufferState == BufferState.Dequeued)
                    {
                        return Status.BadValue;
                    }
                }

                if (bufferCount == 0)
                {
                    Core.OverrideMaxBufferCount = 0;
                    Core.SignalDequeueEvent();

                    return Status.Success;
                }

                int minBufferSlots = Core.GetMinMaxBufferCountLocked(false);

                if (bufferCount < minBufferSlots)
                {
                    return Status.BadValue;
                }

                int preallocatedBufferCount = GetPreallocatedBufferCountLocked();

                if (preallocatedBufferCount <= 0)
                {
                    Core.Queue.Clear();
                    Core.FreeAllBuffersLocked();
                }
                else if (preallocatedBufferCount < bufferCount)
                {
                    Logger.Error?.Print(LogClass.SurfaceFlinger, "Not enough buffers. Try with more pre-allocated buffers");

                    return Status.Success;
                }

                Core.OverrideMaxBufferCount = bufferCount;

                Core.SignalDequeueEvent();
                Core.SignalWaitBufferFreeEvent();

                listener = Core.ConsumerListener;
            }

            listener?.OnBuffersReleased();

            return Status.Success;
        }

        public override Status DequeueBuffer(out int slot,
                                             out AndroidFence fence,
                                             bool async,
                                             uint width,
                                             uint height,
                                             PixelFormat format,
                                             uint usage)
        {
            if ((width == 0 && height != 0) || (height == 0 && width != 0))
            {
                slot = BufferSlotArray.InvalidBufferSlot;
                fence = AndroidFence.NoFence;

                return Status.BadValue;
            }

            Status returnFlags = Status.Success;

            bool attachedByConsumer = false;

            lock (Core.Lock)
            {
                if (format == PixelFormat.Unknown)
                {
                    format = Core.DefaultBufferFormat;
                }

                usage |= Core.ConsumerUsageBits;

                Status status = WaitForFreeSlotThenRelock(async, out slot, out returnFlags);

                if (status != Status.Success)
                {
                    slot = BufferSlotArray.InvalidBufferSlot;
                    fence = AndroidFence.NoFence;

                    return status;
                }

                if (slot == BufferSlotArray.InvalidBufferSlot)
                {
                    fence = AndroidFence.NoFence;

                    Logger.Error?.Print(LogClass.SurfaceFlinger, "No available buffer slots");

                    return Status.Busy;
                }

                attachedByConsumer = Core.Slots[slot].AttachedByConsumer;

                if (width == 0 || height == 0)
                {
                    width = (uint)Core.DefaultWidth;
                    height = (uint)Core.DefaultHeight;
                }

                GraphicBuffer graphicBuffer = Core.Slots[slot].GraphicBuffer.Object;

                if (Core.Slots[slot].GraphicBuffer.IsNull
                    || graphicBuffer.Width != width
                    || graphicBuffer.Height != height
                    || graphicBuffer.Format != format
                    || (graphicBuffer.Usage & usage) != usage)
                {
                    if (!Core.Slots[slot].IsPreallocated)
                    {
                        slot = BufferSlotArray.InvalidBufferSlot;
                        fence = AndroidFence.NoFence;

                        return Status.NoMemory;
                    }
                    else
                    {
                        Logger.Error?.Print(LogClass.SurfaceFlinger,
                                            $"Preallocated buffer mismatch - slot {slot}\n" +
                                            $"available: Width = {graphicBuffer.Width} Height = {graphicBuffer.Height} Format = {graphicBuffer.Format} Usage = {graphicBuffer.Usage:x} " +
                                            $"requested: Width = {width} Height = {height} Format = {format} Usage = {usage:x}");

                        slot = BufferSlotArray.InvalidBufferSlot;
                        fence = AndroidFence.NoFence;

                        return Status.NoInit;
                    }
                }

                Core.Slots[slot].BufferState = BufferState.Dequeued;

                Core.UpdateMaxBufferCountCachedLocked(slot);

                fence = Core.Slots[slot].Fence;

                Core.Slots[slot].Fence = AndroidFence.NoFence;
                Core.Slots[slot].QueueTime = TimeSpanType.Zero;
                Core.Slots[slot].PresentationTime = TimeSpanType.Zero;

                Core.CheckSystemEventsLocked(Core.GetMaxBufferCountLocked(async));
            }

            if (attachedByConsumer)
            {
                returnFlags |= Status.BufferNeedsReallocation;
            }

            return returnFlags;
        }

        public override Status DetachBuffer(int slot)
        {
            lock (Core.Lock)
            {
                if (Core.IsAbandoned)
                {
                    return Status.NoInit;
                }

                if (slot < 0 || slot >= Core.Slots.Length || !Core.IsOwnedByProducerLocked(slot))
                {
                    return Status.BadValue;
                }

                if (!Core.Slots[slot].RequestBufferCalled)
                {
                    Logger.Error?.Print(LogClass.SurfaceFlinger, $"Slot {slot} was detached without requesting a buffer");

                    return Status.BadValue;
                }

                Core.FreeBufferLocked(slot);
                Core.SignalDequeueEvent();

                return Status.Success;
            }
        }

        public override Status DetachNextBuffer(out AndroidStrongPointer<GraphicBuffer> graphicBuffer, out AndroidFence fence)
        {
            lock (Core.Lock)
            {
                Core.WaitWhileAllocatingLocked();

                if (Core.IsAbandoned)
                {
                    graphicBuffer = default;
                    fence = AndroidFence.NoFence;

                    return Status.NoInit;
                }

                int nextBufferSlot = BufferSlotArray.InvalidBufferSlot;

                for (int slot = 0; slot < Core.Slots.Length; slot++)
                {
                    if (Core.Slots[slot].BufferState == BufferState.Free && !Core.Slots[slot].GraphicBuffer.IsNull)
                    {
                        if (nextBufferSlot == BufferSlotArray.InvalidBufferSlot || Core.Slots[slot].FrameNumber < Core.Slots[nextBufferSlot].FrameNumber)
                        {
                            nextBufferSlot = slot;
                        }
                    }
                }

                if (nextBufferSlot == BufferSlotArray.InvalidBufferSlot)
                {
                    graphicBuffer = default;
                    fence = AndroidFence.NoFence;

                    return Status.NoMemory;
                }

                graphicBuffer = Core.Slots[nextBufferSlot].GraphicBuffer;
                fence = Core.Slots[nextBufferSlot].Fence;

                Core.FreeBufferLocked(nextBufferSlot);

                return Status.Success;
            }
        }

        public override Status AttachBuffer(out int slot, AndroidStrongPointer<GraphicBuffer> graphicBuffer)
        {
            lock (Core.Lock)
            {
                Core.WaitWhileAllocatingLocked();

                Status status = WaitForFreeSlotThenRelock(false, out slot, out Status returnFlags);

                if (status != Status.Success)
                {
                    return status;
                }

                if (slot == BufferSlotArray.InvalidBufferSlot)
                {
                    Logger.Error?.Print(LogClass.SurfaceFlinger, "No available buffer slots");

                    return Status.Busy;
                }

                Core.UpdateMaxBufferCountCachedLocked(slot);

                Core.Slots[slot].GraphicBuffer.Set(graphicBuffer);

                Core.Slots[slot].BufferState = BufferState.Dequeued;
                Core.Slots[slot].Fence = AndroidFence.NoFence;
                Core.Slots[slot].RequestBufferCalled = true;

                return returnFlags;
            }
        }

        public override Status QueueBuffer(int slot, ref QueueBufferInput input, out QueueBufferOutput output)
        {
            output = default;

            switch (input.ScalingMode)
            {
                case NativeWindowScalingMode.Freeze:
                case NativeWindowScalingMode.ScaleToWindow:
                case NativeWindowScalingMode.ScaleCrop:
                case NativeWindowScalingMode.Unknown:
                case NativeWindowScalingMode.NoScaleCrop:
                    break;
                default:
                    return Status.BadValue;
            }

            BufferItem item = new();

            IConsumerListener frameAvailableListener = null;
            IConsumerListener frameReplaceListener = null;

            lock (Core.Lock)
            {
                if (Core.IsAbandoned)
                {
                    return Status.NoInit;
                }

                int maxBufferCount = Core.GetMaxBufferCountLocked(input.Async != 0);

                if (input.Async != 0 && Core.OverrideMaxBufferCount != 0 && Core.OverrideMaxBufferCount < maxBufferCount)
                {
                    return Status.BadValue;
                }

                if (slot < 0 || slot >= Core.Slots.Length || !Core.IsOwnedByProducerLocked(slot))
                {
                    return Status.BadValue;
                }

                if (!Core.Slots[slot].RequestBufferCalled)
                {
                    Logger.Error?.Print(LogClass.SurfaceFlinger, $"Slot {slot} was queued without requesting a buffer");

                    return Status.BadValue;
                }

                input.Crop.Intersect(Core.Slots[slot].GraphicBuffer.Object.ToRect(), out Rect croppedRect);

                if (croppedRect != input.Crop)
                {
                    return Status.BadValue;
                }

                Core.Slots[slot].Fence = input.Fence;
                Core.Slots[slot].BufferState = BufferState.Queued;
                Core.FrameCounter++;
                Core.Slots[slot].FrameNumber = Core.FrameCounter;
                Core.Slots[slot].QueueTime = TimeSpanType.FromTimeSpan(_tickSource.ElapsedTime);
                Core.Slots[slot].PresentationTime = TimeSpanType.Zero;

                item.AcquireCalled = Core.Slots[slot].AcquireCalled;
                item.Crop = input.Crop;
                item.Transform = input.Transform;
                item.TransformToDisplayInverse = (input.Transform & NativeWindowTransform.InverseDisplay) == NativeWindowTransform.InverseDisplay;
                item.ScalingMode = input.ScalingMode;
                item.Timestamp = input.Timestamp;
                item.IsAutoTimestamp = input.IsAutoTimestamp != 0;
                item.SwapInterval = input.SwapInterval;
                item.FrameNumber = Core.FrameCounter;
                item.Slot = slot;
                item.Fence = input.Fence;
                item.IsDroppable = Core.DequeueBufferCannotBlock || input.Async != 0;

                item.GraphicBuffer.Set(Core.Slots[slot].GraphicBuffer);
                item.GraphicBuffer.Object.IncrementNvMapHandleRefCount(Core.Owner);

                Core.BufferHistoryPosition = (Core.BufferHistoryPosition + 1) % BufferQueueCore.BufferHistoryArraySize;

                Core.BufferHistory[Core.BufferHistoryPosition] = new BufferInfo
                {
                    FrameNumber = Core.FrameCounter,
                    QueueTime = Core.Slots[slot].QueueTime,
                    State = BufferState.Queued,
                };

                _stickyTransform = input.StickyTransform;

                if (Core.Queue.Count == 0)
                {
                    Core.Queue.Add(item);

                    frameAvailableListener = Core.ConsumerListener;
                }
                else
                {
                    BufferItem frontItem = Core.Queue[0];

                    if (frontItem.IsDroppable)
                    {
                        if (Core.StillTracking(ref frontItem))
                        {
                            Core.Slots[slot].BufferState = BufferState.Free;
                            Core.Slots[slot].FrameNumber = 0;
                        }

                        Core.Queue.RemoveAt(0);
                        Core.Queue.Insert(0, item);

                        frameReplaceListener = Core.ConsumerListener;
                    }
                    else
                    {
                        Core.Queue.Add(item);

                        frameAvailableListener = Core.ConsumerListener;
                    }
                }

                Core.BufferHasBeenQueued = true;
                Core.SignalDequeueEvent();

                Core.CheckSystemEventsLocked(maxBufferCount);

                output = new QueueBufferOutput
                {
                    Width = (uint)Core.DefaultWidth,
                    Height = (uint)Core.DefaultHeight,
                    TransformHint = Core.TransformHint,
                    NumPendingBuffers = (uint)Core.Queue.Count,
                };

                if ((input.StickyTransform & 8) != 0)
                {
                    output.TransformHint |= NativeWindowTransform.ReturnFrameNumber;
                    output.FrameNumber = Core.Slots[slot].FrameNumber;
                }

                _callbackTicket = _nextCallbackTicket++;
            }

            lock (_callbackLock)
            {
                while (_callbackTicket != _currentCallbackTicket)
                {
                    Monitor.Wait(_callbackLock);
                }

                frameAvailableListener?.OnFrameAvailable(ref item);
                frameReplaceListener?.OnFrameReplaced(ref item);

                _currentCallbackTicket++;

                Monitor.PulseAll(_callbackLock);
            }

            Core.SignalQueueEvent();

            return Status.Success;
        }

        public override void CancelBuffer(int slot, ref AndroidFence fence)
        {
            lock (Core.Lock)
            {
                if (Core.IsAbandoned || slot < 0 || slot >= Core.Slots.Length || !Core.IsOwnedByProducerLocked(slot))
                {
                    return;
                }

                Core.Slots[slot].BufferState = BufferState.Free;
                Core.Slots[slot].FrameNumber = 0;
                Core.Slots[slot].Fence = fence;
                Core.SignalDequeueEvent();
                Core.SignalWaitBufferFreeEvent();
            }
        }

        public override Status Query(NativeWindowAttribute what, out int outValue)
        {
            lock (Core.Lock)
            {
                if (Core.IsAbandoned)
                {
                    outValue = 0;
                    return Status.NoInit;
                }

                switch (what)
                {
                    case NativeWindowAttribute.Width:
                        outValue = Core.DefaultWidth;
                        return Status.Success;
                    case NativeWindowAttribute.Height:
                        outValue = Core.DefaultHeight;
                        return Status.Success;
                    case NativeWindowAttribute.Format:
                        outValue = (int)Core.DefaultBufferFormat;
                        return Status.Success;
                    case NativeWindowAttribute.MinUnqueuedBuffers:
                        outValue = Core.GetMinUndequeuedBufferCountLocked(false);
                        return Status.Success;
                    case NativeWindowAttribute.ConsumerRunningBehind:
                        outValue = Core.Queue.Count > 1 ? 1 : 0;
                        return Status.Success;
                    case NativeWindowAttribute.ConsumerUsageBits:
                        outValue = (int)Core.ConsumerUsageBits;
                        return Status.Success;
                    case NativeWindowAttribute.MaxBufferCountAsync:
                        outValue = Core.GetMaxBufferCountLocked(true);
                        return Status.Success;
                    default:
                        outValue = 0;
                        return Status.BadValue;
                }
            }
        }

        public override Status Connect(IProducerListener listener, NativeWindowApi api, bool producerControlledByApp, out QueueBufferOutput output)
        {
            output = new QueueBufferOutput();

            lock (Core.Lock)
            {
                if (Core.IsAbandoned || Core.ConsumerListener == null)
                {
                    return Status.NoInit;
                }

                if (Core.ConnectedApi != NativeWindowApi.NoApi)
                {
                    return Status.BadValue;
                }

                Core.BufferHasBeenQueued = false;
                Core.DequeueBufferCannotBlock = Core.ConsumerControlledByApp && producerControlledByApp;

                switch (api)
                {
                    case NativeWindowApi.NVN:
                    case NativeWindowApi.CPU:
                    case NativeWindowApi.Media:
                    case NativeWindowApi.Camera:
                        Core.ProducerListener = listener;
                        Core.ConnectedApi = api;

                        output.Width = (uint)Core.DefaultWidth;
                        output.Height = (uint)Core.DefaultHeight;
                        output.TransformHint = Core.TransformHint;
                        output.NumPendingBuffers = (uint)Core.Queue.Count;

                        if (NxSettings.Settings.TryGetValue("nv!nvn_no_vsync_capability", out object noVSyncCapability) && (bool)noVSyncCapability)
                        {
                            output.TransformHint |= NativeWindowTransform.NoVSyncCapability;
                        }

                        return Status.Success;
                    default:
                        return Status.BadValue;
                }
            }
        }

        public override Status Disconnect(NativeWindowApi api)
        {
            IProducerListener producerListener = null;

            Status status = Status.BadValue;

            lock (Core.Lock)
            {
                Core.WaitWhileAllocatingLocked();

                if (Core.IsAbandoned)
                {
                    return Status.Success;
                }

                switch (api)
                {
                    case NativeWindowApi.NVN:
                    case NativeWindowApi.CPU:
                    case NativeWindowApi.Media:
                    case NativeWindowApi.Camera:
                        if (Core.ConnectedApi == api)
                        {
                            Core.Queue.Clear();
                            Core.FreeAllBuffersLocked();
                            Core.SignalDequeueEvent();

                            producerListener = Core.ProducerListener;

                            Core.ProducerListener = null;
                            Core.ConnectedApi = NativeWindowApi.NoApi;

                            Core.SignalWaitBufferFreeEvent();
                            Core.SignalFrameAvailableEvent();

                            status = Status.Success;
                        }
                        break;
                }
            }

            producerListener?.OnBufferReleased();

            return status;
        }

        private int GetPreallocatedBufferCountLocked()
        {
            int bufferCount = 0;

            for (int i = 0; i < Core.Slots.Length; i++)
            {
                if (Core.Slots[i].IsPreallocated)
                {
                    bufferCount++;
                }
            }

            return bufferCount;
        }

        public override Status SetPreallocatedBuffer(int slot, AndroidStrongPointer<GraphicBuffer> graphicBuffer)
        {
            if (slot < 0 || slot >= Core.Slots.Length)
            {
                return Status.BadValue;
            }

            lock (Core.Lock)
            {
                // If we are replacing a buffer that has already been queued, make sure we release the references.
                if (Core.Slots[slot].BufferState == BufferState.Queued)
                {
                    Core.Slots[slot].GraphicBuffer.Object.DecrementNvMapHandleRefCount(Core.Owner);
                }

                Core.Slots[slot].BufferState = BufferState.Free;
                Core.Slots[slot].Fence = AndroidFence.NoFence;
                Core.Slots[slot].RequestBufferCalled = false;
                Core.Slots[slot].AcquireCalled = false;
                Core.Slots[slot].NeedsCleanupOnRelease = false;
                Core.Slots[slot].IsPreallocated = !graphicBuffer.IsNull;
                Core.Slots[slot].FrameNumber = 0;

                Core.Slots[slot].GraphicBuffer.Set(graphicBuffer);

                if (!Core.Slots[slot].GraphicBuffer.IsNull)
                {
                    Core.Slots[slot].GraphicBuffer.Object.Buffer.Usage &= (int)Core.ConsumerUsageBits;
                }

                Core.OverrideMaxBufferCount = GetPreallocatedBufferCountLocked();
                Core.UseAsyncBuffer = false;

                if (!graphicBuffer.IsNull)
                {
                    // NOTE: Nintendo set the default width, height and format from the GraphicBuffer..
                    //       This is entirely wrong and should only be controlled by the consumer...
                    Core.DefaultWidth = graphicBuffer.Object.Width;
                    Core.DefaultHeight = graphicBuffer.Object.Height;
                    Core.DefaultBufferFormat = graphicBuffer.Object.Format;
                }
                else
                {
                    bool allBufferFreed = true;

                    for (int i = 0; i < Core.Slots.Length; i++)
                    {
                        if (!Core.Slots[i].GraphicBuffer.IsNull)
                        {
                            allBufferFreed = false;
                            break;
                        }
                    }

                    if (allBufferFreed)
                    {
                        Core.Queue.Clear();
                        Core.FreeAllBuffersLocked();
                        Core.SignalDequeueEvent();
                        Core.SignalWaitBufferFreeEvent();
                        Core.SignalFrameAvailableEvent();

                        return Status.Success;
                    }
                }

                Core.SignalDequeueEvent();
                Core.SignalWaitBufferFreeEvent();

                return Status.Success;
            }
        }

        private Status WaitForFreeSlotThenRelock(bool async, out int freeSlot, out Status returnStatus)
        {
            bool tryAgain = true;

            freeSlot = BufferSlotArray.InvalidBufferSlot;
            returnStatus = Status.Success;

            while (tryAgain)
            {
                if (Core.IsAbandoned)
                {
                    freeSlot = BufferSlotArray.InvalidBufferSlot;

                    return Status.NoInit;
                }

                int maxBufferCount = Core.GetMaxBufferCountLocked(async);

                if (async && Core.OverrideMaxBufferCount != 0 && Core.OverrideMaxBufferCount < maxBufferCount)
                {
                    freeSlot = BufferSlotArray.InvalidBufferSlot;

                    return Status.BadValue;
                }


                if (maxBufferCount < Core.MaxBufferCountCached)
                {
                    for (int slot = maxBufferCount; slot < Core.MaxBufferCountCached; slot++)
                    {
                        if (Core.Slots[slot].BufferState == BufferState.Free && !Core.Slots[slot].GraphicBuffer.IsNull && !Core.Slots[slot].IsPreallocated)
                        {
                            Core.FreeBufferLocked(slot);
                            returnStatus |= Status.ReleaseAllBuffers;
                        }
                    }
                }

                freeSlot = BufferSlotArray.InvalidBufferSlot;

                int dequeuedCount = 0;
                int acquiredCount = 0;

                for (int slot = 0; slot < maxBufferCount; slot++)
                {
                    switch (Core.Slots[slot].BufferState)
                    {
                        case BufferState.Acquired:
                            acquiredCount++;
                            break;
                        case BufferState.Dequeued:
                            dequeuedCount++;
                            break;
                        case BufferState.Free:
                            if (freeSlot == BufferSlotArray.InvalidBufferSlot || Core.Slots[slot].FrameNumber < Core.Slots[freeSlot].FrameNumber)
                            {
                                freeSlot = slot;
                            }
                            break;
                        default:
                            break;
                    }
                }

                // The producer SHOULD call SetBufferCount otherwise it's not allowed to dequeue multiple buffers.
                if (Core.OverrideMaxBufferCount == 0 && dequeuedCount > 0)
                {
                    return Status.InvalidOperation;
                }

                if (Core.BufferHasBeenQueued)
                {
                    int newUndequeuedCount = maxBufferCount - (dequeuedCount + 1);
                    int minUndequeuedCount = Core.GetMinUndequeuedBufferCountLocked(async);

                    if (newUndequeuedCount < minUndequeuedCount)
                    {
                        Logger.Error?.Print(LogClass.SurfaceFlinger, $"Min undequeued buffer count ({minUndequeuedCount}) exceeded (dequeued = {dequeuedCount} undequeued = {newUndequeuedCount})");

                        return Status.InvalidOperation;
                    }
                }

                bool tooManyBuffers = Core.Queue.Count > maxBufferCount;

                tryAgain = freeSlot == BufferSlotArray.InvalidBufferSlot || tooManyBuffers;

                if (tryAgain)
                {
                    if (async || (Core.DequeueBufferCannotBlock && acquiredCount < Core.MaxAcquiredBufferCount))
                    {
                        Core.CheckSystemEventsLocked(maxBufferCount);

                        return Status.WouldBlock;
                    }

                    Core.WaitDequeueEvent();

                    if (!Core.Active)
                    {
                        break;
                    }
                }
            }

            return Status.Success;
        }

        protected override KReadableEvent GetWaitBufferFreeEvent()
        {
            return Core.GetWaitBufferFreeEvent();
        }

        public override Status GetBufferHistory(int bufferHistoryCount, out Span<BufferInfo> bufferInfos)
        {
            if (bufferHistoryCount <= 0)
            {
                bufferInfos = [];

                return Status.BadValue;
            }

            lock (Core.Lock)
            {
                bufferHistoryCount = Math.Min(bufferHistoryCount, Core.BufferHistory.Length);

                BufferInfo[] result = new BufferInfo[bufferHistoryCount];

                uint position = Core.BufferHistoryPosition;

                for (uint i = 0; i < bufferHistoryCount; i++)
                {
                    result[i] = Core.BufferHistory[(position - i) % Core.BufferHistory.Length];

                    position--;
                }

                bufferInfos = result;

                return Status.Success;
            }
        }
    }
}

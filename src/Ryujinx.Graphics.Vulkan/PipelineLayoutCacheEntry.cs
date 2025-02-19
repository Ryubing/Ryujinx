using Ryujinx.Graphics.GAL;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Vulkan
{
    class PipelineLayoutCacheEntry
    {
        private const int MaxPoolSizesPerSet = 8;

        private readonly VulkanRenderer _gd;
        private readonly Device _device;

        public DescriptorSetLayout[] DescriptorSetLayouts { get; }
        public bool[] DescriptorSetLayoutsUpdateAfterBind { get; }
        public PipelineLayout PipelineLayout { get; }

        private readonly int[] _consumedDescriptorsPerSet;
        private readonly DescriptorPoolSize[][] _poolSizes;

        private readonly DescriptorSetManager _descriptorSetManager;

        private readonly List<Auto<DescriptorSetCollection>>[][] _dsCache;
        private List<Auto<DescriptorSetCollection>>[] _currentDsCache;
        private readonly int[] _dsCacheCursor;
        private int _dsLastCbIndex;
        private int _dsLastSubmissionCount;

        private struct ManualDescriptorSetEntry
        {
            public Auto<DescriptorSetCollection> DescriptorSet;
            public uint CbRefMask;
            public bool InUse;

            public ManualDescriptorSetEntry(Auto<DescriptorSetCollection> descriptorSet, int cbIndex)
            {
                DescriptorSet = descriptorSet;
                CbRefMask = 1u << cbIndex;
                InUse = true;
            }
        }

        private readonly struct PendingManualDsConsumption
        {
            public FenceHolder Fence { get; }
            public int CommandBufferIndex { get; }
            public int SetIndex { get; }
            public int CacheIndex { get; }

            public PendingManualDsConsumption(FenceHolder fence, int commandBufferIndex, int setIndex, int cacheIndex)
            {
                Fence = fence;
                CommandBufferIndex = commandBufferIndex;
                SetIndex = setIndex;
                CacheIndex = cacheIndex;
                fence.Get();
            }
        }

        private readonly List<ManualDescriptorSetEntry>[] _manualDsCache;
        private readonly Queue<PendingManualDsConsumption> _pendingManualDsConsumptions;
        private readonly Queue<int>[] _freeManualDsCacheEntries;

        private readonly Dictionary<long, DescriptorSetTemplate> _pdTemplates;
        private readonly ResourceDescriptorCollection _pdDescriptors;
        private long _lastPdUsage;
        private DescriptorSetTemplate _lastPdTemplate;

        private PipelineLayoutCacheEntry(VulkanRenderer gd, Device device, int setsCount)
        {
            _gd = gd;
            _device = device;

            _dsCache = new List<Auto<DescriptorSetCollection>>[CommandBufferPool.MaxCommandBuffers][];

            for (int i = 0; i < CommandBufferPool.MaxCommandBuffers; i++)
            {
                _dsCache[i] = new List<Auto<DescriptorSetCollection>>[setsCount];

                for (int j = 0; j < _dsCache[i].Length; j++)
                {
                    _dsCache[i][j] = [];
                }
            }

            _dsCacheCursor = new int[setsCount];
            _manualDsCache = new List<ManualDescriptorSetEntry>[setsCount];
            _pendingManualDsConsumptions = new Queue<PendingManualDsConsumption>();
            _freeManualDsCacheEntries = new Queue<int>[setsCount];
        }

        public PipelineLayoutCacheEntry(
            VulkanRenderer gd,
            Device device,
            ReadOnlyCollection<ResourceDescriptorCollection> setDescriptors,
            bool usePushDescriptors) : this(gd, device, setDescriptors.Count)
        {
            ResourceLayouts layouts = PipelineLayoutFactory.Create(gd, device, setDescriptors, usePushDescriptors);

            DescriptorSetLayouts = layouts.DescriptorSetLayouts;
            DescriptorSetLayoutsUpdateAfterBind = layouts.DescriptorSetLayoutsUpdateAfterBind;
            PipelineLayout = layouts.PipelineLayout;

            _consumedDescriptorsPerSet = new int[setDescriptors.Count];
            _poolSizes = new DescriptorPoolSize[setDescriptors.Count][];

            Span<DescriptorPoolSize> poolSizes = stackalloc DescriptorPoolSize[MaxPoolSizesPerSet];

            for (int setIndex = 0; setIndex < setDescriptors.Count; setIndex++)
            {
                int count = 0;

                foreach (ResourceDescriptor descriptor in setDescriptors[setIndex].Descriptors)
                {
                    count += descriptor.Count;
                }

                _consumedDescriptorsPerSet[setIndex] = count;
                _poolSizes[setIndex] = GetDescriptorPoolSizes(poolSizes, setDescriptors[setIndex], DescriptorSetManager.MaxSets).ToArray();
            }

            if (usePushDescriptors)
            {
                _pdDescriptors = setDescriptors[0];
                _pdTemplates = new();
            }

            _descriptorSetManager = new DescriptorSetManager(_device, setDescriptors.Count);
        }

        public void UpdateCommandBufferIndex(int commandBufferIndex)
        {
            int submissionCount = _gd.CommandBufferPool.GetSubmissionCount(commandBufferIndex);

            if (_dsLastCbIndex != commandBufferIndex || _dsLastSubmissionCount != submissionCount)
            {
                _dsLastCbIndex = commandBufferIndex;
                _dsLastSubmissionCount = submissionCount;
                Array.Clear(_dsCacheCursor);
            }

            _currentDsCache = _dsCache[commandBufferIndex];
        }

        public Auto<DescriptorSetCollection> GetNewDescriptorSetCollection(int setIndex, out bool isNew)
        {
            List<Auto<DescriptorSetCollection>> list = _currentDsCache[setIndex];
            int index = _dsCacheCursor[setIndex]++;
            if (index == list.Count)
            {
                Auto<DescriptorSetCollection> dsc = _descriptorSetManager.AllocateDescriptorSet(
                    _gd.Api,
                    DescriptorSetLayouts[setIndex],
                    _poolSizes[setIndex],
                    setIndex,
                    _consumedDescriptorsPerSet[setIndex],
                    DescriptorSetLayoutsUpdateAfterBind[setIndex]);

                list.Add(dsc);
                isNew = true;
                return dsc;
            }

            isNew = false;
            return list[index];
        }

        public Auto<DescriptorSetCollection> GetNewManualDescriptorSetCollection(CommandBufferScoped cbs, int setIndex, out int cacheIndex)
        {
            FreeCompletedManualDescriptorSets();

            List<ManualDescriptorSetEntry> list = _manualDsCache[setIndex] ??= [];
            Span<ManualDescriptorSetEntry> span = CollectionsMarshal.AsSpan(list);

            Queue<int> freeQueue = _freeManualDsCacheEntries[setIndex];

            // Do we have at least one freed descriptor set? If so, just use that.
            if (freeQueue != null && freeQueue.TryDequeue(out int freeIndex))
            {
                ref ManualDescriptorSetEntry entry = ref span[freeIndex];

                Debug.Assert(!entry.InUse && entry.CbRefMask == 0);

                entry.InUse = true;
                entry.CbRefMask = 1u << cbs.CommandBufferIndex;
                cacheIndex = freeIndex;

                _pendingManualDsConsumptions.Enqueue(new PendingManualDsConsumption(cbs.GetFence(), cbs.CommandBufferIndex, setIndex, freeIndex));

                return entry.DescriptorSet;
            }

            // Otherwise create a new descriptor set, and add to our pending queue for command buffer consumption tracking.
            Auto<DescriptorSetCollection> dsc = _descriptorSetManager.AllocateDescriptorSet(
                _gd.Api,
                DescriptorSetLayouts[setIndex],
                _poolSizes[setIndex],
                setIndex,
                _consumedDescriptorsPerSet[setIndex],
                DescriptorSetLayoutsUpdateAfterBind[setIndex]);

            cacheIndex = list.Count;
            list.Add(new ManualDescriptorSetEntry(dsc, cbs.CommandBufferIndex));
            _pendingManualDsConsumptions.Enqueue(new PendingManualDsConsumption(cbs.GetFence(), cbs.CommandBufferIndex, setIndex, cacheIndex));

            return dsc;
        }

        public void UpdateManualDescriptorSetCollectionOwnership(CommandBufferScoped cbs, int setIndex, int cacheIndex)
        {
            FreeCompletedManualDescriptorSets();

            List<ManualDescriptorSetEntry> list = _manualDsCache[setIndex];
            Span<ManualDescriptorSetEntry> span = CollectionsMarshal.AsSpan(list);
            ref ManualDescriptorSetEntry entry = ref span[cacheIndex];

            uint cbMask = 1u << cbs.CommandBufferIndex;

            if ((entry.CbRefMask & cbMask) == 0)
            {
                entry.CbRefMask |= cbMask;

                _pendingManualDsConsumptions.Enqueue(new PendingManualDsConsumption(cbs.GetFence(), cbs.CommandBufferIndex, setIndex, cacheIndex));
            }
        }

        private void FreeCompletedManualDescriptorSets()
        {
            FenceHolder signalledFence = null;
            while (_pendingManualDsConsumptions.TryPeek(out PendingManualDsConsumption pds) && (pds.Fence == signalledFence || pds.Fence.IsSignaled()))
            {
                signalledFence = pds.Fence; // Already checked - don't need to do it again.
                PendingManualDsConsumption dequeued = _pendingManualDsConsumptions.Dequeue();
                Debug.Assert(dequeued.Fence == pds.Fence);
                pds.Fence.Put();

                Span<ManualDescriptorSetEntry> span = CollectionsMarshal.AsSpan(_manualDsCache[dequeued.SetIndex]);
                ref ManualDescriptorSetEntry entry = ref span[dequeued.CacheIndex];
                entry.CbRefMask &= ~(1u << dequeued.CommandBufferIndex);

                if (!entry.InUse && entry.CbRefMask == 0)
                {
                    // If not in use by any array, and not bound to any command buffer, the descriptor set can be re-used immediately.
                    (_freeManualDsCacheEntries[dequeued.SetIndex] ??= new()).Enqueue(dequeued.CacheIndex);
                }
            }
        }

        public void ReleaseManualDescriptorSetCollection(int setIndex, int cacheIndex)
        {
            List<ManualDescriptorSetEntry> list = _manualDsCache[setIndex];
            Span<ManualDescriptorSetEntry> span = CollectionsMarshal.AsSpan(list);

            span[cacheIndex].InUse = false;

            if (span[cacheIndex].CbRefMask == 0)
            {
                // This is no longer in use by any array, so if not bound to any command buffer, the descriptor set can be re-used immediately.
                (_freeManualDsCacheEntries[setIndex] ??= new()).Enqueue(cacheIndex);
            }
        }

        private static Span<DescriptorPoolSize> GetDescriptorPoolSizes(Span<DescriptorPoolSize> output, ResourceDescriptorCollection setDescriptor, uint multiplier)
        {
            int count = 0;

            for (int index = 0; index < setDescriptor.Descriptors.Count; index++)
            {
                ResourceDescriptor descriptor = setDescriptor.Descriptors[index];
                DescriptorType descriptorType = descriptor.Type.Convert();

                bool found = false;

                for (int poolSizeIndex = 0; poolSizeIndex < count; poolSizeIndex++)
                {
                    if (output[poolSizeIndex].Type == descriptorType)
                    {
                        output[poolSizeIndex].DescriptorCount += (uint)descriptor.Count * multiplier;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    output[count++] = new DescriptorPoolSize()
                    {
                        Type = descriptorType,
                        DescriptorCount = (uint)descriptor.Count,
                    };
                }
            }

            return output[..count];
        }

        public DescriptorSetTemplate GetPushDescriptorTemplate(PipelineBindPoint pbp, long updateMask)
        {
            if (_lastPdUsage == updateMask && _lastPdTemplate != null)
            {
                // Most likely result is that it asks to update the same buffers.
                return _lastPdTemplate;
            }

            if (!_pdTemplates.TryGetValue(updateMask, out DescriptorSetTemplate template))
            {
                template = new DescriptorSetTemplate(_gd, _device, _pdDescriptors, updateMask, this, pbp, 0);

                _pdTemplates.Add(updateMask, template);
            }

            _lastPdUsage = updateMask;
            _lastPdTemplate = template;

            return template;
        }

        protected virtual unsafe void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_pdTemplates != null)
                {
                    foreach (DescriptorSetTemplate template in _pdTemplates.Values)
                    {
                        template.Dispose();
                    }
                }

                for (int i = 0; i < _dsCache.Length; i++)
                {
                    for (int j = 0; j < _dsCache[i].Length; j++)
                    {
                        for (int k = 0; k < _dsCache[i][j].Count; k++)
                        {
                            _dsCache[i][j][k].Dispose();
                        }

                        _dsCache[i][j].Clear();
                    }
                }

                for (int i = 0; i < _manualDsCache.Length; i++)
                {
                    if (_manualDsCache[i] == null)
                    {
                        continue;
                    }

                    for (int j = 0; j < _manualDsCache[i].Count; j++)
                    {
                        _manualDsCache[i][j].DescriptorSet.Dispose();
                    }

                    _manualDsCache[i].Clear();
                }

                _gd.Api.DestroyPipelineLayout(_device, PipelineLayout, null);

                for (int i = 0; i < DescriptorSetLayouts.Length; i++)
                {
                    _gd.Api.DestroyDescriptorSetLayout(_device, DescriptorSetLayouts[i], null);
                }

                while (_pendingManualDsConsumptions.TryDequeue(out PendingManualDsConsumption pds))
                {
                    pds.Fence.Put();
                }

                _descriptorSetManager.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}

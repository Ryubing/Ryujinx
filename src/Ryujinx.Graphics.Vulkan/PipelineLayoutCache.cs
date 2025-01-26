using Ryujinx.Graphics.GAL;
using Silk.NET.Vulkan;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace Ryujinx.Graphics.Vulkan
{
    class PipelineLayoutCache
    {
        private readonly struct PlceKey : IEquatable<PlceKey>
        {
            public readonly ReadOnlyCollection<ResourceDescriptorCollection> SetDescriptors;
            public readonly bool UsePushDescriptors;

            public PlceKey(ReadOnlyCollection<ResourceDescriptorCollection> setDescriptors, bool usePushDescriptors)
            {
                SetDescriptors = setDescriptors;
                UsePushDescriptors = usePushDescriptors;
            }

            public override int GetHashCode()
            {
                HashCode hasher = new();

                if (SetDescriptors != null)
                {
                    foreach (ResourceDescriptorCollection setDescriptor in SetDescriptors)
                    {
                        hasher.Add(setDescriptor);
                    }
                }

                hasher.Add(UsePushDescriptors);

                return hasher.ToHashCode();
            }

            public override bool Equals(object obj)
            {
                return obj is PlceKey other && Equals(other);
            }

            public bool Equals(PlceKey other)
            {
                if ((SetDescriptors == null) != (other.SetDescriptors == null))
                {
                    return false;
                }

                if (SetDescriptors != null)
                {
                    if (SetDescriptors.Count != other.SetDescriptors.Count)
                    {
                        return false;
                    }

                    for (int index = 0; index < SetDescriptors.Count; index++)
                    {
                        if (!SetDescriptors[index].Equals(other.SetDescriptors[index]))
                        {
                            return false;
                        }
                    }
                }

                return UsePushDescriptors == other.UsePushDescriptors;
            }
        }

        private readonly ConcurrentDictionary<PlceKey, PipelineLayoutCacheEntry> _plces;

        public PipelineLayoutCache()
        {
            _plces = new ConcurrentDictionary<PlceKey, PipelineLayoutCacheEntry>();
        }

        public PipelineLayoutCacheEntry GetOrCreate(
            VulkanRenderer gd,
            Device device,
            ReadOnlyCollection<ResourceDescriptorCollection> setDescriptors,
            bool usePushDescriptors)
        {
            PlceKey key = new(setDescriptors, usePushDescriptors);

            return _plces.GetOrAdd(key, newKey => new PipelineLayoutCacheEntry(gd, device, setDescriptors, usePushDescriptors));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (PipelineLayoutCacheEntry plce in _plces.Values)
                {
                    plce.Dispose();
                }

                _plces.Clear();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}

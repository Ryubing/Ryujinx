using Ryujinx.Graphics.GAL;
using Silk.NET.Vulkan;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Ryujinx.Graphics.Vulkan
{
    class DescriptorSetTemplate : IDisposable
    {
        /// <summary>
        /// Renderdoc seems to crash when doing a templated uniform update with count > 1 on a push descriptor.
        /// When this is true, consecutive buffers are always updated individually.
        /// </summary>
        private const bool RenderdocPushCountBug = true;

        private readonly VulkanRenderer _gd;
        private readonly Device _device;

        public readonly DescriptorUpdateTemplate Template;
        public readonly int Size;

        public unsafe DescriptorSetTemplate(
            VulkanRenderer gd,
            Device device,
            ResourceBindingSegment[] segments,
            PipelineLayoutCacheEntry plce,
            PipelineBindPoint pbp,
            int setIndex)
        {
            _gd = gd;
            _device = device;

            // Create a template from the set usages. Assumes the descriptor set is updated in segment order then binding order.

            DescriptorUpdateTemplateEntry* entries = stackalloc DescriptorUpdateTemplateEntry[segments.Length];
            nuint structureOffset = 0;

            for (int seg = 0; seg < segments.Length; seg++)
            {
                ResourceBindingSegment segment = segments[seg];

                int binding = segment.Binding;
                int count = segment.Count;

                if (IsBufferType(segment.Type))
                {
                    entries[seg] = new DescriptorUpdateTemplateEntry()
                    {
                        DescriptorType = segment.Type.Convert(),
                        DstBinding = (uint)binding,
                        DescriptorCount = (uint)count,
                        Offset = structureOffset,
                        Stride = (nuint)Unsafe.SizeOf<DescriptorBufferInfo>()
                    };

                    structureOffset += (nuint)(Unsafe.SizeOf<DescriptorBufferInfo>() * count);
                }
                else if (IsBufferTextureType(segment.Type))
                {
                    entries[seg] = new DescriptorUpdateTemplateEntry()
                    {
                        DescriptorType = segment.Type.Convert(),
                        DstBinding = (uint)binding,
                        DescriptorCount = (uint)count,
                        Offset = structureOffset,
                        Stride = (nuint)Unsafe.SizeOf<BufferView>()
                    };

                    structureOffset += (nuint)(Unsafe.SizeOf<BufferView>() * count);
                }
                else
                {
                    entries[seg] = new DescriptorUpdateTemplateEntry()
                    {
                        DescriptorType = segment.Type.Convert(),
                        DstBinding = (uint)binding,
                        DescriptorCount = (uint)count,
                        Offset = structureOffset,
                        Stride = (nuint)Unsafe.SizeOf<DescriptorImageInfo>()
                    };

                    structureOffset += (nuint)(Unsafe.SizeOf<DescriptorImageInfo>() * count);
                }
            }

            Size = (int)structureOffset;

            DescriptorUpdateTemplateCreateInfo info = new()
            {
                SType = StructureType.DescriptorUpdateTemplateCreateInfo,
                DescriptorUpdateEntryCount = (uint)segments.Length,
                PDescriptorUpdateEntries = entries,

                TemplateType = DescriptorUpdateTemplateType.DescriptorSet,
                DescriptorSetLayout = plce.DescriptorSetLayouts[setIndex],
                PipelineBindPoint = pbp,
                PipelineLayout = plce.PipelineLayout,
                Set = (uint)setIndex,
            };

            DescriptorUpdateTemplate result;
            gd.Api.CreateDescriptorUpdateTemplate(device, &info, null, &result).ThrowOnError();

            Template = result;
        }

        public unsafe DescriptorSetTemplate(
            VulkanRenderer gd,
            Device device,
            ResourceDescriptorCollection descriptors,
            long updateMask,
            PipelineLayoutCacheEntry plce,
            PipelineBindPoint pbp,
            int setIndex)
        {
            _gd = gd;
            _device = device;

            // Create a template from the set usages. Assumes the descriptor set is updated in segment order then binding order.
            int segmentCount = BitOperations.PopCount((ulong)updateMask);

            DescriptorUpdateTemplateEntry* entries = stackalloc DescriptorUpdateTemplateEntry[segmentCount];
            int entry = 0;
            nuint structureOffset = 0;

            void AddBinding(int binding, int count)
            {
                entries[entry++] = new DescriptorUpdateTemplateEntry()
                {
                    DescriptorType = DescriptorType.UniformBuffer,
                    DstBinding = (uint)binding,
                    DescriptorCount = (uint)count,
                    Offset = structureOffset,
                    Stride = (nuint)Unsafe.SizeOf<DescriptorBufferInfo>()
                };

                structureOffset += (nuint)(Unsafe.SizeOf<DescriptorBufferInfo>() * count);
            }

            int startBinding = 0;
            int bindingCount = 0;

            foreach (ResourceDescriptor descriptor in descriptors.Descriptors)
            {
                for (int i = 0; i < descriptor.Count; i++)
                {
                    int binding = descriptor.Binding + i;

                    if ((updateMask & (1L << binding)) != 0)
                    {
                        if (bindingCount > 0 && (RenderdocPushCountBug || startBinding + bindingCount != binding))
                        {
                            AddBinding(startBinding, bindingCount);

                            bindingCount = 0;
                        }

                        if (bindingCount == 0)
                        {
                            startBinding = binding;
                        }

                        bindingCount++;
                    }
                }
            }

            if (bindingCount > 0)
            {
                AddBinding(startBinding, bindingCount);
            }

            Size = (int)structureOffset;

            DescriptorUpdateTemplateCreateInfo info = new()
            {
                SType = StructureType.DescriptorUpdateTemplateCreateInfo,
                DescriptorUpdateEntryCount = (uint)entry,
                PDescriptorUpdateEntries = entries,

                TemplateType = DescriptorUpdateTemplateType.PushDescriptorsKhr,
                DescriptorSetLayout = plce.DescriptorSetLayouts[setIndex],
                PipelineBindPoint = pbp,
                PipelineLayout = plce.PipelineLayout,
                Set = (uint)setIndex,
            };

            DescriptorUpdateTemplate result;
            gd.Api.CreateDescriptorUpdateTemplate(device, &info, null, &result).ThrowOnError();

            Template = result;
        }

        private static bool IsBufferType(ResourceType type)
        {
            return type == ResourceType.UniformBuffer || type == ResourceType.StorageBuffer;
        }

        private static bool IsBufferTextureType(ResourceType type)
        {
            return type == ResourceType.BufferTexture || type == ResourceType.BufferImage;
        }

        public unsafe void Dispose()
        {
            _gd.Api.DestroyDescriptorUpdateTemplate(_device, Template, null);
        }
    }
}

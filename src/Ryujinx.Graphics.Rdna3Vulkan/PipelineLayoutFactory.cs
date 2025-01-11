using Ryujinx.Common.Memory;
using Ryujinx.Graphics.GAL;
using Silk.NET.Vulkan;
using System;
using System.Collections.ObjectModel;

namespace Ryujinx.Graphics.Rdna3Vulkan
{
    record struct ResourceLayouts(DescriptorSetLayout[] DescriptorSetLayouts, bool[] DescriptorSetLayoutsUpdateAfterBind, PipelineLayout PipelineLayout);

    static class PipelineLayoutFactory
    {
        public static unsafe ResourceLayouts Create(
            VulkanRenderer gd,
            Device device,
            ReadOnlyCollection<ResourceDescriptorCollection> setDescriptors,
            bool usePushDescriptors)
        {
            DescriptorSetLayout[] layouts = new DescriptorSetLayout[setDescriptors.Count];
            bool[] updateAfterBindFlags = new bool[setDescriptors.Count];

            for (int setIndex = 0; setIndex < setDescriptors.Count; setIndex++)
            {
                ResourceDescriptorCollection rdc = setDescriptors[setIndex];

                DescriptorSetLayoutBinding[] layoutBindings = new DescriptorSetLayoutBinding[rdc.Descriptors.Count];

                bool hasArray = false;

                for (int descIndex = 0; descIndex < rdc.Descriptors.Count; descIndex++)
                {
                    ResourceDescriptor descriptor = rdc.Descriptors[descIndex];
                    ResourceStages stages = descriptor.Stages;

                    layoutBindings[descIndex] = new DescriptorSetLayoutBinding
                    {
                        Binding = (uint)descriptor.Binding,
                        DescriptorType = descriptor.Type.Convert(),
                        DescriptorCount = (uint)descriptor.Count,
                        StageFlags = stages.Convert(),
                    };

                    if (descriptor.Count > 1)
                    {
                        hasArray = true;
                    }
                }

                fixed (DescriptorSetLayoutBinding* pLayoutBindings = layoutBindings)
                {
                    DescriptorSetLayoutCreateFlags flags = DescriptorSetLayoutCreateFlags.None;

                    if (usePushDescriptors && setIndex == 0)
                    {
                        flags = DescriptorSetLayoutCreateFlags.PushDescriptorBitKhr;
                    }

                    var descriptorSetLayoutCreateInfo = new DescriptorSetLayoutCreateInfo
                    {
                        SType = StructureType.DescriptorSetLayoutCreateInfo,
                        PBindings = pLayoutBindings,
                        BindingCount = (uint)layoutBindings.Length,
                        Flags = flags,
                    };

                    gd.Api.CreateDescriptorSetLayout(device, in descriptorSetLayoutCreateInfo, null, out layouts[setIndex]).ThrowOnError();
                }
            }

            PipelineLayout layout;

            fixed (DescriptorSetLayout* pLayouts = layouts)
            {
                var pipelineLayoutCreateInfo = new PipelineLayoutCreateInfo
                {
                    SType = StructureType.PipelineLayoutCreateInfo,
                    PSetLayouts = pLayouts,
                    SetLayoutCount = (uint)layouts.Length,
                };

                gd.Api.CreatePipelineLayout(device, &pipelineLayoutCreateInfo, null, out layout).ThrowOnError();
            }

            return new ResourceLayouts(layouts, updateAfterBindFlags, layout);
        }
    }
}

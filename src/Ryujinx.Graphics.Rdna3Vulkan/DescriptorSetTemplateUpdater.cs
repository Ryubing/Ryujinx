using Ryujinx.Common;
using Silk.NET.Vulkan;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Rdna3Vulkan
{
    ref struct DescriptorSetTemplateWriter
    {
        private Span<byte> _data;

        public DescriptorSetTemplateWriter(Span<byte> data)
        {
            _data = data;
        }

        public void Push<T>(ReadOnlySpan<T> values) where T : unmanaged
        {
            Span<T> target = MemoryMarshal.Cast<byte, T>(_data);

            values.CopyTo(target);

            _data = _data[(Unsafe.SizeOf<T>() * values.Length)..];
        }
    }

    unsafe class DescriptorSetTemplateUpdater : IDisposable
    {
        private const int SizeGranularity = 512;

        private DescriptorSetTemplate _activeTemplate;
        private NativeArray<byte> _data;

        private void EnsureSize(int size)
        {
            if (_data == null || _data.Length < size)
            {
                _data?.Dispose();

                int dataSize = BitUtils.AlignUp(size, SizeGranularity);
                _data = new NativeArray<byte>(dataSize);
            }
        }

        public DescriptorSetTemplateWriter Begin(DescriptorSetTemplate template)
        {
            _activeTemplate = template;

            EnsureSize(template.Size);

            return new DescriptorSetTemplateWriter(new Span<byte>(_data.Pointer, template.Size));
        }

        public DescriptorSetTemplateWriter Begin(int maxSize)
        {
            EnsureSize(maxSize);

            return new DescriptorSetTemplateWriter(new Span<byte>(_data.Pointer, maxSize));
        }

        public void Commit(VulkanRenderer gd, Device device, DescriptorSet set)
        {
            gd.Api.UpdateDescriptorSetWithTemplate(device, set, _activeTemplate.Template, _data.Pointer);
        }

        public void CommitPushDescriptor(VulkanRenderer gd, CommandBufferScoped cbs, DescriptorSetTemplate template, PipelineLayout layout)
        {
            gd.PushDescriptorApi.CmdPushDescriptorSetWithTemplate(cbs.CommandBuffer, template.Template, layout, 0, _data.Pointer);
        }

        public void Dispose()
        {
            _data?.Dispose();
        }
    }
}

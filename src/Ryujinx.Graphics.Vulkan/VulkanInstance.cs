using Ryujinx.Common.Utilities;
using Silk.NET.Core;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Vulkan
{
    class VulkanInstance : IDisposable
    {
        private readonly Vk _api;
        public readonly Instance Instance;
        public readonly Version32 InstanceVersion;

        private bool _disposed;

        private VulkanInstance(Vk api, Instance instance)
        {
            _api = api;
            Instance = instance;

            if (api.GetInstanceProcAddr(instance, "vkEnumerateInstanceVersion") == nint.Zero)
            {
                InstanceVersion = Vk.Version10;
            }
            else
            {
                uint rawInstanceVersion = 0;

                if (api.EnumerateInstanceVersion(ref rawInstanceVersion) != Result.Success)
                {
                    rawInstanceVersion = Vk.Version11.Value;
                }

                InstanceVersion = (Version32)rawInstanceVersion;
            }
        }

        public static Result Create(Vk api, ref InstanceCreateInfo createInfo, out VulkanInstance instance)
        {
            instance = null;

            Instance rawInstance = default;

            Result result = api.CreateInstance(SpanHelpers.AsReadOnlySpan(ref createInfo), [], SpanHelpers.AsSpan(ref rawInstance));

            if (result == Result.Success)
            {
                instance = new VulkanInstance(api, rawInstance);
            }

            return result;
        }

        public Result EnumeratePhysicalDevices(out VulkanPhysicalDevice[] physicalDevices)
        {
            physicalDevices = null;

            uint physicalDeviceCount = 0;

            Result result = _api.EnumeratePhysicalDevices(Instance, SpanHelpers.AsSpan(ref physicalDeviceCount), []);

            if (result != Result.Success)
            {
                return result;
            }

            PhysicalDevice[] rawPhysicalDevices = new PhysicalDevice[physicalDeviceCount];

            result = _api.EnumeratePhysicalDevices(Instance, SpanHelpers.AsSpan(ref physicalDeviceCount), rawPhysicalDevices);

            if (result != Result.Success)
            {
                return result;
            }

            physicalDevices = rawPhysicalDevices.Select(x => new VulkanPhysicalDevice(_api, x)).ToArray();

            return Result.Success;
        }

        public static IReadOnlySet<string> GetInstanceExtensions(Vk api)
        {
            uint propertiesCount = 0;

            api.EnumerateInstanceExtensionProperties([], SpanHelpers.AsSpan(ref propertiesCount), []).ThrowOnError();

            ExtensionProperties[] extensionProperties = new ExtensionProperties[propertiesCount];

            api.EnumerateInstanceExtensionProperties([], SpanHelpers.AsSpan(ref propertiesCount), extensionProperties).ThrowOnError();

            unsafe
            {
                return extensionProperties.Select(x => Marshal.PtrToStringAnsi((nint)x.ExtensionName)).ToImmutableHashSet();
            }
        }

        public static IReadOnlySet<string> GetInstanceLayers(Vk api)
        {
            uint propertiesCount = 0;

            api.EnumerateInstanceLayerProperties(SpanHelpers.AsSpan(ref propertiesCount), []).ThrowOnError();

            LayerProperties[] layerProperties = new LayerProperties[propertiesCount];

            api.EnumerateInstanceLayerProperties(SpanHelpers.AsSpan(ref propertiesCount), layerProperties).ThrowOnError();

            unsafe
            {
                return layerProperties.Select(x => Marshal.PtrToStringAnsi((nint)x.LayerName)).ToImmutableHashSet();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _api.DestroyInstance(Instance, []);

                _disposed = true;
            }
        }
    }
}

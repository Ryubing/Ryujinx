using Silk.NET.Vulkan;
using System;

namespace Ryujinx.Graphics.Vulkan
{
    static class ResultExtensions
    {
        public static bool IsError(this Result result)
        {
            // Only negative result codes are errors.
            return result < Result.Success;
        }

        public static void ThrowOnError(this Result result)
        {
            // Only negative result codes are errors.
            if (result.IsError())
            {
                throw new VulkanException(result);
            }
        }
    }

    class VulkanException : Exception
    {
        public VulkanException()
        {
        }

        public VulkanException(Result result) : base($"Unexpected API error \"{result}\".")
        {
        }

        public VulkanException(string message) : base(message)
        {
        }

        public VulkanException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

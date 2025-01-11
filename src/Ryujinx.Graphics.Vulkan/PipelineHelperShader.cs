using Silk.NET.Vulkan;
using VkFormat = Silk.NET.Vulkan.Format;

namespace Ryujinx.Graphics.Vulkan
{
    class PipelineHelperShader : PipelineBase
    {
        public PipelineHelperShader(VulkanRenderer gd, Device device) : base(gd, device)
        {
        }

        public void SetRenderTarget(TextureView view, uint width, uint height, bool signalChange = true)
        {
            CreateFramebuffer(view, width, height);
            CreateRenderPass();

            if (signalChange)
            {
                SignalStateChange();
            }
        }

        private void CreateFramebuffer(TextureView view, uint width, uint height)
        {
            FramebufferParams = new FramebufferParams(Device, view, width, height);
            UpdatePipelineAttachmentFormats();
        }

        public void SetCommandBuffer(CommandBufferScoped cbs)
        {
            CommandBuffer = (Cbs = cbs).CommandBuffer;

            // Restore per-command buffer state.

            if (Pipeline != null)
            {
                Gd.Api.CmdBindPipeline(CommandBuffer, Pbp, Pipeline.Get(CurrentCommandBuffer).Value);
            }

            SignalCommandBufferChange();
        }

        public void Finish()
        {
            EndRenderPass();
        }

        public void Finish(VulkanRenderer gd, CommandBufferScoped cbs)
        {
            Finish();

            if (gd.PipelineInternal.IsCommandBufferActive(cbs.CommandBuffer))
            {
                gd.PipelineInternal.Restore();
            }
        }
    }
}

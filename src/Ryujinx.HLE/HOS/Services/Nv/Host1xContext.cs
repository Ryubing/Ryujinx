using Ryujinx.Graphics.Device;
using Ryujinx.Graphics.Host1x;
using Ryujinx.Graphics.Nvdec;
using Ryujinx.Graphics.Vic;
using System;
using GpuContext = Ryujinx.Graphics.Gpu.GpuContext;

namespace Ryujinx.HLE.HOS.Services.Nv
{
    class Host1xContext : IDisposable
    {
        public DeviceMemoryManager Smmu { get; }
        public NvMemoryAllocator MemoryAllocator { get; }
        public Host1xDevice Host1x { get; }

        public Host1xContext(GpuContext gpu, ulong pid)
        {
            MemoryAllocator = new NvMemoryAllocator();
            Host1x = new Host1xDevice(gpu.Synchronization);
            Smmu = gpu.CreateDeviceMemoryManager(pid);
            NvdecDevice nvdec = new(Smmu);
            VicDevice vic = new(Smmu);
            Host1x.RegisterDevice(ClassId.Nvdec, nvdec);
            Host1x.RegisterDevice(ClassId.Vic, vic);
        }

        public void Dispose()
        {
            Host1x.Dispose();
        }
    }
}

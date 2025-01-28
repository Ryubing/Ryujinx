using Ryujinx.Common.Memory;
using Ryujinx.Graphics.Device;
using Ryujinx.Graphics.Vic.Image;
using Ryujinx.Graphics.Vic.Types;
using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Vic
{
    public class VicDevice : IDeviceState
    {
        private readonly DeviceMemoryManager _mm;
        private readonly ResourceManager _rm;
        private readonly DeviceState<VicRegisters> _state;

        public VicDevice(DeviceMemoryManager mm)
        {
            _mm = mm;
            _rm = new ResourceManager(mm, new BufferPool<Pixel>(), new BufferPool<byte>());
            _state = new DeviceState<VicRegisters>(new Dictionary<string, RwCallback>
            {
                { nameof(VicRegisters.Execute), new RwCallback(Execute, null) },
            });
        }

        public int Read(int offset) => _state.Read(offset);
        public void Write(int offset, int data) => _state.Write(offset, data);

        private void Execute(int data)
        {
            ConfigStruct config = ReadIndirect<ConfigStruct>(_state.State.SetConfigStructOffset);

            using Surface output = new(
                _rm.SurfacePool,
                config.OutputSurfaceConfig.OutSurfaceWidth + 1,
                config.OutputSurfaceConfig.OutSurfaceHeight + 1);

            for (int i = 0; i < config.SlotStruct.Length; i++)
            {
                ref SlotStruct slot = ref config.SlotStruct[i];

                if (!slot.SlotConfig.SlotEnable)
                {
                    continue;
                }

                ref Array8<PlaneOffsets> offsets = ref _state.State.SetSurfacexSlotx[i];

                using Surface src = SurfaceReader.Read(_rm, ref slot.SlotConfig, ref slot.SlotSurfaceConfig, ref offsets);

                int x1 = config.OutputConfig.TargetRectLeft;
                int y1 = config.OutputConfig.TargetRectTop;
                int x2 = config.OutputConfig.TargetRectRight + 1;
                int y2 = config.OutputConfig.TargetRectBottom + 1;

                int targetX = Math.Min(x1, x2);
                int targetY = Math.Min(y1, y2);
                int targetW = Math.Min(output.Width - targetX, Math.Abs(x2 - x1));
                int targetH = Math.Min(output.Height - targetY, Math.Abs(y2 - y1));

                Rectangle targetRect = new(targetX, targetY, targetW, targetH);

                Blender.BlendOne(output, src, ref slot, targetRect);
            }

            SurfaceWriter.Write(_rm, output, ref config.OutputSurfaceConfig, ref _state.State.SetOutputSurface);
        }

        private T ReadIndirect<T>(uint offset) where T : unmanaged
        {
            return _mm.Read<T>((ulong)offset << 8);
        }
    }
}

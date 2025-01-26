using Ryujinx.Common;
using Ryujinx.Graphics.GAL;
using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Gpu.Engine.Threed.Blender
{
    /// <summary>
    /// Advanced blend manager.
    /// </summary>
    class AdvancedBlendManager
    {
        private const int InstructionRamSize = 128;
        private const int InstructionRamSizeMask = InstructionRamSize - 1;

        private readonly DeviceStateWithShadow<ThreedClassState> _state;

        private readonly uint[] _code;
        private int _ip;

        /// <summary>
        /// Creates a new instance of the advanced blend manager.
        /// </summary>
        /// <param name="state">GPU state of the channel owning this manager</param>
        public AdvancedBlendManager(DeviceStateWithShadow<ThreedClassState> state)
        {
            _state = state;
            _code = new uint[InstructionRamSize];
        }

        /// <summary>
        /// Sets the start offset of the blend microcode in memory.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        public void LoadBlendUcodeStart(int argument)
        {
            _ip = argument;
        }

        /// <summary>
        /// Pushes one word of blend microcode.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        public void LoadBlendUcodeInstruction(int argument)
        {
            _code[_ip++ & InstructionRamSizeMask] = (uint)argument;
        }

        /// <summary>
        /// Tries to identify the current advanced blend function being used,
        /// given the current state and microcode that was uploaded.
        /// </summary>
        /// <param name="descriptor">Advanced blend descriptor</param>
        /// <returns>True if the function was found, false otherwise</returns>
        public bool TryGetAdvancedBlend(out AdvancedBlendDescriptor descriptor)
        {
            Span<uint> currentCode = new(_code);
            byte codeLength = (byte)_state.State.BlendUcodeSize;

            if (currentCode.Length > codeLength)
            {
                currentCode = currentCode[..codeLength];
            }

            Hash128 hash = Hash128.ComputeHash(MemoryMarshal.Cast<uint, byte>(currentCode));

            descriptor = default;

            if (!AdvancedBlendPreGenTable.Entries.TryGetValue(hash, out AdvancedBlendEntry entry))
            {
                return false;
            }

            if (entry.Constants != null)
            {
                bool constantsMatch = true;

                for (int i = 0; i < entry.Constants.Length; i++)
                {
                    RgbFloat constant = entry.Constants[i];
                    RgbHalf constant2 = _state.State.BlendUcodeConstants[i];

                    if ((Half)constant.R != constant2.UnpackR() ||
                        (Half)constant.G != constant2.UnpackG() ||
                        (Half)constant.B != constant2.UnpackB())
                    {
                        constantsMatch = false;
                        break;
                    }
                }

                if (!constantsMatch)
                {
                    return false;
                }
            }

            if (entry.Alpha.Enable != _state.State.BlendUcodeEnable)
            {
                return false;
            }

            if (entry.Alpha.Enable == BlendUcodeEnable.EnableRGBA &&
                (entry.Alpha.AlphaOp != _state.State.BlendStateCommon.AlphaOp ||
                entry.Alpha.AlphaSrcFactor != _state.State.BlendStateCommon.AlphaSrcFactor ||
                entry.Alpha.AlphaDstFactor != _state.State.BlendStateCommon.AlphaDstFactor))
            {
                return false;
            }

            descriptor = new AdvancedBlendDescriptor(entry.Op, entry.Overlap, entry.SrcPreMultiplied);
            return true;
        }
    }
}

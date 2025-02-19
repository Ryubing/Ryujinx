using Ryujinx.Graphics.Shader.Decoders;
using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.StructuredIr;
using Ryujinx.Graphics.Shader.Translation;
using System;
using System.Collections.Generic;
using System.Numerics;
using static Ryujinx.Graphics.Shader.Instructions.InstEmitHelper;
using static Ryujinx.Graphics.Shader.IntermediateRepresentation.OperandHelper;

namespace Ryujinx.Graphics.Shader.Instructions
{
    static partial class InstEmit
    {
        public static void SuatomB(EmitterContext context)
        {
            InstSuatomB op = context.GetOp<InstSuatomB>();

            EmitSuatom(
                context,
                op.Dim,
                op.Op,
                op.Size,
                0,
                op.SrcA,
                op.SrcB,
                op.SrcC,
                op.Dest,
                op.Ba,
                isBindless: true,
                compareAndSwap: false);
        }

        public static void Suatom(EmitterContext context)
        {
            InstSuatom op = context.GetOp<InstSuatom>();

            EmitSuatom(
                context,
                op.Dim,
                op.Op,
                op.Size,
                op.TidB,
                op.SrcA,
                op.SrcB,
                0,
                op.Dest,
                op.Ba,
                isBindless: false,
                compareAndSwap: false);
        }

        public static void SuatomB2(EmitterContext context)
        {
            InstSuatomB2 op = context.GetOp<InstSuatomB2>();

            EmitSuatom(
                context,
                op.Dim,
                op.Op,
                op.Size,
                0,
                op.SrcA,
                op.SrcB,
                op.SrcC,
                op.Dest,
                op.Ba,
                isBindless: true,
                compareAndSwap: false);
        }

        public static void SuatomCasB(EmitterContext context)
        {
            InstSuatomCasB op = context.GetOp<InstSuatomCasB>();

            EmitSuatom(
                context,
                op.Dim,
                0,
                op.Size,
                0,
                op.SrcA,
                op.SrcB,
                op.SrcC,
                op.Dest,
                op.Ba,
                isBindless: true,
                compareAndSwap: true);
        }

        public static void SuatomCas(EmitterContext context)
        {
            InstSuatomCas op = context.GetOp<InstSuatomCas>();

            EmitSuatom(
                context,
                op.Dim,
                0,
                op.Size,
                op.TidB,
                op.SrcA,
                op.SrcB,
                0,
                op.Dest,
                op.Ba,
                isBindless: false,
                compareAndSwap: true);
        }

        public static void SuldDB(EmitterContext context)
        {
            InstSuldDB op = context.GetOp<InstSuldDB>();

            EmitSuld(context, op.CacheOp, op.Dim, op.Size, 0, 0, op.SrcA, op.Dest, op.SrcC, useComponents: false, op.Ba, isBindless: true);
        }

        public static void SuldD(EmitterContext context)
        {
            InstSuldD op = context.GetOp<InstSuldD>();

            EmitSuld(context, op.CacheOp, op.Dim, op.Size, op.TidB, 0, op.SrcA, op.Dest, 0, useComponents: false, op.Ba, isBindless: false);
        }

        public static void SuldB(EmitterContext context)
        {
            InstSuldB op = context.GetOp<InstSuldB>();

            EmitSuld(context, op.CacheOp, op.Dim, 0, 0, op.Rgba, op.SrcA, op.Dest, op.SrcC, useComponents: true, false, isBindless: true);
        }

        public static void Suld(EmitterContext context)
        {
            InstSuld op = context.GetOp<InstSuld>();

            EmitSuld(context, op.CacheOp, op.Dim, 0, op.TidB, op.Rgba, op.SrcA, op.Dest, 0, useComponents: true, false, isBindless: false);
        }

        public static void SuredB(EmitterContext context)
        {
            InstSuredB op = context.GetOp<InstSuredB>();

            EmitSured(context, op.Dim, op.Op, op.Size, 0, op.SrcA, op.Dest, op.SrcC, op.Ba, isBindless: true);
        }

        public static void Sured(EmitterContext context)
        {
            InstSured op = context.GetOp<InstSured>();

            EmitSured(context, op.Dim, op.Op, op.Size, op.TidB, op.SrcA, op.Dest, 0, op.Ba, isBindless: false);
        }

        public static void SustDB(EmitterContext context)
        {
            InstSustDB op = context.GetOp<InstSustDB>();

            EmitSust(context, op.CacheOp, op.Dim, op.Size, 0, 0, op.SrcA, op.Dest, op.SrcC, useComponents: false, op.Ba, isBindless: true);
        }

        public static void SustD(EmitterContext context)
        {
            InstSustD op = context.GetOp<InstSustD>();

            EmitSust(context, op.CacheOp, op.Dim, op.Size, op.TidB, 0, op.SrcA, op.Dest, 0, useComponents: false, op.Ba, isBindless: false);
        }

        public static void SustB(EmitterContext context)
        {
            InstSustB op = context.GetOp<InstSustB>();

            EmitSust(context, op.CacheOp, op.Dim, 0, 0, op.Rgba, op.SrcA, op.Dest, op.SrcC, useComponents: true, false, isBindless: true);
        }

        public static void Sust(EmitterContext context)
        {
            InstSust op = context.GetOp<InstSust>();

            EmitSust(context, op.CacheOp, op.Dim, 0, op.TidB, op.Rgba, op.SrcA, op.Dest, 0, useComponents: true, false, isBindless: false);
        }

        private static void EmitSuatom(
            EmitterContext context,
            SuDim dimensions,
            SuatomOp atomicOp,
            SuatomSize size,
            int imm,
            int srcA,
            int srcB,
            int srcC,
            int dest,
            bool byteAddress,
            bool isBindless,
            bool compareAndSwap)
        {
            SamplerType type = ConvertSamplerType(dimensions);

            if (type == SamplerType.None)
            {
                context.TranslatorContext.GpuAccessor.Log("Invalid image atomic sampler type.");
                return;
            }

            Operand Ra()
            {
                if (srcA > RegisterConsts.RegisterZeroIndex)
                {
                    return Const(0);
                }

                return context.Copy(Register(srcA++, RegisterType.Gpr));
            }

            Operand Rb()
            {
                if (srcB > RegisterConsts.RegisterZeroIndex)
                {
                    return Const(0);
                }

                return context.Copy(Register(srcB++, RegisterType.Gpr));
            }

            Operand d = Register(dest, RegisterType.Gpr);

            List<Operand> sourcesList = [];

            if (isBindless)
            {
                sourcesList.Add(context.Copy(GetSrcReg(context, srcC)));
            }

            int coordsCount = type.GetDimensions();

            for (int index = 0; index < coordsCount; index++)
            {
                sourcesList.Add(Ra());
            }

            if (Sample1DAs2D && (type & SamplerType.Mask) == SamplerType.Texture1D)
            {
                sourcesList.Add(Const(0));

                type &= ~SamplerType.Mask;
                type |= SamplerType.Texture2D;
            }

            if (type.HasFlag(SamplerType.Array))
            {
                sourcesList.Add(Ra());

                type |= SamplerType.Array;
            }

            if (byteAddress)
            {
                int xIndex = isBindless ? 1 : 0;

                sourcesList[xIndex] = context.ShiftRightS32(sourcesList[xIndex], Const(GetComponentSizeInBytesLog2(size)));
            }

            // TODO: FP and 64-bit formats.
            TextureFormat format = size == SuatomSize.Sd32 || size == SuatomSize.Sd64
                ? (isBindless ? TextureFormat.Unknown : ShaderProperties.GetTextureFormatAtomic(context.TranslatorContext.GpuAccessor, imm))
                : GetTextureFormat(size);

            if (compareAndSwap)
            {
                sourcesList.Add(Rb());
            }

            sourcesList.Add(Rb());

            Operand[] sources = sourcesList.ToArray();

            TextureFlags flags = compareAndSwap ? TextureFlags.CAS : GetAtomicOpFlags(atomicOp);

            if (isBindless)
            {
                flags |= TextureFlags.Bindless;
            }

            SetBindingPair setAndBinding = isBindless ? default : context.ResourceManager.GetTextureOrImageBinding(
                Instruction.ImageAtomic,
                type,
                format,
                flags,
                TextureOperation.DefaultCbufSlot,
                imm);

            Operand res = context.ImageAtomic(type, format, flags, setAndBinding, sources);

            context.Copy(d, res);
        }

        private static void EmitSuld(
            EmitterContext context,
            CacheOpLd cacheOp,
            SuDim dimensions,
            SuSize size,
            int imm,
            SuRgba componentMask,
            int srcA,
            int srcB,
            int srcC,
            bool useComponents,
            bool byteAddress,
            bool isBindless)
        {
            if (srcB == RegisterConsts.RegisterZeroIndex)
            {
                return;
            }

            SamplerType type = ConvertSamplerType(dimensions);

            if (type == SamplerType.None)
            {
                context.TranslatorContext.GpuAccessor.Log("Invalid image store sampler type.");
                return;
            }

            Operand Ra()
            {
                if (srcA > RegisterConsts.RegisterZeroIndex)
                {
                    return Const(0);
                }

                return context.Copy(Register(srcA++, RegisterType.Gpr));
            }

            List<Operand> sourcesList = [];

            if (isBindless)
            {
                sourcesList.Add(context.Copy(Register(srcC, RegisterType.Gpr)));
            }

            int coordsCount = type.GetDimensions();

            for (int index = 0; index < coordsCount; index++)
            {
                sourcesList.Add(Ra());
            }

            if (Sample1DAs2D && (type & SamplerType.Mask) == SamplerType.Texture1D)
            {
                sourcesList.Add(Const(0));

                type &= ~SamplerType.Mask;
                type |= SamplerType.Texture2D;
            }

            if (type.HasFlag(SamplerType.Array))
            {
                sourcesList.Add(Ra());
            }

            Operand[] sources = sourcesList.ToArray();

            int handle = imm;

            TextureFlags flags = isBindless ? TextureFlags.Bindless : TextureFlags.None;

            if (cacheOp == CacheOpLd.Cg)
            {
                flags |= TextureFlags.Coherent;
            }

            if (useComponents)
            {
                Operand[] dests = new Operand[BitOperations.PopCount((uint)componentMask)];

                int outputIndex = 0;

                for (int i = 0; i < dests.Length; i++)
                {
                    if (srcB + i >= RegisterConsts.RegisterZeroIndex)
                    {
                        break;
                    }

                    dests[outputIndex++] = Register(srcB + i, RegisterType.Gpr);
                }

                if (outputIndex != dests.Length)
                {
                    Array.Resize(ref dests, outputIndex);
                }

                TextureFormat format = isBindless ? TextureFormat.Unknown : ShaderProperties.GetTextureFormat(context.TranslatorContext.GpuAccessor, handle);

                SetBindingPair setAndBinding = isBindless ? default : context.ResourceManager.GetTextureOrImageBinding(
                    Instruction.ImageLoad,
                    type,
                    format,
                    flags,
                    TextureOperation.DefaultCbufSlot,
                    handle);

                context.ImageLoad(type, format, flags, setAndBinding, (int)componentMask, dests, sources);
            }
            else
            {
                if (byteAddress)
                {
                    int xIndex = isBindless ? 1 : 0;

                    sources[xIndex] = context.ShiftRightS32(sources[xIndex], Const(GetComponentSizeInBytesLog2(size)));
                }

                int components = GetComponents(size);
                int compMask = (1 << components) - 1;

                Operand[] dests = new Operand[components];

                int outputIndex = 0;

                for (int i = 0; i < dests.Length; i++)
                {
                    if (srcB + i >= RegisterConsts.RegisterZeroIndex)
                    {
                        break;
                    }

                    dests[outputIndex++] = Register(srcB + i, RegisterType.Gpr);
                }

                if (outputIndex != dests.Length)
                {
                    Array.Resize(ref dests, outputIndex);
                }

                TextureFormat format = GetTextureFormat(size);

                SetBindingPair setAndBinding = isBindless ? default : context.ResourceManager.GetTextureOrImageBinding(
                    Instruction.ImageLoad,
                    type,
                    format,
                    flags,
                    TextureOperation.DefaultCbufSlot,
                    handle);

                context.ImageLoad(type, format, flags, setAndBinding, compMask, dests, sources);

                switch (size)
                {
                    case SuSize.U8:
                        context.Copy(dests[0], ZeroExtendTo32(context, dests[0], 8));
                        break;
                    case SuSize.U16:
                        context.Copy(dests[0], ZeroExtendTo32(context, dests[0], 16));
                        break;
                    case SuSize.S8:
                        context.Copy(dests[0], SignExtendTo32(context, dests[0], 8));
                        break;
                    case SuSize.S16:
                        context.Copy(dests[0], SignExtendTo32(context, dests[0], 16));
                        break;
                }
            }
        }

        private static void EmitSured(
            EmitterContext context,
            SuDim dimensions,
            RedOp atomicOp,
            SuatomSize size,
            int imm,
            int srcA,
            int srcB,
            int srcC,
            bool byteAddress,
            bool isBindless)
        {
            SamplerType type = ConvertSamplerType(dimensions);

            if (type == SamplerType.None)
            {
                context.TranslatorContext.GpuAccessor.Log("Invalid image reduction sampler type.");
                return;
            }

            Operand Ra()
            {
                if (srcA > RegisterConsts.RegisterZeroIndex)
                {
                    return Const(0);
                }

                return context.Copy(Register(srcA++, RegisterType.Gpr));
            }

            Operand Rb()
            {
                if (srcB > RegisterConsts.RegisterZeroIndex)
                {
                    return Const(0);
                }

                return context.Copy(Register(srcB++, RegisterType.Gpr));
            }

            List<Operand> sourcesList = [];

            if (isBindless)
            {
                sourcesList.Add(context.Copy(GetSrcReg(context, srcC)));
            }

            int coordsCount = type.GetDimensions();

            for (int index = 0; index < coordsCount; index++)
            {
                sourcesList.Add(Ra());
            }

            if (Sample1DAs2D && (type & SamplerType.Mask) == SamplerType.Texture1D)
            {
                sourcesList.Add(Const(0));

                type &= ~SamplerType.Mask;
                type |= SamplerType.Texture2D;
            }

            if (type.HasFlag(SamplerType.Array))
            {
                sourcesList.Add(Ra());

                type |= SamplerType.Array;
            }

            if (byteAddress)
            {
                int xIndex = isBindless ? 1 : 0;

                sourcesList[xIndex] = context.ShiftRightS32(sourcesList[xIndex], Const(GetComponentSizeInBytesLog2(size)));
            }

            // TODO: FP and 64-bit formats.
            TextureFormat format = size == SuatomSize.Sd32 || size == SuatomSize.Sd64
                ? (isBindless ? TextureFormat.Unknown : ShaderProperties.GetTextureFormatAtomic(context.TranslatorContext.GpuAccessor, imm))
                : GetTextureFormat(size);

            sourcesList.Add(Rb());

            Operand[] sources = sourcesList.ToArray();

            TextureFlags flags = GetAtomicOpFlags((SuatomOp)atomicOp);

            if (isBindless)
            {
                flags |= TextureFlags.Bindless;
            }

            SetBindingPair setAndBinding = isBindless ? default : context.ResourceManager.GetTextureOrImageBinding(
                Instruction.ImageAtomic,
                type,
                format,
                flags,
                TextureOperation.DefaultCbufSlot,
                imm);

            context.ImageAtomic(type, format, flags, setAndBinding, sources);
        }

        private static void EmitSust(
            EmitterContext context,
            CacheOpSt cacheOp,
            SuDim dimensions,
            SuSize size,
            int imm,
            SuRgba componentMask,
            int srcA,
            int srcB,
            int srcC,
            bool useComponents,
            bool byteAddress,
            bool isBindless)
        {
            SamplerType type = ConvertSamplerType(dimensions);

            if (type == SamplerType.None)
            {
                context.TranslatorContext.GpuAccessor.Log("Invalid image store sampler type.");
                return;
            }

            Operand Ra()
            {
                if (srcA > RegisterConsts.RegisterZeroIndex)
                {
                    return Const(0);
                }

                return context.Copy(Register(srcA++, RegisterType.Gpr));
            }

            Operand Rb()
            {
                if (srcB > RegisterConsts.RegisterZeroIndex)
                {
                    return Const(0);
                }

                return context.Copy(Register(srcB++, RegisterType.Gpr));
            }

            List<Operand> sourcesList = [];

            if (isBindless)
            {
                sourcesList.Add(context.Copy(Register(srcC, RegisterType.Gpr)));
            }

            int coordsCount = type.GetDimensions();

            for (int index = 0; index < coordsCount; index++)
            {
                sourcesList.Add(Ra());
            }

            if (Sample1DAs2D && (type & SamplerType.Mask) == SamplerType.Texture1D)
            {
                sourcesList.Add(Const(0));

                type &= ~SamplerType.Mask;
                type |= SamplerType.Texture2D;
            }

            if (type.HasFlag(SamplerType.Array))
            {
                sourcesList.Add(Ra());
            }

            TextureFormat format = TextureFormat.Unknown;

            if (useComponents)
            {
                for (int compMask = (int)componentMask, compIndex = 0; compMask != 0; compMask >>= 1, compIndex++)
                {
                    if ((compMask & 1) != 0)
                    {
                        sourcesList.Add(Rb());
                    }
                }

                if (!isBindless)
                {
                    format = ShaderProperties.GetTextureFormat(context.TranslatorContext.GpuAccessor, imm);
                }
            }
            else
            {
                if (byteAddress)
                {
                    int xIndex = isBindless ? 1 : 0;

                    sourcesList[xIndex] = context.ShiftRightS32(sourcesList[xIndex], Const(GetComponentSizeInBytesLog2(size)));
                }

                int components = GetComponents(size);

                for (int compIndex = 0; compIndex < components; compIndex++)
                {
                    sourcesList.Add(Rb());
                }

                format = GetTextureFormat(size);
            }

            Operand[] sources = sourcesList.ToArray();

            int handle = imm;

            TextureFlags flags = isBindless ? TextureFlags.Bindless : TextureFlags.None;

            if (cacheOp == CacheOpSt.Cg)
            {
                flags |= TextureFlags.Coherent;
            }

            SetBindingPair setAndBinding = isBindless ? default : context.ResourceManager.GetTextureOrImageBinding(
                Instruction.ImageStore,
                type,
                format,
                flags,
                TextureOperation.DefaultCbufSlot,
                handle);

            context.ImageStore(type, format, flags, setAndBinding, sources);
        }

        private static int GetComponentSizeInBytesLog2(SuatomSize size)
        {
            return size switch
            {
                SuatomSize.U32 => 2,
                SuatomSize.S32 => 2,
                SuatomSize.U64 => 3,
                SuatomSize.F32FtzRn => 2,
                SuatomSize.F16x2FtzRn => 2,
                SuatomSize.S64 => 3,
                SuatomSize.Sd32 => 2,
                SuatomSize.Sd64 => 3,
                _ => 2,
            };
        }

        private static TextureFormat GetTextureFormat(SuatomSize size)
        {
            return size switch
            {
                SuatomSize.U32 => TextureFormat.R32Uint,
                SuatomSize.S32 => TextureFormat.R32Sint,
                SuatomSize.U64 => TextureFormat.R32G32Uint,
                SuatomSize.F32FtzRn => TextureFormat.R32Float,
                SuatomSize.F16x2FtzRn => TextureFormat.R16G16Float,
                SuatomSize.S64 => TextureFormat.R32G32Uint,
                SuatomSize.Sd32 => TextureFormat.R32Uint,
                SuatomSize.Sd64 => TextureFormat.R32G32Uint,
                _ => TextureFormat.R32Uint,
            };
        }

        private static TextureFlags GetAtomicOpFlags(SuatomOp op)
        {
            return op switch
            {
                SuatomOp.Add => TextureFlags.Add,
                SuatomOp.Min => TextureFlags.Minimum,
                SuatomOp.Max => TextureFlags.Maximum,
                SuatomOp.Inc => TextureFlags.Increment,
                SuatomOp.Dec => TextureFlags.Decrement,
                SuatomOp.And => TextureFlags.BitwiseAnd,
                SuatomOp.Or => TextureFlags.BitwiseOr,
                SuatomOp.Xor => TextureFlags.BitwiseXor,
                SuatomOp.Exch => TextureFlags.Swap,
                _ => TextureFlags.Add,
            };
        }

        private static int GetComponents(SuSize size)
        {
            return size switch
            {
                SuSize.B64 => 2,
                SuSize.B128 => 4,
                SuSize.UB128 => 4,
                _ => 1,
            };
        }

        private static int GetComponentSizeInBytesLog2(SuSize size)
        {
            return size switch
            {
                SuSize.U8 => 0,
                SuSize.S8 => 0,
                SuSize.U16 => 1,
                SuSize.S16 => 1,
                SuSize.B32 => 2,
                SuSize.B64 => 3,
                SuSize.B128 => 4,
                SuSize.UB128 => 4,
                _ => 2,
            };
        }

        private static TextureFormat GetTextureFormat(SuSize size)
        {
            return size switch
            {
                SuSize.U8 => TextureFormat.R8Uint,
                SuSize.S8 => TextureFormat.R8Sint,
                SuSize.U16 => TextureFormat.R16Uint,
                SuSize.S16 => TextureFormat.R16Sint,
                SuSize.B32 => TextureFormat.R32Uint,
                SuSize.B64 => TextureFormat.R32G32Uint,
                SuSize.B128 => TextureFormat.R32G32B32A32Uint,
                SuSize.UB128 => TextureFormat.R32G32B32A32Uint,
                _ => TextureFormat.R32Uint,
            };
        }

        private static SamplerType ConvertSamplerType(SuDim target)
        {
            return target switch
            {
                SuDim._1d => SamplerType.Texture1D,
                SuDim._1dBuffer => SamplerType.TextureBuffer,
                SuDim._1dArray => SamplerType.Texture1D | SamplerType.Array,
                SuDim._2d => SamplerType.Texture2D,
                SuDim._2dArray => SamplerType.Texture2D | SamplerType.Array,
                SuDim._3d => SamplerType.Texture3D,
                _ => SamplerType.None,
            };
        }
    }
}

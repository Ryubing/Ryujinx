using Ryujinx.Graphics.Shader.Decoders;
using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.Translation;
using System;
using System.Collections.Generic;
using System.Numerics;
using static Ryujinx.Graphics.Shader.IntermediateRepresentation.OperandHelper;

namespace Ryujinx.Graphics.Shader.Instructions
{
    static partial class InstEmit
    {
        private static readonly int[][] _maskLut = new int[][]
        {
            [0b0001, 0b0010, 0b0100, 0b1000, 0b0011, 0b1001, 0b1010, 0b1100], [0b0111, 0b1011, 0b1101, 0b1110, 0b1111, 0b0000, 0b0000, 0b0000
            ],
        };

        public const bool Sample1DAs2D = true;

        private enum TexsType
        {
            Texs,
            Tlds,
            Tld4s,
        }

        public static void Tex(EmitterContext context)
        {
            InstTex op = context.GetOp<InstTex>();

            EmitTex(context, TextureFlags.None, op.Dim, op.Lod, op.TidB, op.WMask, op.SrcA, op.SrcB, op.Dest, false, op.Dc, op.Aoffi);
        }

        public static void TexB(EmitterContext context)
        {
            InstTexB op = context.GetOp<InstTexB>();

            EmitTex(context, TextureFlags.Bindless, op.Dim, op.Lodb, 0, op.WMask, op.SrcA, op.SrcB, op.Dest, false, op.Dc, op.Aoffib);
        }

        public static void Texs(EmitterContext context)
        {
            InstTexs op = context.GetOp<InstTexs>();

            EmitTexs(context, TexsType.Texs, op.TidB, op.WMask, op.SrcA, op.SrcB, op.Dest, op.Dest2, isF16: false);
        }

        public static void TexsF16(EmitterContext context)
        {
            InstTexs op = context.GetOp<InstTexs>();

            EmitTexs(context, TexsType.Texs, op.TidB, op.WMask, op.SrcA, op.SrcB, op.Dest, op.Dest2, isF16: true);
        }

        public static void Tld(EmitterContext context)
        {
            InstTld op = context.GetOp<InstTld>();

            Lod lod = op.Lod ? Lod.Ll : Lod.Lz;

            EmitTex(context, TextureFlags.IntCoords, op.Dim, lod, op.TidB, op.WMask, op.SrcA, op.SrcB, op.Dest, op.Ms, false, op.Toff);
        }

        public static void TldB(EmitterContext context)
        {
            InstTldB op = context.GetOp<InstTldB>();

            TextureFlags flags = TextureFlags.IntCoords | TextureFlags.Bindless;
            Lod lod = op.Lod ? Lod.Ll : Lod.Lz;

            EmitTex(context, flags, op.Dim, lod, 0, op.WMask, op.SrcA, op.SrcB, op.Dest, op.Ms, false, op.Toff);
        }

        public static void Tlds(EmitterContext context)
        {
            InstTlds op = context.GetOp<InstTlds>();

            EmitTexs(context, TexsType.Tlds, op.TidB, op.WMask, op.SrcA, op.SrcB, op.Dest, op.Dest2, isF16: false);
        }

        public static void TldsF16(EmitterContext context)
        {
            InstTlds op = context.GetOp<InstTlds>();

            EmitTexs(context, TexsType.Tlds, op.TidB, op.WMask, op.SrcA, op.SrcB, op.Dest, op.Dest2, isF16: true);
        }

        public static void Tld4(EmitterContext context)
        {
            InstTld4 op = context.GetOp<InstTld4>();

            EmitTld4(context, op.Dim, op.TexComp, op.TidB, op.WMask, op.SrcA, op.SrcB, op.Dest, op.Toff, op.Dc, isBindless: false);
        }

        public static void Tld4B(EmitterContext context)
        {
            InstTld4B op = context.GetOp<InstTld4B>();

            EmitTld4(context, op.Dim, op.TexComp, 0, op.WMask, op.SrcA, op.SrcB, op.Dest, op.Toff, op.Dc, isBindless: true);
        }

        public static void Tld4s(EmitterContext context)
        {
            InstTld4s op = context.GetOp<InstTld4s>();

            EmitTexs(context, TexsType.Tld4s, op.TidB, 4, op.SrcA, op.SrcB, op.Dest, op.Dest2, isF16: false);
        }

        public static void Tld4sF16(EmitterContext context)
        {
            InstTld4s op = context.GetOp<InstTld4s>();

            EmitTexs(context, TexsType.Tld4s, op.TidB, 4, op.SrcA, op.SrcB, op.Dest, op.Dest2, isF16: true);
        }

        public static void Tmml(EmitterContext context)
        {
            InstTmml op = context.GetOp<InstTmml>();

            EmitTmml(context, op.Dim, op.TidB, op.WMask, op.SrcA, op.SrcB, op.Dest, isBindless: false);
        }

        public static void TmmlB(EmitterContext context)
        {
            InstTmmlB op = context.GetOp<InstTmmlB>();

            EmitTmml(context, op.Dim, 0, op.WMask, op.SrcA, op.SrcB, op.Dest, isBindless: true);
        }

        public static void Txd(EmitterContext context)
        {
            InstTxd op = context.GetOp<InstTxd>();

            EmitTxd(context, op.Dim, op.TidB, op.WMask, op.SrcA, op.SrcB, op.Dest, op.Toff, isBindless: false);
        }

        public static void TxdB(EmitterContext context)
        {
            InstTxdB op = context.GetOp<InstTxdB>();

            EmitTxd(context, op.Dim, 0, op.WMask, op.SrcA, op.SrcB, op.Dest, op.Toff, isBindless: true);
        }

        public static void Txq(EmitterContext context)
        {
            InstTxq op = context.GetOp<InstTxq>();

            EmitTxq(context, op.TexQuery, op.TidB, op.WMask, op.SrcA, op.Dest, isBindless: false);
        }

        public static void TxqB(EmitterContext context)
        {
            InstTxqB op = context.GetOp<InstTxqB>();

            EmitTxq(context, op.TexQuery, 0, op.WMask, op.SrcA, op.Dest, isBindless: true);
        }

        private static void EmitTex(
            EmitterContext context,
            TextureFlags flags,
            TexDim dimensions,
            Lod lodMode,
            int imm,
            int componentMask,
            int raIndex,
            int rbIndex,
            int rdIndex,
            bool isMultisample,
            bool hasDepthCompare,
            bool hasOffset)
        {
            if (rdIndex == RegisterConsts.RegisterZeroIndex)
            {
                return;
            }

            Operand Ra()
            {
                if (raIndex > RegisterConsts.RegisterZeroIndex)
                {
                    return Const(0);
                }

                return context.Copy(Register(raIndex++, RegisterType.Gpr));
            }

            Operand Rb()
            {
                if (rbIndex > RegisterConsts.RegisterZeroIndex)
                {
                    return Const(0);
                }

                return context.Copy(Register(rbIndex++, RegisterType.Gpr));
            }

            SamplerType type = ConvertSamplerType(dimensions);

            bool isArray = type.HasFlag(SamplerType.Array);
            bool isBindless = flags.HasFlag(TextureFlags.Bindless);

            Operand arrayIndex = isArray ? Ra() : null;

            List<Operand> sourcesList = [];

            if (isBindless)
            {
                sourcesList.Add(Rb());
            }

            bool hasLod = lodMode > Lod.Lz;

            if (type == SamplerType.Texture1D && (flags & ~TextureFlags.Bindless) == TextureFlags.IntCoords && !(
                hasLod ||
                hasDepthCompare ||
                hasOffset ||
                isArray ||
                isMultisample))
            {
                // For bindless, we don't have any way to know the texture type,
                // so we assume it's texture buffer when the sampler type is 1D, since that's more common.
                bool isTypeBuffer = isBindless || context.TranslatorContext.GpuAccessor.QuerySamplerType(imm) == SamplerType.TextureBuffer;
                if (isTypeBuffer)
                {
                    type = SamplerType.TextureBuffer;
                }
            }

            int coordsCount = type.GetDimensions();

            for (int index = 0; index < coordsCount; index++)
            {
                sourcesList.Add(Ra());
            }

            bool is1DTo2D = false;

            if (Sample1DAs2D && (type & SamplerType.Mask) == SamplerType.Texture1D)
            {
                sourcesList.Add(ConstF(0));

                type = SamplerType.Texture2D | (type & SamplerType.Array);
                is1DTo2D = true;
            }

            if (isArray)
            {
                sourcesList.Add(arrayIndex);
            }

            Operand lodValue = hasLod ? Rb() : ConstF(0);

            Operand packedOffs = hasOffset ? Rb() : null;

            if (hasDepthCompare)
            {
                sourcesList.Add(Rb());

                type |= SamplerType.Shadow;
            }

            if ((lodMode == Lod.Lz ||
                 lodMode == Lod.Ll ||
                 lodMode == Lod.Lla) && !isMultisample && type != SamplerType.TextureBuffer)
            {
                sourcesList.Add(lodValue);

                flags |= TextureFlags.LodLevel;
            }

            if (hasOffset)
            {
                for (int index = 0; index < coordsCount; index++)
                {
                    sourcesList.Add(context.BitfieldExtractS32(packedOffs, Const(index * 4), Const(4)));
                }

                if (is1DTo2D)
                {
                    sourcesList.Add(Const(0));
                }

                flags |= TextureFlags.Offset;
            }

            if (lodMode == Lod.Lb || lodMode == Lod.Lba)
            {
                sourcesList.Add(lodValue);

                flags |= TextureFlags.LodBias;
            }

            if (isMultisample)
            {
                sourcesList.Add(Rb());

                type |= SamplerType.Multisample;
            }

            Operand[] sources = sourcesList.ToArray();
            Operand[] dests = new Operand[BitOperations.PopCount((uint)componentMask)];

            int outputIndex = 0;

            for (int i = 0; i < dests.Length; i++)
            {
                if (rdIndex + i >= RegisterConsts.RegisterZeroIndex)
                {
                    break;
                }

                dests[outputIndex++] = Register(rdIndex + i, RegisterType.Gpr);
            }

            if (outputIndex != dests.Length)
            {
                Array.Resize(ref dests, outputIndex);
            }

            int handle = !isBindless ? imm : 0;

            EmitTextureSample(context, type, flags, handle, componentMask, dests, sources);
        }

        private static void EmitTexs(
            EmitterContext context,
            TexsType texsType,
            int imm,
            int writeMask,
            int srcA,
            int srcB,
            int dest,
            int dest2,
            bool isF16)
        {
            if (dest == RegisterConsts.RegisterZeroIndex && dest2 == RegisterConsts.RegisterZeroIndex)
            {
                return;
            }

            List<Operand> sourcesList = [];

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

            void AddTextureOffset(int coordsCount, int stride, int size)
            {
                Operand packedOffs = Rb();

                for (int index = 0; index < coordsCount; index++)
                {
                    sourcesList.Add(context.BitfieldExtractS32(packedOffs, Const(index * stride), Const(size)));
                }
            }

            SamplerType type;
            TextureFlags flags;

            if (texsType == TexsType.Texs)
            {
                InstTexs texsOp = context.GetOp<InstTexs>();

                type = ConvertSamplerType(texsOp.Target);

                if (type == SamplerType.None)
                {
                    context.TranslatorContext.GpuAccessor.Log("Invalid texture sampler type.");
                    return;
                }

                flags = ConvertTextureFlags(texsOp.Target);

                // We don't need to handle 1D -> Buffer conversions here as
                // only texture sample with integer coordinates can ever use buffer targets.

                if ((type & SamplerType.Array) != 0)
                {
                    Operand arrayIndex = Ra();

                    sourcesList.Add(Ra());
                    sourcesList.Add(Rb());

                    sourcesList.Add(arrayIndex);

                    if ((type & SamplerType.Shadow) != 0)
                    {
                        sourcesList.Add(Rb());
                    }

                    if ((flags & TextureFlags.LodLevel) != 0)
                    {
                        sourcesList.Add(ConstF(0));
                    }
                }
                else
                {
                    switch (texsOp.Target)
                    {
                        case TexsTarget.Texture1DLodZero:
                            sourcesList.Add(Ra());

                            if (Sample1DAs2D)
                            {
                                sourcesList.Add(ConstF(0));

                                type &= ~SamplerType.Mask;
                                type |= SamplerType.Texture2D;
                            }

                            sourcesList.Add(ConstF(0));
                            break;

                        case TexsTarget.Texture2D:
                            sourcesList.Add(Ra());
                            sourcesList.Add(Rb());
                            break;

                        case TexsTarget.Texture2DLodZero:
                            sourcesList.Add(Ra());
                            sourcesList.Add(Rb());
                            sourcesList.Add(ConstF(0));
                            break;

                        case TexsTarget.Texture2DLodLevel:
                        case TexsTarget.Texture2DDepthCompare:
                        case TexsTarget.Texture3D:
                        case TexsTarget.TextureCube:
                            sourcesList.Add(Ra());
                            sourcesList.Add(Ra());
                            sourcesList.Add(Rb());
                            break;

                        case TexsTarget.Texture2DLodZeroDepthCompare:
                        case TexsTarget.Texture3DLodZero:
                            sourcesList.Add(Ra());
                            sourcesList.Add(Ra());
                            sourcesList.Add(Rb());
                            sourcesList.Add(ConstF(0));
                            break;

                        case TexsTarget.Texture2DLodLevelDepthCompare:
                        case TexsTarget.TextureCubeLodLevel:
                            sourcesList.Add(Ra());
                            sourcesList.Add(Ra());
                            sourcesList.Add(Rb());
                            sourcesList.Add(Rb());
                            break;
                    }
                }
            }
            else if (texsType == TexsType.Tlds)
            {
                InstTlds tldsOp = context.GetOp<InstTlds>();

                type = ConvertSamplerType(tldsOp.Target);

                if (type == SamplerType.None)
                {
                    context.TranslatorContext.GpuAccessor.Log("Invalid texel fetch sampler type.");
                    return;
                }

                flags = ConvertTextureFlags(tldsOp.Target) | TextureFlags.IntCoords;

                if (tldsOp.Target == TldsTarget.Texture1DLodZero &&
                    context.TranslatorContext.GpuAccessor.QuerySamplerType(tldsOp.TidB) == SamplerType.TextureBuffer)
                {
                    type = SamplerType.TextureBuffer;
                    flags &= ~TextureFlags.LodLevel;
                }

                switch (tldsOp.Target)
                {
                    case TldsTarget.Texture1DLodZero:
                        sourcesList.Add(Ra());

                        if (type != SamplerType.TextureBuffer)
                        {
                            if (Sample1DAs2D)
                            {
                                sourcesList.Add(ConstF(0));

                                type &= ~SamplerType.Mask;
                                type |= SamplerType.Texture2D;
                            }

                            sourcesList.Add(ConstF(0));
                        }
                        break;

                    case TldsTarget.Texture1DLodLevel:
                        sourcesList.Add(Ra());

                        if (Sample1DAs2D)
                        {
                            sourcesList.Add(ConstF(0));

                            type &= ~SamplerType.Mask;
                            type |= SamplerType.Texture2D;
                        }

                        sourcesList.Add(Rb());
                        break;

                    case TldsTarget.Texture2DLodZero:
                        sourcesList.Add(Ra());
                        sourcesList.Add(Rb());
                        sourcesList.Add(Const(0));
                        break;

                    case TldsTarget.Texture2DLodZeroOffset:
                        sourcesList.Add(Ra());
                        sourcesList.Add(Ra());
                        sourcesList.Add(Const(0));
                        break;

                    case TldsTarget.Texture2DLodZeroMultisample:
                    case TldsTarget.Texture2DLodLevel:
                    case TldsTarget.Texture2DLodLevelOffset:
                        sourcesList.Add(Ra());
                        sourcesList.Add(Ra());
                        sourcesList.Add(Rb());
                        break;

                    case TldsTarget.Texture3DLodZero:
                        sourcesList.Add(Ra());
                        sourcesList.Add(Ra());
                        sourcesList.Add(Rb());
                        sourcesList.Add(Const(0));
                        break;

                    case TldsTarget.Texture2DArrayLodZero:
                        sourcesList.Add(Rb());
                        sourcesList.Add(Rb());
                        sourcesList.Add(Ra());
                        sourcesList.Add(Const(0));
                        break;
                }

                if ((flags & TextureFlags.Offset) != 0)
                {
                    AddTextureOffset(type.GetDimensions(), 4, 4);
                }
            }
            else if (texsType == TexsType.Tld4s)
            {
                InstTld4s tld4sOp = context.GetOp<InstTld4s>();

                if (!(tld4sOp.Dc || tld4sOp.Aoffi))
                {
                    sourcesList.Add(Ra());
                    sourcesList.Add(Rb());
                }
                else
                {
                    sourcesList.Add(Ra());
                    sourcesList.Add(Ra());
                }

                type = SamplerType.Texture2D;
                flags = TextureFlags.Gather;

                int depthCompareIndex = sourcesList.Count;

                if (tld4sOp.Aoffi)
                {
                    AddTextureOffset(type.GetDimensions(), 8, 6);

                    flags |= TextureFlags.Offset;
                }

                if (tld4sOp.Dc)
                {
                    sourcesList.Insert(depthCompareIndex, Rb());

                    type |= SamplerType.Shadow;
                }
                else
                {
                    sourcesList.Add(Const((int)tld4sOp.TexComp));
                }
            }
            else
            {
                throw new ArgumentException($"Invalid TEXS type \"{texsType}\".");
            }

            Operand[] sources = sourcesList.ToArray();

            Operand[] rd0 = [ConstF(0), ConstF(0)];
            Operand[] rd1 = [ConstF(0), ConstF(0)];

            int handle = imm;
            int componentMask = _maskLut[dest2 == RegisterConsts.RegisterZeroIndex ? 0 : 1][writeMask];

            int componentsCount = BitOperations.PopCount((uint)componentMask);

            Operand[] dests = new Operand[componentsCount];

            int outputIndex = 0;

            for (int i = 0; i < componentsCount; i++)
            {
                int high = i >> 1;
                int low = i & 1;

                if (isF16)
                {
                    dests[outputIndex++] = high != 0
                        ? (rd1[low] = Local())
                        : (rd0[low] = Local());
                }
                else
                {
                    int rdIndex = high != 0 ? dest2 : dest;

                    if (rdIndex < RegisterConsts.RegisterZeroIndex)
                    {
                        rdIndex += low;
                    }

                    dests[outputIndex++] = Register(rdIndex, RegisterType.Gpr);
                }
            }

            if (outputIndex != dests.Length)
            {
                Array.Resize(ref dests, outputIndex);
            }

            EmitTextureSample(context, type, flags, handle, componentMask, dests, sources);

            if (isF16)
            {
                context.Copy(Register(dest, RegisterType.Gpr), context.PackHalf2x16(rd0[0], rd0[1]));
                context.Copy(Register(dest2, RegisterType.Gpr), context.PackHalf2x16(rd1[0], rd1[1]));
            }
        }

        private static void EmitTld4(
            EmitterContext context,
            TexDim dimensions,
            TexComp component,
            int imm,
            int componentMask,
            int srcA,
            int srcB,
            int dest,
            TexOffset offset,
            bool hasDepthCompare,
            bool isBindless)
        {
            if (dest == RegisterConsts.RegisterZeroIndex)
            {
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

            bool isArray =
                dimensions == TexDim.Array1d ||
                dimensions == TexDim.Array2d ||
                dimensions == TexDim.Array3d ||
                dimensions == TexDim.ArrayCube;

            Operand arrayIndex = isArray ? Ra() : null;

            List<Operand> sourcesList = [];

            SamplerType type = ConvertSamplerType(dimensions);
            TextureFlags flags = TextureFlags.Gather;

            if (isBindless)
            {
                sourcesList.Add(Rb());

                flags |= TextureFlags.Bindless;
            }

            int coordsCount = type.GetDimensions();

            for (int index = 0; index < coordsCount; index++)
            {
                sourcesList.Add(Ra());
            }

            bool is1DTo2D = Sample1DAs2D && (type & SamplerType.Mask) == SamplerType.Texture1D;

            if (is1DTo2D)
            {
                sourcesList.Add(ConstF(0));

                type = SamplerType.Texture2D | (type & SamplerType.Array);
            }

            if (isArray)
            {
                sourcesList.Add(arrayIndex);
            }

            Operand[] packedOffs = new Operand[2];

            bool hasAnyOffset = offset == TexOffset.Aoffi || offset == TexOffset.Ptp;

            packedOffs[0] = hasAnyOffset ? Rb() : null;
            packedOffs[1] = offset == TexOffset.Ptp ? Rb() : null;

            if (hasDepthCompare)
            {
                sourcesList.Add(Rb());

                type |= SamplerType.Shadow;
            }

            if (hasAnyOffset)
            {
                int offsetTexelsCount = offset == TexOffset.Ptp ? 4 : 1;

                for (int index = 0; index < coordsCount * offsetTexelsCount; index++)
                {
                    Operand packed = packedOffs[(index >> 2) & 1];

                    sourcesList.Add(context.BitfieldExtractS32(packed, Const((index & 3) * 8), Const(6)));
                }

                if (is1DTo2D)
                {
                    for (int index = 0; index < offsetTexelsCount; index++)
                    {
                        sourcesList.Add(Const(0));
                    }
                }

                flags |= offset == TexOffset.Ptp ? TextureFlags.Offsets : TextureFlags.Offset;
            }

            if (!hasDepthCompare)
            {
                sourcesList.Add(Const((int)component));
            }

            Operand[] sources = sourcesList.ToArray();
            Operand[] dests = new Operand[BitOperations.PopCount((uint)componentMask)];

            int outputIndex = 0;

            for (int i = 0; i < dests.Length; i++)
            {
                if (dest + i >= RegisterConsts.RegisterZeroIndex)
                {
                    break;
                }

                dests[outputIndex++] = Register(dest + i, RegisterType.Gpr);
            }

            if (outputIndex != dests.Length)
            {
                Array.Resize(ref dests, outputIndex);
            }

            EmitTextureSample(context, type, flags, imm, componentMask, dests, sources);
        }

        private static void EmitTmml(
            EmitterContext context,
            TexDim dimensions,
            int imm,
            int componentMask,
            int srcA,
            int srcB,
            int dest,
            bool isBindless)
        {
            if (dest == RegisterConsts.RegisterZeroIndex)
            {
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

            TextureFlags flags = TextureFlags.None;

            List<Operand> sourcesList = [];

            if (isBindless)
            {
                sourcesList.Add(Rb());

                flags |= TextureFlags.Bindless;
            }

            SamplerType type = ConvertSamplerType(dimensions);

            int coordsCount = type.GetDimensions();

            bool isArray =
                dimensions == TexDim.Array1d ||
                dimensions == TexDim.Array2d ||
                dimensions == TexDim.Array3d ||
                dimensions == TexDim.ArrayCube;

            Operand arrayIndex = isArray ? Ra() : null;

            for (int index = 0; index < coordsCount; index++)
            {
                sourcesList.Add(Ra());
            }

            if (Sample1DAs2D && (type & SamplerType.Mask) == SamplerType.Texture1D)
            {
                sourcesList.Add(ConstF(0));

                type = SamplerType.Texture2D | (type & SamplerType.Array);
            }

            if (isArray)
            {
                sourcesList.Add(arrayIndex);
            }

            Operand[] sources = sourcesList.ToArray();

            Operand GetDest()
            {
                if (dest >= RegisterConsts.RegisterZeroIndex)
                {
                    return null;
                }

                return Register(dest++, RegisterType.Gpr);
            }

            SetBindingPair setAndBinding = isBindless ? default : context.ResourceManager.GetTextureOrImageBinding(
                Instruction.Lod,
                type,
                TextureFormat.Unknown,
                flags,
                TextureOperation.DefaultCbufSlot,
                imm);

            for (int compMask = componentMask, compIndex = 0; compMask != 0; compMask >>= 1, compIndex++)
            {
                if ((compMask & 1) != 0)
                {
                    Operand d = GetDest();

                    if (d == null)
                    {
                        break;
                    }

                    // Components z and w aren't standard, we return 0 in this case and add a comment.
                    if (compIndex >= 2)
                    {
                        context.Add(new CommentNode("Unsupported component z or w found"));
                        context.Copy(d, Const(0));
                    }
                    else
                    {
                        // The instruction component order is the inverse of GLSL's.
                        Operand res = context.Lod(type, flags, setAndBinding, compIndex ^ 1, sources);

                        res = context.FPMultiply(res, ConstF(256.0f));

                        Operand fixedPointValue = context.FP32ConvertToS32(res);

                        context.Copy(d, fixedPointValue);
                    }
                }
            }
        }

        private static void EmitTxd(
            EmitterContext context,
            TexDim dimensions,
            int imm,
            int componentMask,
            int srcA,
            int srcB,
            int dest,
            bool hasOffset,
            bool isBindless)
        {
            if (dest == RegisterConsts.RegisterZeroIndex)
            {
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

            TextureFlags flags = TextureFlags.Derivatives;

            List<Operand> sourcesList = [];

            if (isBindless)
            {
                sourcesList.Add(Ra());

                flags |= TextureFlags.Bindless;
            }

            SamplerType type = ConvertSamplerType(dimensions);

            int coordsCount = type.GetDimensions();

            for (int index = 0; index < coordsCount; index++)
            {
                sourcesList.Add(Ra());
            }

            bool is1DTo2D = Sample1DAs2D && (type & SamplerType.Mask) == SamplerType.Texture1D;

            if (is1DTo2D)
            {
                sourcesList.Add(ConstF(0));

                type = SamplerType.Texture2D | (type & SamplerType.Array);
            }

            Operand packedParams = Ra();

            bool isArray =
                dimensions == TexDim.Array1d ||
                dimensions == TexDim.Array2d ||
                dimensions == TexDim.Array3d ||
                dimensions == TexDim.ArrayCube;

            if (isArray)
            {
                sourcesList.Add(context.BitwiseAnd(packedParams, Const(0xffff)));
            }

            // Derivatives (X and Y).
            for (int dIndex = 0; dIndex < 2 * coordsCount; dIndex++)
            {
                sourcesList.Add(Rb());

                if (is1DTo2D)
                {
                    sourcesList.Add(ConstF(0));
                }
            }

            if (hasOffset)
            {
                for (int index = 0; index < coordsCount; index++)
                {
                    sourcesList.Add(context.BitfieldExtractS32(packedParams, Const(16 + index * 4), Const(4)));
                }

                if (is1DTo2D)
                {
                    sourcesList.Add(Const(0));
                }

                flags |= TextureFlags.Offset;
            }

            Operand[] sources = sourcesList.ToArray();
            Operand[] dests = new Operand[BitOperations.PopCount((uint)componentMask)];

            int outputIndex = 0;

            for (int i = 0; i < dests.Length; i++)
            {
                if (dest + i >= RegisterConsts.RegisterZeroIndex)
                {
                    break;
                }

                dests[outputIndex++] = Register(dest + i, RegisterType.Gpr);
            }

            if (outputIndex != dests.Length)
            {
                Array.Resize(ref dests, outputIndex);
            }

            EmitTextureSample(context, type, flags, imm, componentMask, dests, sources);
        }

        private static void EmitTxq(
            EmitterContext context,
            TexQuery query,
            int imm,
            int componentMask,
            int srcA,
            int dest,
            bool isBindless)
        {
            if (dest == RegisterConsts.RegisterZeroIndex)
            {
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
                sourcesList.Add(Ra());
            }

            sourcesList.Add(Ra());

            Operand[] sources = sourcesList.ToArray();

            Operand GetDest()
            {
                if (dest >= RegisterConsts.RegisterZeroIndex)
                {
                    return null;
                }

                return Register(dest++, RegisterType.Gpr);
            }

            SamplerType type;

            if (isBindless)
            {
                if (query == TexQuery.TexHeaderTextureType)
                {
                    type = SamplerType.Texture2D | SamplerType.Multisample;
                }
                else
                {
                    type = (componentMask & 4) != 0 ? SamplerType.Texture3D : SamplerType.Texture2D;
                }
            }
            else
            {
                type = context.TranslatorContext.GpuAccessor.QuerySamplerType(imm);
            }

            TextureFlags flags = isBindless ? TextureFlags.Bindless : TextureFlags.None;
            SetBindingPair setAndBinding;

            switch (query)
            {
                case TexQuery.TexHeaderDimension:
                    setAndBinding = isBindless ? default : context.ResourceManager.GetTextureOrImageBinding(
                        Instruction.TextureQuerySize,
                        type,
                        TextureFormat.Unknown,
                        flags,
                        TextureOperation.DefaultCbufSlot,
                        imm);

                    for (int compMask = componentMask, compIndex = 0; compMask != 0; compMask >>= 1, compIndex++)
                    {
                        if ((compMask & 1) != 0)
                        {
                            Operand d = GetDest();

                            if (d == null)
                            {
                                break;
                            }

                            context.Copy(d, context.TextureQuerySize(type, flags, setAndBinding, compIndex, sources));
                        }
                    }
                    break;

                case TexQuery.TexHeaderTextureType:
                    setAndBinding = isBindless ? default : context.ResourceManager.GetTextureOrImageBinding(
                        Instruction.TextureQuerySamples,
                        type,
                        TextureFormat.Unknown,
                        flags,
                        TextureOperation.DefaultCbufSlot,
                        imm);

                    if ((componentMask & 4) != 0)
                    {
                        // Skip first 2 components if necessary.
                        if ((componentMask & 1) != 0)
                        {
                            GetDest();
                        }

                        if ((componentMask & 2) != 0)
                        {
                            GetDest();
                        }

                        Operand d = GetDest();

                        if (d != null)
                        {
                            context.Copy(d, context.TextureQuerySamples(type, flags, setAndBinding, sources));
                        }
                    }
                    break;

                default:
                    context.TranslatorContext.GpuAccessor.Log($"Invalid or unsupported query type \"{query}\".");
                    break;
            }
        }

        private static void EmitTextureSample(
            EmitterContext context,
            SamplerType type,
            TextureFlags flags,
            int handle,
            int componentMask,
            Operand[] dests,
            Operand[] sources)
        {
            SetBindingPair setAndBinding = flags.HasFlag(TextureFlags.Bindless) ? default : context.ResourceManager.GetTextureOrImageBinding(
                Instruction.TextureSample,
                type,
                TextureFormat.Unknown,
                flags,
                TextureOperation.DefaultCbufSlot,
                handle);

            context.TextureSample(type, flags, setAndBinding, componentMask, dests, sources);
        }

        private static SamplerType ConvertSamplerType(TexDim dimensions)
        {
            return dimensions switch
            {
                TexDim._1d => SamplerType.Texture1D,
                TexDim.Array1d => SamplerType.Texture1D | SamplerType.Array,
                TexDim._2d => SamplerType.Texture2D,
                TexDim.Array2d => SamplerType.Texture2D | SamplerType.Array,
                TexDim._3d => SamplerType.Texture3D,
                TexDim.Array3d => SamplerType.Texture3D | SamplerType.Array,
                TexDim.Cube => SamplerType.TextureCube,
                TexDim.ArrayCube => SamplerType.TextureCube | SamplerType.Array,
                _ => throw new ArgumentException($"Invalid texture dimensions \"{dimensions}\"."),
            };
        }

        private static SamplerType ConvertSamplerType(TexsTarget type)
        {
            switch (type)
            {
                case TexsTarget.Texture1DLodZero:
                    return SamplerType.Texture1D;

                case TexsTarget.Texture2D:
                case TexsTarget.Texture2DLodZero:
                case TexsTarget.Texture2DLodLevel:
                    return SamplerType.Texture2D;

                case TexsTarget.Texture2DDepthCompare:
                case TexsTarget.Texture2DLodLevelDepthCompare:
                case TexsTarget.Texture2DLodZeroDepthCompare:
                    return SamplerType.Texture2D | SamplerType.Shadow;

                case TexsTarget.Texture2DArray:
                case TexsTarget.Texture2DArrayLodZero:
                    return SamplerType.Texture2D | SamplerType.Array;

                case TexsTarget.Texture2DArrayLodZeroDepthCompare:
                    return SamplerType.Texture2D | SamplerType.Array | SamplerType.Shadow;

                case TexsTarget.Texture3D:
                case TexsTarget.Texture3DLodZero:
                    return SamplerType.Texture3D;

                case TexsTarget.TextureCube:
                case TexsTarget.TextureCubeLodLevel:
                    return SamplerType.TextureCube;
            }

            return SamplerType.None;
        }

        private static SamplerType ConvertSamplerType(TldsTarget type)
        {
            switch (type)
            {
                case TldsTarget.Texture1DLodZero:
                case TldsTarget.Texture1DLodLevel:
                    return SamplerType.Texture1D;

                case TldsTarget.Texture2DLodZero:
                case TldsTarget.Texture2DLodZeroOffset:
                case TldsTarget.Texture2DLodLevel:
                case TldsTarget.Texture2DLodLevelOffset:
                    return SamplerType.Texture2D;

                case TldsTarget.Texture2DLodZeroMultisample:
                    return SamplerType.Texture2D | SamplerType.Multisample;

                case TldsTarget.Texture3DLodZero:
                    return SamplerType.Texture3D;

                case TldsTarget.Texture2DArrayLodZero:
                    return SamplerType.Texture2D | SamplerType.Array;
            }

            return SamplerType.None;
        }

        private static TextureFlags ConvertTextureFlags(TexsTarget type)
        {
            switch (type)
            {
                case TexsTarget.Texture1DLodZero:
                case TexsTarget.Texture2DLodZero:
                case TexsTarget.Texture2DLodLevel:
                case TexsTarget.Texture2DLodLevelDepthCompare:
                case TexsTarget.Texture2DLodZeroDepthCompare:
                case TexsTarget.Texture2DArrayLodZero:
                case TexsTarget.Texture2DArrayLodZeroDepthCompare:
                case TexsTarget.Texture3DLodZero:
                case TexsTarget.TextureCubeLodLevel:
                    return TextureFlags.LodLevel;

                case TexsTarget.Texture2D:
                case TexsTarget.Texture2DDepthCompare:
                case TexsTarget.Texture2DArray:
                case TexsTarget.Texture3D:
                case TexsTarget.TextureCube:
                    return TextureFlags.None;
            }

            return TextureFlags.None;
        }

        private static TextureFlags ConvertTextureFlags(TldsTarget type)
        {
            switch (type)
            {
                case TldsTarget.Texture1DLodZero:
                case TldsTarget.Texture1DLodLevel:
                case TldsTarget.Texture2DLodZero:
                case TldsTarget.Texture2DLodLevel:
                case TldsTarget.Texture2DLodZeroMultisample:
                case TldsTarget.Texture3DLodZero:
                case TldsTarget.Texture2DArrayLodZero:
                    return TextureFlags.LodLevel;

                case TldsTarget.Texture2DLodZeroOffset:
                case TldsTarget.Texture2DLodLevelOffset:
                    return TextureFlags.LodLevel | TextureFlags.Offset;
            }

            return TextureFlags.None;
        }
    }
}

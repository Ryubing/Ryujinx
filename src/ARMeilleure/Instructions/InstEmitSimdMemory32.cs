using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.State;
using ARMeilleure.Translation;

using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.Instructions.InstEmitMemoryHelper;
using static ARMeilleure.IntermediateRepresentation.Operand.Factory;

namespace ARMeilleure.Instructions
{
    static partial class InstEmit32
    {
        public static void Vld1(ArmEmitterContext context)
        {
            EmitVStoreOrLoadN(context, 1, true);
        }

        public static void Vld2(ArmEmitterContext context)
        {
            EmitVStoreOrLoadN(context, 2, true);
        }

        public static void Vld3(ArmEmitterContext context)
        {
            EmitVStoreOrLoadN(context, 3, true);
        }

        public static void Vld4(ArmEmitterContext context)
        {
            EmitVStoreOrLoadN(context, 4, true);
        }

        public static void Vst1(ArmEmitterContext context)
        {
            EmitVStoreOrLoadN(context, 1, false);
        }

        public static void Vst2(ArmEmitterContext context)
        {
            EmitVStoreOrLoadN(context, 2, false);
        }

        public static void Vst3(ArmEmitterContext context)
        {
            EmitVStoreOrLoadN(context, 3, false);
        }

        public static void Vst4(ArmEmitterContext context)
        {
            EmitVStoreOrLoadN(context, 4, false);
        }

        public static void EmitVStoreOrLoadN(ArmEmitterContext context, int count, bool load)
        {
            if (context.CurrOp is OpCode32SimdMemSingle)
            {
                OpCode32SimdMemSingle op = (OpCode32SimdMemSingle)context.CurrOp;

                int eBytes = 1 << op.Size;

                Operand n = context.Copy(GetIntA32(context, op.Rn));

                // TODO: Check alignment.
                int offset = 0;
                int d = op.Vd;

                for (int i = 0; i < count; i++)
                {
                    // Accesses an element from a double simd register.
                    Operand address = context.Add(n, Const(offset));
                    if (eBytes == 8)
                    {
                        if (load)
                        {
                            EmitDVectorLoad(context, address, d);
                        }
                        else
                        {
                            EmitDVectorStore(context, address, d);
                        }
                    }
                    else
                    {
                        int index = ((d & 1) << (3 - op.Size)) + op.Index;
                        if (load)
                        {
                            if (op.Replicate)
                            {
                                int regs = (count > 1) ? 1 : op.Increment;
                                for (int reg = 0; reg < regs; reg++)
                                {
                                    int dreg = reg + d;
                                    int rIndex = ((dreg & 1) << (3 - op.Size));
                                    int limit = rIndex + (1 << (3 - op.Size));

                                    while (rIndex < limit)
                                    {
                                        EmitLoadSimd(context, address, GetVecA32(dreg >> 1), dreg >> 1, rIndex++, op.Size);
                                    }
                                }
                            }
                            else
                            {
                                EmitLoadSimd(context, address, GetVecA32(d >> 1), d >> 1, index, op.Size);
                            }
                        }
                        else
                        {
                            EmitStoreSimd(context, address, d >> 1, index, op.Size);
                        }
                    }
                    offset += eBytes;
                    d += op.Increment;
                }

                if (op.WBack)
                {
                    if (op.RegisterIndex)
                    {
                        Operand m = GetIntA32(context, op.Rm);
                        SetIntA32(context, op.Rn, context.Add(n, m));
                    }
                    else
                    {
                        SetIntA32(context, op.Rn, context.Add(n, Const(count * eBytes)));
                    }
                }
            }
            else
            {
                OpCode32SimdMemPair op = (OpCode32SimdMemPair)context.CurrOp;

                int increment = count > 1 ? op.Increment : 1;
                int eBytes = 1 << op.Size;

                Operand n = context.Copy(GetIntA32(context, op.Rn));
                int offset = 0;
                int d = op.Vd;

                for (int reg = 0; reg < op.Regs; reg++)
                {
                    for (int elem = 0; elem < op.Elems; elem++)
                    {
                        int elemD = d + reg;
                        for (int i = 0; i < count; i++)
                        {
                            // Accesses an element from a double simd register,
                            // add ebytes for each element.
                            Operand address = context.Add(n, Const(offset));
                            int index = ((elemD & 1) << (3 - op.Size)) + elem;
                            if (eBytes == 8)
                            {
                                if (load)
                                {
                                    EmitDVectorLoad(context, address, elemD);
                                }
                                else
                                {
                                    EmitDVectorStore(context, address, elemD);
                                }
                            }
                            else
                            {
                                if (load)
                                {
                                    EmitLoadSimd(context, address, GetVecA32(elemD >> 1), elemD >> 1, index, op.Size);
                                }
                                else
                                {
                                    EmitStoreSimd(context, address, elemD >> 1, index, op.Size);
                                }
                            }

                            offset += eBytes;
                            elemD += increment;
                        }
                    }
                }

                if (op.WBack)
                {
                    if (op.RegisterIndex)
                    {
                        Operand m = GetIntA32(context, op.Rm);
                        SetIntA32(context, op.Rn, context.Add(n, m));
                    }
                    else
                    {
                        SetIntA32(context, op.Rn, context.Add(n, Const(count * 8 * op.Regs)));
                    }
                }
            }
        }

        public static void Vldm(ArmEmitterContext context)
        {
            OpCode32SimdMemMult op = (OpCode32SimdMemMult)context.CurrOp;

            Operand n = context.Copy(GetIntA32(context, op.Rn));

            Operand baseAddress = context.Add(n, Const(op.Offset));

            bool writeBack = op.PostOffset != 0;

            if (writeBack)
            {
                SetIntA32(context, op.Rn, context.Add(n, Const(op.PostOffset)));
            }

            int range = op.RegisterRange;

            int sReg = (op.DoubleWidth) ? (op.Vd << 1) : op.Vd;
            int offset = 0;
            int byteSize = 4;

            for (int num = 0; num < range; num++, sReg++)
            {
                Operand address = context.Add(baseAddress, Const(offset));
                Operand vec = GetVecA32(sReg >> 2);

                EmitLoadSimd(context, address, vec, sReg >> 2, sReg & 3, WordSizeLog2);
                offset += byteSize;
            }
        }

        public static void Vstm(ArmEmitterContext context)
        {
            OpCode32SimdMemMult op = (OpCode32SimdMemMult)context.CurrOp;

            Operand n = context.Copy(GetIntA32(context, op.Rn));

            Operand baseAddress = context.Add(n, Const(op.Offset));

            bool writeBack = op.PostOffset != 0;

            if (writeBack)
            {
                SetIntA32(context, op.Rn, context.Add(n, Const(op.PostOffset)));
            }

            int offset = 0;

            int range = op.RegisterRange;
            int sReg = (op.DoubleWidth) ? (op.Vd << 1) : op.Vd;
            int byteSize = 4;

            for (int num = 0; num < range; num++, sReg++)
            {
                Operand address = context.Add(baseAddress, Const(offset));

                EmitStoreSimd(context, address, sReg >> 2, sReg & 3, WordSizeLog2);

                offset += byteSize;
            }
        }

        public static void Vldr(ArmEmitterContext context)
        {
            EmitVLoadOrStore(context, AccessType.Load);
        }

        public static void Vstr(ArmEmitterContext context)
        {
            EmitVLoadOrStore(context, AccessType.Store);
        }

        private static void EmitDVectorStore(ArmEmitterContext context, Operand address, int vecD)
        {
            int vecQ = vecD >> 1;
            int vecSElem = (vecD & 1) << 1;
            Operand lblBigEndian = Label();
            Operand lblEnd = Label();

            context.BranchIfTrue(lblBigEndian, GetFlag(PState.EFlag));

            EmitStoreSimd(context, address, vecQ, vecSElem, WordSizeLog2);
            EmitStoreSimd(context, context.Add(address, Const(4)), vecQ, vecSElem | 1, WordSizeLog2);

            context.Branch(lblEnd);

            context.MarkLabel(lblBigEndian);

            EmitStoreSimd(context, address, vecQ, vecSElem | 1, WordSizeLog2);
            EmitStoreSimd(context, context.Add(address, Const(4)), vecQ, vecSElem, WordSizeLog2);

            context.MarkLabel(lblEnd);
        }

        private static void EmitDVectorLoad(ArmEmitterContext context, Operand address, int vecD)
        {
            int vecQ = vecD >> 1;
            int vecSElem = (vecD & 1) << 1;
            Operand vec = GetVecA32(vecQ);

            Operand lblBigEndian = Label();
            Operand lblEnd = Label();

            context.BranchIfTrue(lblBigEndian, GetFlag(PState.EFlag));

            EmitLoadSimd(context, address, vec, vecQ, vecSElem, WordSizeLog2);
            EmitLoadSimd(context, context.Add(address, Const(4)), vec, vecQ, vecSElem | 1, WordSizeLog2);

            context.Branch(lblEnd);

            context.MarkLabel(lblBigEndian);

            EmitLoadSimd(context, address, vec, vecQ, vecSElem | 1, WordSizeLog2);
            EmitLoadSimd(context, context.Add(address, Const(4)), vec, vecQ, vecSElem, WordSizeLog2);

            context.MarkLabel(lblEnd);
        }

        private static void EmitVLoadOrStore(ArmEmitterContext context, AccessType accType)
        {
            OpCode32SimdMemImm op = (OpCode32SimdMemImm)context.CurrOp;

            Operand n = context.Copy(GetIntA32(context, op.Rn));
            Operand m = GetMemM(context, setCarry: false);

            Operand address = op.Add
                ? context.Add(n, m)
                : context.Subtract(n, m);

            int size = op.Size;

            if ((accType & AccessType.Load) != 0)
            {
                if (size == DWordSizeLog2)
                {
                    EmitDVectorLoad(context, address, op.Vd);
                }
                else
                {
                    Operand vec = GetVecA32(op.Vd >> 2);
                    EmitLoadSimd(context, address, vec, op.Vd >> 2, (op.Vd & 3) << (2 - size), size);
                }
            }
            else
            {
                if (size == DWordSizeLog2)
                {
                    EmitDVectorStore(context, address, op.Vd);
                }
                else
                {
                    EmitStoreSimd(context, address, op.Vd >> 2, (op.Vd & 3) << (2 - size), size);
                }
            }
        }
    }
}

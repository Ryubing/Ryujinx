using ARMeilleure.Instructions;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ARMeilleure.Translation
{
    static class Delegates
    {
        public static bool TryGetDelegateFuncPtrByIndex(int index, out nint funcPtr)
        {
            if (index >= 0 && index < _delegates.Count)
            {
                funcPtr = _delegates.Values[index].FuncPtr; // O(1).

                return true;
            }
            else
            {
                funcPtr = default;

                return false;
            }
        }

        public static nint GetDelegateFuncPtrByIndex(int index)
        {
            if (index < 0 || index >= _delegates.Count)
            {
                throw new ArgumentOutOfRangeException($"({nameof(index)} = {index})");
            }

            return _delegates.Values[index].FuncPtr; // O(1).
        }
        
        public static int GetDelegateIndex(MethodInfo info)
        {
            ArgumentNullException.ThrowIfNull(info);

            string key = GetKey(info);

            int index = _delegates.IndexOfKey(key); // O(log(n)).

            if (index == -1)
            {
                throw new KeyNotFoundException($"({nameof(key)} = {key})");
            }

            return index;
        }
        
        private static void SetDelegateInfo(MethodInfo method)
        {
            string key = GetKey(method);

            _delegates.Add(key, new DelegateInfo(method.MethodHandle.GetFunctionPointer())); // ArgumentException (key).
        }

        private static string GetKey(MethodInfo info)
        {
            return $"{info.DeclaringType.Name}.{info.Name}";
        }

        private static readonly SortedList<string, DelegateInfo> _delegates;

        static Delegates()
        {
            _delegates = new SortedList<string, DelegateInfo>();

            SetDelegateInfo(typeof(MathHelper).GetMethod(nameof(MathHelper.Abs)));
            SetDelegateInfo(typeof(MathHelper).GetMethod(nameof(MathHelper.Ceiling)));
            SetDelegateInfo(typeof(MathHelper).GetMethod(nameof(MathHelper.Floor)));
            SetDelegateInfo(typeof(MathHelper).GetMethod(nameof(MathHelper.Round)));
            SetDelegateInfo(typeof(MathHelper).GetMethod(nameof(MathHelper.Truncate)));

            SetDelegateInfo(typeof(MathHelperF).GetMethod(nameof(MathHelperF.Abs)));
            SetDelegateInfo(typeof(MathHelperF).GetMethod(nameof(MathHelperF.Ceiling)));
            SetDelegateInfo(typeof(MathHelperF).GetMethod(nameof(MathHelperF.Floor)));
            SetDelegateInfo(typeof(MathHelperF).GetMethod(nameof(MathHelperF.Round)));
            SetDelegateInfo(typeof(MathHelperF).GetMethod(nameof(MathHelperF.Truncate)));

            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.Break)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.CheckSynchronization)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.EnqueueForRejit)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.GetCntfrqEl0)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.GetCntpctEl0)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.GetCntvctEl0)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.GetCtrEl0)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.GetDczidEl0)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.GetFunctionAddress)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.InvalidateCacheLine)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.ReadByte)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.ReadUInt16)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.ReadUInt32)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.ReadUInt64)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.ReadVector128)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.SignalMemoryTracking)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.SupervisorCall)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.ThrowInvalidMemoryAccess)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.Undefined)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.WriteByte)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.WriteUInt16)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.WriteUInt32)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.WriteUInt64)));
            SetDelegateInfo(typeof(NativeInterface).GetMethod(nameof(NativeInterface.WriteVector128)));

            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.CountLeadingSigns)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.CountLeadingZeros)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Crc32b)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Crc32cb)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Crc32ch)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Crc32cw)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Crc32cx)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Crc32h)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Crc32w)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Crc32x)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Decrypt)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Encrypt)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.FixedRotate)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.HashChoose)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.HashLower)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.HashMajority)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.HashParity)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.HashUpper)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.InverseMixColumns)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.MixColumns)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.PolynomialMult64_128)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.SatF32ToS32)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.SatF32ToS64)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.SatF32ToU32)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.SatF32ToU64)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.SatF64ToS32)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.SatF64ToS64)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.SatF64ToU32)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.SatF64ToU64)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Sha1SchedulePart1)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Sha1SchedulePart2)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Sha256SchedulePart1)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Sha256SchedulePart2)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.SignedShrImm64)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Tbl1)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Tbl2)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Tbl3)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Tbl4)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Tbx1)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Tbx2)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Tbx3)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Tbx4)));
            SetDelegateInfo(typeof(SoftFallback).GetMethod(nameof(SoftFallback.UnsignedShrImm64)));

            SetDelegateInfo(typeof(SoftFloat16_32).GetMethod(nameof(SoftFloat16_32.FPConvert)));
            SetDelegateInfo(typeof(SoftFloat16_64).GetMethod(nameof(SoftFloat16_64.FPConvert)));

            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPAdd)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPAddFpscr))); // A32 only.
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPCompare)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPCompareEQ)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPCompareEQFpscr))); // A32 only.
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPCompareGE)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPCompareGEFpscr))); // A32 only.
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPCompareGT)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPCompareGTFpscr))); // A32 only.
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPCompareLE)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPCompareLEFpscr))); // A32 only.
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPCompareLT)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPCompareLTFpscr))); // A32 only.
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPDiv)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPMax)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPMaxFpscr))); // A32 only.
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPMaxNum)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPMaxNumFpscr))); // A32 only.
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPMin)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPMinFpscr))); // A32 only.
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPMinNum)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPMinNumFpscr))); // A32 only.
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPMul)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPMulFpscr))); // A32 only.
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPMulAdd)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPMulAddFpscr))); // A32 only.
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPMulSub)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPMulSubFpscr))); // A32 only.
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPMulX)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPNegMulAdd)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPNegMulSub)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPRecipEstimate)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPRecipEstimateFpscr))); // A32 only.
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPRecipStep))); // A32 only.
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPRecipStepFused)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPRecpX)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPRSqrtEstimate)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPRSqrtEstimateFpscr))); // A32 only.
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPRSqrtStep))); // A32 only.
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPRSqrtStepFused)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPSqrt)));
            SetDelegateInfo(typeof(SoftFloat32).GetMethod(nameof(SoftFloat32.FPSub)));

            SetDelegateInfo(typeof(SoftFloat32_16).GetMethod(nameof(SoftFloat32_16.FPConvert)));

            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPAdd)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPAddFpscr))); // A32 only.
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPCompare)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPCompareEQ)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPCompareEQFpscr))); // A32 only.
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPCompareGE)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPCompareGEFpscr))); // A32 only.
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPCompareGT)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPCompareGTFpscr))); // A32 only.
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPCompareLE)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPCompareLEFpscr))); // A32 only.
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPCompareLT)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPCompareLTFpscr))); // A32 only.
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPDiv)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPMax)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPMaxFpscr))); // A32 only.
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPMaxNum)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPMaxNumFpscr))); // A32 only.
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPMin)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPMinFpscr))); // A32 only.
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPMinNum)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPMinNumFpscr))); // A32 only.
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPMul)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPMulFpscr))); // A32 only.
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPMulAdd)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPMulAddFpscr))); // A32 only.
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPMulSub)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPMulSubFpscr))); // A32 only.
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPMulX)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPNegMulAdd)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPNegMulSub)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPRecipEstimate)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPRecipEstimateFpscr))); // A32 only.
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPRecipStep))); // A32 only.
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPRecipStepFused)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPRecpX)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPRSqrtEstimate)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPRSqrtEstimateFpscr))); // A32 only.
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPRSqrtStep))); // A32 only.
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPRSqrtStepFused)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPSqrt)));
            SetDelegateInfo(typeof(SoftFloat64).GetMethod(nameof(SoftFloat64.FPSub)));

            SetDelegateInfo(typeof(SoftFloat64_16).GetMethod(nameof(SoftFloat64_16.FPConvert)));
        }
    }
}

using System.Collections.Generic;
using System.Numerics;

namespace Ryujinx.Cpu.LightningJit.Table
{
    class InstTableLevel<T> where T : IInstInfo
    {
        private readonly int _shift;
        private readonly uint _mask;
        private readonly InstTableLevel<T>[] _childs;
        private readonly List<T> _insts;

        private InstTableLevel(List<T> insts, uint baseMask)
        {
            uint commonEncodingMask = baseMask;

            foreach (T info in insts)
            {
                commonEncodingMask &= info.EncodingMask;
            }

            if (commonEncodingMask != 0)
            {
                _shift = BitOperations.TrailingZeroCount(commonEncodingMask);
                int bits = BitOperations.TrailingZeroCount(~(commonEncodingMask >> _shift));
                int count = 1 << bits;
                _mask = uint.MaxValue >> (32 - bits);

                _childs = new InstTableLevel<T>[count];

                List<T>[] splitList = new List<T>[count];

                for (int index = 0; index < insts.Count; index++)
                {
                    int splitIndex = (int)((insts[index].Encoding >> _shift) & _mask);

                    (splitList[splitIndex] ??= []).Add(insts[index]);
                }

                for (int index = 0; index < count; index++)
                {
                    if (splitList[index] == null)
                    {
                        continue;
                    }

                    _childs[index] = new InstTableLevel<T>(splitList[index], baseMask & ~commonEncodingMask);
                }
            }
            else
            {
                _insts = insts;
            }
        }

        public InstTableLevel(List<T> insts) : this(insts, uint.MaxValue)
        {
        }

        public bool TryFind(uint encoding, IsaVersion version, IsaFeature features, out T value)
        {
            if (_childs != null)
            {
                int index = (int)((encoding >> _shift) & _mask);

                if (_childs[index] == null)
                {
                    value = default;

                    return false;
                }

                return _childs[index].TryFind(encoding, version, features, out value);
            }
            else
            {
                foreach (T info in _insts)
                {
                    if ((encoding & info.EncodingMask) == info.Encoding &&
                        !info.IsConstrained(encoding) &&
                        info.Version <= version &&
                        (info.Feature & features) == info.Feature)
                    {
                        value = info;

                        return true;
                    }
                }

                value = default;

                return false;
            }
        }
    }
}

using Ryujinx.Common.Memory;
using Ryujinx.Graphics.Nvdec.Vp9.Dsp;
using Ryujinx.Graphics.Nvdec.Vp9.Types;
using Ryujinx.Graphics.Video;

namespace Ryujinx.Graphics.Nvdec.Vp9
{
    internal struct TileWorkerData
    {
        public ArrayPtr<byte> DataEnd;
        public int BufStart;
        public int BufEnd;
        public Reader BitReader;
        public Vp9BackwardUpdates Counts;

        public MacroBlockD Xd;

        /* dqcoeff are shared by all the planes. So planes must be decoded serially */
        public Array32<Array32<int>> Dqcoeff;
        public InternalErrorInfo ErrorInfo;

        public int DecPartitionPlaneContext(int miRow, int miCol, int bsl)
        {
            ref sbyte aboveCtx = ref Xd.AboveSegContext[miCol];
            ref sbyte leftCtx = ref Xd.LeftSegContext[miRow & Constants.MiMask];
            int above = (aboveCtx >> bsl) & 1, left = (leftCtx >> bsl) & 1;

            return (left * 2) + above + (bsl * Constants.PartitionPloffset);
        }
    }
}
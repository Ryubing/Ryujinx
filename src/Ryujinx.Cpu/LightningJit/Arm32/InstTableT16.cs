using Ryujinx.Cpu.LightningJit.Table;
using System.Collections.Generic;

namespace Ryujinx.Cpu.LightningJit.Arm32
{
    static class InstTableT16<T> where T : IInstEmit
    {
        private static readonly InstTableLevel<InstInfoForTable> _table;

        static InstTableT16()
        {
            InstEncoding[] rmRdndnConstraints =
            [
                new(0x00680000, 0x00780000),
                new(0x00850000, 0x00870000)
            ];

            InstEncoding[] rmConstraints =
            [
                new(0x00680000, 0x00780000)
            ];

            InstEncoding[] condCondConstraints =
            [
                new(0x0E000000, 0x0F000000),
                new(0x0F000000, 0x0F000000)
            ];

            InstEncoding[] maskConstraints =
            [
                new(0x00000000, 0x000F0000)
            ];

            InstEncoding[] opConstraints =
            [
                new(0x18000000, 0x18000000)
            ];

            InstEncoding[] opOpOpOpConstraints =
            [
                new(0x00000000, 0x03C00000),
                new(0x00400000, 0x03C00000),
                new(0x01400000, 0x03C00000),
                new(0x01800000, 0x03C00000)
            ];

            List<InstInfoForTable> insts =
            [
                new(0x41400000, 0xFFC00000, InstName.AdcR, T.AdcRT1, IsaVersion.v80, InstFlags.Rdn),
                new(0x1C000000, 0xFE000000, InstName.AddI, T.AddIT1, IsaVersion.v80, InstFlags.Rd),
                new(0x30000000, 0xF8000000, InstName.AddI, T.AddIT2, IsaVersion.v80, InstFlags.Rdn),
                new(0x18000000, 0xFE000000, InstName.AddR, T.AddRT1, IsaVersion.v80, InstFlags.Rd),
                new(0x44000000, 0xFF000000, rmRdndnConstraints, InstName.AddR, T.AddRT2, IsaVersion.v80,
                    InstFlags.RdnDn),
                new(0xA8000000, 0xF8000000, InstName.AddSpI, T.AddSpIT1, IsaVersion.v80, InstFlags.RdRd16),
                new(0xB0000000, 0xFF800000, InstName.AddSpI, T.AddSpIT2, IsaVersion.v80, InstFlags.None),
                new(0x44680000, 0xFF780000, InstName.AddSpR, T.AddSpRT1, IsaVersion.v80, InstFlags.None),
                new(0x44850000, 0xFF870000, rmConstraints, InstName.AddSpR, T.AddSpRT2, IsaVersion.v80, InstFlags.None),
                new(0xA0000000, 0xF8000000, InstName.Adr, T.AdrT1, IsaVersion.v80, InstFlags.RdRd16),
                new(0x40000000, 0xFFC00000, InstName.AndR, T.AndRT1, IsaVersion.v80, InstFlags.Rdn),
                new(0xD0000000, 0xF0000000, condCondConstraints, InstName.B, T.BT1, IsaVersion.v80, InstFlags.Cond),
                new(0xE0000000, 0xF8000000, InstName.B, T.BT2, IsaVersion.v80, InstFlags.None),
                new(0x43800000, 0xFFC00000, InstName.BicR, T.BicRT1, IsaVersion.v80, InstFlags.Rdn),
                new(0xBE000000, 0xFF000000, InstName.Bkpt, T.BkptT1, IsaVersion.v80, InstFlags.None),
                new(0x47800000, 0xFF870000, InstName.BlxR, T.BlxRT1, IsaVersion.v80, InstFlags.None),
                new(0x47000000, 0xFF870000, InstName.Bx, T.BxT1, IsaVersion.v80, InstFlags.None),
                new(0xB1000000, 0xF5000000, InstName.Cbnz, T.CbnzT1, IsaVersion.v80, InstFlags.None),
                new(0x42C00000, 0xFFC00000, InstName.CmnR, T.CmnRT1, IsaVersion.v80, InstFlags.None),
                new(0x28000000, 0xF8000000, InstName.CmpI, T.CmpIT1, IsaVersion.v80, InstFlags.None),
                new(0x42800000, 0xFFC00000, InstName.CmpR, T.CmpRT1, IsaVersion.v80, InstFlags.None),
                new(0x45000000, 0xFF000000, InstName.CmpR, T.CmpRT2, IsaVersion.v80, InstFlags.None),
                new(0xB6600000, 0xFFE80000, InstName.Cps, T.CpsT1, IsaVersion.v80, InstFlags.None),
                new(0x40400000, 0xFFC00000, InstName.EorR, T.EorRT1, IsaVersion.v80, InstFlags.Rdn),
                new(0xBA800000, 0xFFC00000, InstName.Hlt, T.HltT1, IsaVersion.v80, InstFlags.None),
                new(0xBF000000, 0xFF000000, maskConstraints, InstName.It, T.ItT1, IsaVersion.v80, InstFlags.None),
                new(0xC8000000, 0xF8000000, InstName.Ldm, T.LdmT1, IsaVersion.v80, InstFlags.Rlist),
                new(0x78000000, 0xF8000000, InstName.LdrbI, T.LdrbIT1, IsaVersion.v80, InstFlags.Rt),
                new(0x5C000000, 0xFE000000, InstName.LdrbR, T.LdrbRT1, IsaVersion.v80, InstFlags.Rt),
                new(0x88000000, 0xF8000000, InstName.LdrhI, T.LdrhIT1, IsaVersion.v80, InstFlags.Rt),
                new(0x5A000000, 0xFE000000, InstName.LdrhR, T.LdrhRT1, IsaVersion.v80, InstFlags.Rt),
                new(0x56000000, 0xFE000000, InstName.LdrsbR, T.LdrsbRT1, IsaVersion.v80, InstFlags.Rt),
                new(0x5E000000, 0xFE000000, InstName.LdrshR, T.LdrshRT1, IsaVersion.v80, InstFlags.Rt),
                new(0x68000000, 0xF8000000, InstName.LdrI, T.LdrIT1, IsaVersion.v80, InstFlags.Rt),
                new(0x98000000, 0xF8000000, InstName.LdrI, T.LdrIT2, IsaVersion.v80, InstFlags.RtRd16),
                new(0x48000000, 0xF8000000, InstName.LdrL, T.LdrLT1, IsaVersion.v80, InstFlags.RtRd16),
                new(0x58000000, 0xFE000000, InstName.LdrR, T.LdrRT1, IsaVersion.v80, InstFlags.Rt),
                new(0x20000000, 0xF8000000, InstName.MovI, T.MovIT1, IsaVersion.v80, InstFlags.RdRd16),
                new(0x46000000, 0xFF000000, InstName.MovR, T.MovRT1, IsaVersion.v80, InstFlags.Rd),
                new(0x00000000, 0xE0000000, opConstraints, InstName.MovR, T.MovRT2, IsaVersion.v80, InstFlags.Rd),
                new(0x40000000, 0xFE000000, opOpOpOpConstraints, InstName.MovRr, T.MovRrT1, IsaVersion.v80,
                    InstFlags.None),
                new(0x43400000, 0xFFC00000, InstName.Mul, T.MulT1, IsaVersion.v80, InstFlags.None),
                new(0x43C00000, 0xFFC00000, InstName.MvnR, T.MvnRT1, IsaVersion.v80, InstFlags.Rd),
                new(0xBF000000, 0xFFFF0000, InstName.Nop, T.NopT1, IsaVersion.v80, InstFlags.None),
                new(0x43000000, 0xFFC00000, InstName.OrrR, T.OrrRT1, IsaVersion.v80, InstFlags.Rdn),
                new(0xBC000000, 0xFE000000, InstName.Pop, T.PopT1, IsaVersion.v80, InstFlags.Rlist),
                new(0xB4000000, 0xFE000000, InstName.Push, T.PushT1, IsaVersion.v80, InstFlags.RlistRead),
                new(0xBA000000, 0xFFC00000, InstName.Rev, T.RevT1, IsaVersion.v80, InstFlags.Rd),
                new(0xBA400000, 0xFFC00000, InstName.Rev16, T.Rev16T1, IsaVersion.v80, InstFlags.Rd),
                new(0xBAC00000, 0xFFC00000, InstName.Revsh, T.RevshT1, IsaVersion.v80, InstFlags.Rd),
                new(0x42400000, 0xFFC00000, InstName.RsbI, T.RsbIT1, IsaVersion.v80, InstFlags.Rd),
                new(0x41800000, 0xFFC00000, InstName.SbcR, T.SbcRT1, IsaVersion.v80, InstFlags.Rdn),
                new(0xB6500000, 0xFFF70000, InstName.Setend, T.SetendT1, IsaVersion.v80, InstFlags.None),
                new(0xB6100000, 0xFFF70000, InstName.Setpan, T.SetpanT1, IsaVersion.v81, IsaFeature.FeatPan,
                    InstFlags.None),
                new(0xBF400000, 0xFFFF0000, InstName.Sev, T.SevT1, IsaVersion.v80, InstFlags.None),
                new(0xBF500000, 0xFFFF0000, InstName.Sevl, T.SevlT1, IsaVersion.v80, InstFlags.None),
                new(0xC0000000, 0xF8000000, InstName.Stm, T.StmT1, IsaVersion.v80, InstFlags.RlistRead),
                new(0x70000000, 0xF8000000, InstName.StrbI, T.StrbIT1, IsaVersion.v80, InstFlags.RtRead),
                new(0x54000000, 0xFE000000, InstName.StrbR, T.StrbRT1, IsaVersion.v80, InstFlags.RtRead),
                new(0x80000000, 0xF8000000, InstName.StrhI, T.StrhIT1, IsaVersion.v80, InstFlags.RtRead),
                new(0x52000000, 0xFE000000, InstName.StrhR, T.StrhRT1, IsaVersion.v80, InstFlags.RtRead),
                new(0x60000000, 0xF8000000, InstName.StrI, T.StrIT1, IsaVersion.v80, InstFlags.RtRead),
                new(0x90000000, 0xF8000000, InstName.StrI, T.StrIT2, IsaVersion.v80, InstFlags.RtReadRd16),
                new(0x50000000, 0xFE000000, InstName.StrR, T.StrRT1, IsaVersion.v80, InstFlags.RtRead),
                new(0x1E000000, 0xFE000000, InstName.SubI, T.SubIT1, IsaVersion.v80, InstFlags.Rd),
                new(0x38000000, 0xF8000000, InstName.SubI, T.SubIT2, IsaVersion.v80, InstFlags.Rdn),
                new(0x1A000000, 0xFE000000, InstName.SubR, T.SubRT1, IsaVersion.v80, InstFlags.Rd),
                new(0xB0800000, 0xFF800000, InstName.SubSpI, T.SubSpIT1, IsaVersion.v80, InstFlags.None),
                new(0xDF000000, 0xFF000000, InstName.Svc, T.SvcT1, IsaVersion.v80, InstFlags.None),
                new(0xB2400000, 0xFFC00000, InstName.Sxtb, T.SxtbT1, IsaVersion.v80, InstFlags.Rd),
                new(0xB2000000, 0xFFC00000, InstName.Sxth, T.SxthT1, IsaVersion.v80, InstFlags.Rd),
                new(0x42000000, 0xFFC00000, InstName.TstR, T.TstRT1, IsaVersion.v80, InstFlags.None),
                new(0xDE000000, 0xFF000000, InstName.Udf, T.UdfT1, IsaVersion.v80, InstFlags.None),
                new(0xB2C00000, 0xFFC00000, InstName.Uxtb, T.UxtbT1, IsaVersion.v80, InstFlags.Rd),
                new(0xB2800000, 0xFFC00000, InstName.Uxth, T.UxthT1, IsaVersion.v80, InstFlags.Rd),
                new(0xBF200000, 0xFFFF0000, InstName.Wfe, T.WfeT1, IsaVersion.v80, InstFlags.None),
                new(0xBF300000, 0xFFFF0000, InstName.Wfi, T.WfiT1, IsaVersion.v80, InstFlags.None),
                new(0xBF100000, 0xFFFF0000, InstName.Yield, T.YieldT1, IsaVersion.v80, InstFlags.None)
            ];

            _table = new(insts);
        }

        public static bool TryGetMeta(uint encoding, IsaVersion version, IsaFeature features, out InstMeta meta)
        {
            if (_table.TryFind(encoding, version, features, out InstInfoForTable info))
            {
                meta = info.Meta;

                return true;
            }

            meta = new(InstName.Udf, T.UdfA1, IsaVersion.v80, IsaFeature.None, InstFlags.None);

            return false;
        }
    }
}

using Ryujinx.Common;
using Ryujinx.Graphics.GAL;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gpu.Engine.Threed.Blender
{
    /// <summary>
    /// Advanced blend function entry.
    /// </summary>
    readonly struct AdvancedBlendEntry
    {
        /// <summary>
        /// Advanced blend operation.
        /// </summary>
        public AdvancedBlendOp Op { get; }

        /// <summary>
        /// Advanced blend overlap mode.
        /// </summary>
        public AdvancedBlendOverlap Overlap { get; }

        /// <summary>
        /// Whenever the source input is pre-multiplied.
        /// </summary>
        public bool SrcPreMultiplied { get; }

        /// <summary>
        /// Constants used by the microcode.
        /// </summary>
        public RgbFloat[] Constants { get; }

        /// <summary>
        /// Fixed function alpha state.
        /// </summary>
        public FixedFunctionAlpha Alpha { get; }

        /// <summary>
        /// Creates a new advanced blend function entry.
        /// </summary>
        /// <param name="op">Advanced blend operation</param>
        /// <param name="overlap">Advanced blend overlap mode</param>
        /// <param name="srcPreMultiplied">Whenever the source input is pre-multiplied</param>
        /// <param name="constants">Constants used by the microcode</param>
        /// <param name="alpha">Fixed function alpha state</param>
        public AdvancedBlendEntry(
            AdvancedBlendOp op,
            AdvancedBlendOverlap overlap,
            bool srcPreMultiplied,
            RgbFloat[] constants,
            FixedFunctionAlpha alpha)
        {
            Op = op;
            Overlap = overlap;
            SrcPreMultiplied = srcPreMultiplied;
            Constants = constants;
            Alpha = alpha;
        }
    }

    /// <summary>
    /// Pre-generated hash table with advanced blend functions used by the driver.
    /// </summary>
    static class AdvancedBlendPreGenTable
    {
        /// <summary>
        /// Advanced blend functions dictionary.
        /// </summary>
        public static readonly IReadOnlyDictionary<Hash128, AdvancedBlendEntry> Entries = new Dictionary<Hash128, AdvancedBlendEntry>()
        {
            { new Hash128(0x19ECF57B83DE31F7, 0x5BAE759246F264C0), new AdvancedBlendEntry(AdvancedBlendOp.PlusClamped, AdvancedBlendOverlap.Uncorrelated, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0xDE1B14A356A1A9ED, 0x59D803593C607C1D), new AdvancedBlendEntry(AdvancedBlendOp.PlusClampedAlpha, AdvancedBlendOverlap.Uncorrelated, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x1A3C3A6D32DEC368, 0xBCAE519EC6AAA045), new AdvancedBlendEntry(AdvancedBlendOp.PlusDarker, AdvancedBlendOverlap.Uncorrelated, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x6FD380261A63B240, 0x17C3B335DBB9E3DB), new AdvancedBlendEntry(AdvancedBlendOp.Multiply, AdvancedBlendOverlap.Uncorrelated, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0x1D39164823D3A2D1, 0xC45350959CE1C8FB), new AdvancedBlendEntry(AdvancedBlendOp.Screen, AdvancedBlendOverlap.Uncorrelated, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0x18DF09FF53B129FE, 0xC02EDA33C36019F6), new AdvancedBlendEntry(AdvancedBlendOp.Overlay, AdvancedBlendOverlap.Uncorrelated, true,
                [new RgbFloat(0.5f, 0.5f, 0.5f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0x5973E583271EBF06, 0x711497D75D1272E0), new AdvancedBlendEntry(AdvancedBlendOp.Darken, AdvancedBlendOverlap.Uncorrelated, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0x4759E0E5DA54D5E8, 0x1FDD57C0C38AFA1F), new AdvancedBlendEntry(AdvancedBlendOp.Lighten, AdvancedBlendOverlap.Uncorrelated, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0x337684D43CCE97FA, 0x0139E30CC529E1C9), new AdvancedBlendEntry(AdvancedBlendOp.ColorDodge, AdvancedBlendOverlap.Uncorrelated, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0xDA59E85D8428992D, 0x1D3D7C64C9EF0132), new AdvancedBlendEntry(AdvancedBlendOp.ColorBurn, AdvancedBlendOverlap.Uncorrelated, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0x9455B949298CE805, 0xE73D3301518BE98A), new AdvancedBlendEntry(AdvancedBlendOp.HardLight, AdvancedBlendOverlap.Uncorrelated, true,
                [new RgbFloat(0.5f, 0.5f, 0.5f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0xBDD3B4DEDBE336AA, 0xBFA4DCD50D535DEE), new AdvancedBlendEntry(AdvancedBlendOp.SoftLight, AdvancedBlendOverlap.Uncorrelated, true,
                [new RgbFloat(0.2605f, 0.2605f, 0.2605f), new RgbFloat(-0.7817f, -0.7817f, -0.7817f), new RgbFloat(0.3022f, 0.3022f, 0.3022f), new RgbFloat(0.2192f, 0.2192f, 0.2192f), new RgbFloat(0.25f, 0.25f, 0.25f), new RgbFloat(16f, 16f, 16f), new RgbFloat(12f, 12f, 12f), new RgbFloat(3f, 3f, 3f)
                ], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0x22D4E970A028649A, 0x4F3FCB055FCED965), new AdvancedBlendEntry(AdvancedBlendOp.Difference, AdvancedBlendOverlap.Uncorrelated, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0xA346A91311D72114, 0x151A27A3FB0A1904), new AdvancedBlendEntry(AdvancedBlendOp.Minus, AdvancedBlendOverlap.Uncorrelated, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.ReverseSubtractGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x8A307241061FACD6, 0xA39D1826440B8EE7), new AdvancedBlendEntry(AdvancedBlendOp.MinusClamped, AdvancedBlendOverlap.Uncorrelated, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0xB3BE569485EFFFE0, 0x0BA4E269B3CFB165), new AdvancedBlendEntry(AdvancedBlendOp.Exclusion, AdvancedBlendOverlap.Uncorrelated, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0x36FCA3277DC11822, 0x2BC0F6CAC2029672), new AdvancedBlendEntry(AdvancedBlendOp.Contrast, AdvancedBlendOverlap.Uncorrelated, true,
                [new RgbFloat(2f, 2f, 2f), new RgbFloat(0.5f, 0.5f, 0.5f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl)) },
            { new Hash128(0x4A6226AF2DE9BD7F, 0xEB890D7DA716F73A), new AdvancedBlendEntry(AdvancedBlendOp.Invert, AdvancedBlendOverlap.Uncorrelated, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl)) },
            { new Hash128(0xF364CAA94E160FEB, 0xBF364512C72A3797), new AdvancedBlendEntry(AdvancedBlendOp.InvertRGB, AdvancedBlendOverlap.Uncorrelated, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl)) },
            { new Hash128(0x6BF791AB4AC19C87, 0x6FA17A994EA0FCDE), new AdvancedBlendEntry(AdvancedBlendOp.InvertOvg, AdvancedBlendOverlap.Uncorrelated, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0x053C75A0AE0BB222, 0x03C791FEEB59754C), new AdvancedBlendEntry(AdvancedBlendOp.LinearDodge, AdvancedBlendOverlap.Uncorrelated, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0x25762AB40B6CBDE9, 0x595E9A968AC4F01C), new AdvancedBlendEntry(AdvancedBlendOp.LinearBurn, AdvancedBlendOverlap.Uncorrelated, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0xC2D05E2DBE16955D, 0xB8659C7A3FCFA7CE), new AdvancedBlendEntry(AdvancedBlendOp.VividLight, AdvancedBlendOverlap.Uncorrelated, true,
                [new RgbFloat(0.5f, 0.5f, 0.5f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0x223F220B8F74CBFB, 0xD3DD19D7C39209A5), new AdvancedBlendEntry(AdvancedBlendOp.LinearLight, AdvancedBlendOverlap.Uncorrelated, true,
                [new RgbFloat(2f, 2f, 2f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0xD0DAE57A9F1FE78A, 0x353796BCFB8CE30B), new AdvancedBlendEntry(AdvancedBlendOp.PinLight, AdvancedBlendOverlap.Uncorrelated, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0x601C8CBEC07FF8FF, 0xB8E22882360E8695), new AdvancedBlendEntry(AdvancedBlendOp.HardMix, AdvancedBlendOverlap.Uncorrelated, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0x3A55B7B78C76A7A8, 0x206F503B2D9FFEAA), new AdvancedBlendEntry(AdvancedBlendOp.Red, AdvancedBlendOverlap.Uncorrelated, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl)) },
            { new Hash128(0x80BC65C7831388E5, 0xC652457B2C766AEC), new AdvancedBlendEntry(AdvancedBlendOp.Green, AdvancedBlendOverlap.Uncorrelated, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl)) },
            { new Hash128(0x3D3A912E5833EE13, 0x307895951349EE33), new AdvancedBlendEntry(AdvancedBlendOp.Blue, AdvancedBlendOverlap.Uncorrelated, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl)) },
            { new Hash128(0x289105BE92E81803, 0xFD8F1F03D15C53B4), new AdvancedBlendEntry(AdvancedBlendOp.HslHue, AdvancedBlendOverlap.Uncorrelated, true,
                [new RgbFloat(0.3f, 0.59f, 0.11f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0x007AE3BD140764EB, 0x0EE05A0D2E80BBAE), new AdvancedBlendEntry(AdvancedBlendOp.HslSaturation, AdvancedBlendOverlap.Uncorrelated, true,
                [new RgbFloat(0.3f, 0.59f, 0.11f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0x77F7EE0DB3FDDB96, 0xDEA47C881306DB3E), new AdvancedBlendEntry(AdvancedBlendOp.HslColor, AdvancedBlendOverlap.Uncorrelated, true,
                [new RgbFloat(0.3f, 0.59f, 0.11f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0x66F4E9A7D73CA157, 0x1486058A177DB11C), new AdvancedBlendEntry(AdvancedBlendOp.HslLuminosity, AdvancedBlendOverlap.Uncorrelated, true,
                [new RgbFloat(0.3f, 0.59f, 0.11f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0x593E9F331612D618, 0x9D217BEFA4EB919A), new AdvancedBlendEntry(AdvancedBlendOp.Src, AdvancedBlendOverlap.Disjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.ZeroGl)) },
            { new Hash128(0x0A5194C5E6891106, 0xDD8EC6586106557C), new AdvancedBlendEntry(AdvancedBlendOp.Dst, AdvancedBlendOverlap.Disjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl)) },
            { new Hash128(0x8D77173D5E06E916, 0x06AB190E7D10F4D4), new AdvancedBlendEntry(AdvancedBlendOp.SrcOver, AdvancedBlendOverlap.Disjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x655B4EBC148981DA, 0x455999EF2B9BD28A), new AdvancedBlendEntry(AdvancedBlendOp.DstOver, AdvancedBlendOverlap.Disjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x98F5437D5F518929, 0xBFF4A6E83183DB63), new AdvancedBlendEntry(AdvancedBlendOp.SrcIn, AdvancedBlendOverlap.Disjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x6ADDEFE3B9CEF2FD, 0xB6F6272AFECB1AAB), new AdvancedBlendEntry(AdvancedBlendOp.DstIn, AdvancedBlendOverlap.Disjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x80953F0953BF05B1, 0xD59ABFAA34F8196F), new AdvancedBlendEntry(AdvancedBlendOp.SrcOut, AdvancedBlendOverlap.Disjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0xA401D9AA2A39C121, 0xFC0C8005C22AD7E3), new AdvancedBlendEntry(AdvancedBlendOp.DstOut, AdvancedBlendOverlap.Disjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x06274FB7CA9CDD22, 0x6CE8188B1A9AB6EF), new AdvancedBlendEntry(AdvancedBlendOp.SrcAtop, AdvancedBlendOverlap.Disjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl)) },
            { new Hash128(0x0B079BE7F7F70817, 0xB72E7736CA51E321), new AdvancedBlendEntry(AdvancedBlendOp.DstAtop, AdvancedBlendOverlap.Disjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.ZeroGl)) },
            { new Hash128(0x66215C99403CEDDE, 0x900B733D62204C48), new AdvancedBlendEntry(AdvancedBlendOp.Xor, AdvancedBlendOverlap.Disjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x12DEF2AD900CAD6C, 0x58CF5CC3004910DF), new AdvancedBlendEntry(AdvancedBlendOp.Plus, AdvancedBlendOverlap.Disjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x272BA3A49F64DAE4, 0xAC70B96C00A99EAF), new AdvancedBlendEntry(AdvancedBlendOp.Multiply, AdvancedBlendOverlap.Disjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x206C34AAA7D3F545, 0xDA4B30CACAA483A0), new AdvancedBlendEntry(AdvancedBlendOp.Screen, AdvancedBlendOverlap.Disjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x3D93494920D257BE, 0xDCC573BE1F5F4449), new AdvancedBlendEntry(AdvancedBlendOp.Overlay, AdvancedBlendOverlap.Disjoint, true,
                [new RgbFloat(0.5f, 0.5f, 0.5f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x0D7417D80191107B, 0xEAF40547827E005F), new AdvancedBlendEntry(AdvancedBlendOp.Darken, AdvancedBlendOverlap.Disjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0xEC1B03E8C883F9C9, 0x2D3CA044C58C01B4), new AdvancedBlendEntry(AdvancedBlendOp.Lighten, AdvancedBlendOverlap.Disjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x58A19A0135D68B31, 0x82F35B97AED068E5), new AdvancedBlendEntry(AdvancedBlendOp.ColorDodge, AdvancedBlendOverlap.Disjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x20489F9AB36CC0E3, 0x20499874219E35EE), new AdvancedBlendEntry(AdvancedBlendOp.ColorBurn, AdvancedBlendOverlap.Disjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0xBB176935E5EE05BF, 0x95B26D4D30EA7A14), new AdvancedBlendEntry(AdvancedBlendOp.HardLight, AdvancedBlendOverlap.Disjoint, true,
                [new RgbFloat(0.5f, 0.5f, 0.5f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x5FF9393C908ACFED, 0x068B0BD875773ABF), new AdvancedBlendEntry(AdvancedBlendOp.SoftLight, AdvancedBlendOverlap.Disjoint, true,
                [new RgbFloat(0.2605f, 0.2605f, 0.2605f), new RgbFloat(-0.7817f, -0.7817f, -0.7817f), new RgbFloat(0.3022f, 0.3022f, 0.3022f), new RgbFloat(0.2192f, 0.2192f, 0.2192f), new RgbFloat(0.25f, 0.25f, 0.25f), new RgbFloat(16f, 16f, 16f), new RgbFloat(12f, 12f, 12f), new RgbFloat(3f, 3f, 3f)
                ], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x03181F8711C9802C, 0x6B02C7C6B224FE7B), new AdvancedBlendEntry(AdvancedBlendOp.Difference, AdvancedBlendOverlap.Disjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x2EE2209021F6B977, 0xF3AFA1491B8B89FC), new AdvancedBlendEntry(AdvancedBlendOp.Exclusion, AdvancedBlendOverlap.Disjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0xD8BA4DD2EDE4DC9E, 0x01006114977CF715), new AdvancedBlendEntry(AdvancedBlendOp.Invert, AdvancedBlendOverlap.Disjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl)) },
            { new Hash128(0xD156B99835A2D8ED, 0x2D0BEE9E135EA7A7), new AdvancedBlendEntry(AdvancedBlendOp.InvertRGB, AdvancedBlendOverlap.Disjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl)) },
            { new Hash128(0x20CE8C898ED4BE27, 0x1514900B6F5E8F66), new AdvancedBlendEntry(AdvancedBlendOp.LinearDodge, AdvancedBlendOverlap.Disjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0xCDE5F743820BA2D9, 0x917845FE2ECB083D), new AdvancedBlendEntry(AdvancedBlendOp.LinearBurn, AdvancedBlendOverlap.Disjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0xEB03DF4A0C1D14CD, 0xBAE2E831C6E8FFE4), new AdvancedBlendEntry(AdvancedBlendOp.VividLight, AdvancedBlendOverlap.Disjoint, true,
                [new RgbFloat(0.5f, 0.5f, 0.5f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x1DC9E49AABC779AC, 0x4053A1441EB713D3), new AdvancedBlendEntry(AdvancedBlendOp.LinearLight, AdvancedBlendOverlap.Disjoint, true,
                [new RgbFloat(2f, 2f, 2f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0xFBDEF776248F7B3E, 0xE05EEFD65AC47CB7), new AdvancedBlendEntry(AdvancedBlendOp.PinLight, AdvancedBlendOverlap.Disjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x415A1A48E03AA6E7, 0x046D7EE33CA46B9A), new AdvancedBlendEntry(AdvancedBlendOp.HardMix, AdvancedBlendOverlap.Disjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x59A6901EC9BB2041, 0x2F3E19CE5EEC3EBE), new AdvancedBlendEntry(AdvancedBlendOp.HslHue, AdvancedBlendOverlap.Disjoint, true,
                [new RgbFloat(0.3f, 0.59f, 0.11f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x044B2B6E105221DA, 0x3089BBC033F994AF), new AdvancedBlendEntry(AdvancedBlendOp.HslSaturation, AdvancedBlendOverlap.Disjoint, true,
                [new RgbFloat(0.3f, 0.59f, 0.11f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x374A5A24AA8E6CC5, 0x29930FAA6215FA2B), new AdvancedBlendEntry(AdvancedBlendOp.HslColor, AdvancedBlendOverlap.Disjoint, true,
                [new RgbFloat(0.3f, 0.59f, 0.11f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x30CD0F7AF0CF26F9, 0x06CCA6744DE7DCF5), new AdvancedBlendEntry(AdvancedBlendOp.HslLuminosity, AdvancedBlendOverlap.Disjoint, true,
                [new RgbFloat(0.3f, 0.59f, 0.11f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x1A6C9A1F6FE494A5, 0xA0CFAF77617E54DD), new AdvancedBlendEntry(AdvancedBlendOp.Src, AdvancedBlendOverlap.Conjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.ZeroGl)) },
            { new Hash128(0x081AF6DAAB1C8717, 0xBFEDCE59AE3DC9AC), new AdvancedBlendEntry(AdvancedBlendOp.Dst, AdvancedBlendOverlap.Conjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl)) },
            { new Hash128(0x3518E44573AB68BA, 0xC96EE71AF9F8F546), new AdvancedBlendEntry(AdvancedBlendOp.SrcOver, AdvancedBlendOverlap.Conjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0xF89E81FE8D73C96F, 0x4583A04577A0F21C), new AdvancedBlendEntry(AdvancedBlendOp.DstOver, AdvancedBlendOverlap.Conjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0xDF4026421CB61119, 0x14115A1F5139AFC7), new AdvancedBlendEntry(AdvancedBlendOp.SrcIn, AdvancedBlendOverlap.Conjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MinimumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x91A20262C3E3A695, 0x0B3A102BFCDC6B1C), new AdvancedBlendEntry(AdvancedBlendOp.DstIn, AdvancedBlendOverlap.Conjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MinimumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x44F4C7CCFEB9EBFA, 0xF68394E6D56E5C2F), new AdvancedBlendEntry(AdvancedBlendOp.SrcOut, AdvancedBlendOverlap.Conjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0xB89F17C7021E9760, 0x430357EE0F7188EF), new AdvancedBlendEntry(AdvancedBlendOp.DstOut, AdvancedBlendOverlap.Conjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0xDA2D20EA4242B8A0, 0x0D1EC05B72E3838F), new AdvancedBlendEntry(AdvancedBlendOp.SrcAtop, AdvancedBlendOverlap.Conjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl)) },
            { new Hash128(0x855DFEE1208D11B9, 0x77C6E3DDCFE30B85), new AdvancedBlendEntry(AdvancedBlendOp.DstAtop, AdvancedBlendOverlap.Conjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.ZeroGl)) },
            { new Hash128(0x9B3808439683FD58, 0x123DCBE4705AB25E), new AdvancedBlendEntry(AdvancedBlendOp.Xor, AdvancedBlendOverlap.Conjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0xA42CF045C248A00A, 0x0C6C63C24EA0B0C1), new AdvancedBlendEntry(AdvancedBlendOp.Multiply, AdvancedBlendOverlap.Conjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x320A83B6D00C8059, 0x796EDAB3EB7314BC), new AdvancedBlendEntry(AdvancedBlendOp.Screen, AdvancedBlendOverlap.Conjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x45253AC9ABFFC613, 0x8F92EA70195FB573), new AdvancedBlendEntry(AdvancedBlendOp.Overlay, AdvancedBlendOverlap.Conjoint, true,
                [new RgbFloat(0.5f, 0.5f, 0.5f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x1A5D263B588274B6, 0x167D305F6C794179), new AdvancedBlendEntry(AdvancedBlendOp.Darken, AdvancedBlendOverlap.Conjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x709C1A837FE966AC, 0x75D8CE49E8A78EDB), new AdvancedBlendEntry(AdvancedBlendOp.Lighten, AdvancedBlendOverlap.Conjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x8265C26F85E4145F, 0x932E6CCBF37CB600), new AdvancedBlendEntry(AdvancedBlendOp.ColorDodge, AdvancedBlendOverlap.Conjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x3F252B3FEF983F27, 0x9370D7EEFEFA1A9E), new AdvancedBlendEntry(AdvancedBlendOp.ColorBurn, AdvancedBlendOverlap.Conjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x66A334A4AEA41078, 0xCB52254E1E395231), new AdvancedBlendEntry(AdvancedBlendOp.HardLight, AdvancedBlendOverlap.Conjoint, true,
                [new RgbFloat(0.5f, 0.5f, 0.5f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0xFDD05C53B25F0035, 0xB7E3ECEE166C222F), new AdvancedBlendEntry(AdvancedBlendOp.SoftLight, AdvancedBlendOverlap.Conjoint, true,
                [new RgbFloat(0.2605f, 0.2605f, 0.2605f), new RgbFloat(-0.7817f, -0.7817f, -0.7817f), new RgbFloat(0.3022f, 0.3022f, 0.3022f), new RgbFloat(0.2192f, 0.2192f, 0.2192f), new RgbFloat(0.25f, 0.25f, 0.25f), new RgbFloat(16f, 16f, 16f), new RgbFloat(12f, 12f, 12f), new RgbFloat(3f, 3f, 3f)
                ], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x25D932A77FFED81A, 0xA50D797B0FCA94E8), new AdvancedBlendEntry(AdvancedBlendOp.Difference, AdvancedBlendOverlap.Conjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x4A953B6F5F7D341C, 0xDC05CFB50DDB5DC1), new AdvancedBlendEntry(AdvancedBlendOp.Exclusion, AdvancedBlendOverlap.Conjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x838CB660C4F41F6D, 0x9E7D958697543495), new AdvancedBlendEntry(AdvancedBlendOp.Invert, AdvancedBlendOverlap.Conjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl)) },
            { new Hash128(0x4DF6EC1348A8F797, 0xA128E0CD69DB5A64), new AdvancedBlendEntry(AdvancedBlendOp.InvertRGB, AdvancedBlendOverlap.Conjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl)) },
            { new Hash128(0x178CDFAB9A015295, 0x2BF40EA72E596D57), new AdvancedBlendEntry(AdvancedBlendOp.LinearDodge, AdvancedBlendOverlap.Conjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x338FC99050E56AFD, 0x2AF41CF82BE602BF), new AdvancedBlendEntry(AdvancedBlendOp.LinearBurn, AdvancedBlendOverlap.Conjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x62E02ED60D1E978E, 0xBF726B3E68C11E4D), new AdvancedBlendEntry(AdvancedBlendOp.VividLight, AdvancedBlendOverlap.Conjoint, true,
                [new RgbFloat(0.5f, 0.5f, 0.5f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0xFBAF92DD4C101502, 0x7AF2EDA6596B819D), new AdvancedBlendEntry(AdvancedBlendOp.LinearLight, AdvancedBlendOverlap.Conjoint, true,
                [new RgbFloat(2f, 2f, 2f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x0EF1241F65D4B50A, 0xE8D85DFA6AEDDB84), new AdvancedBlendEntry(AdvancedBlendOp.PinLight, AdvancedBlendOverlap.Conjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x77FE024B5C9D4A18, 0xF19D48A932F6860F), new AdvancedBlendEntry(AdvancedBlendOp.HardMix, AdvancedBlendOverlap.Conjoint, true, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x9C88CBFA2E09D857, 0x0A0361704CBEEE1D), new AdvancedBlendEntry(AdvancedBlendOp.HslHue, AdvancedBlendOverlap.Conjoint, true,
                [new RgbFloat(0.3f, 0.59f, 0.11f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x5B94127FA190E640, 0x8D1FEFF837A91268), new AdvancedBlendEntry(AdvancedBlendOp.HslSaturation, AdvancedBlendOverlap.Conjoint, true,
                [new RgbFloat(0.3f, 0.59f, 0.11f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0xB9C9105B7E063DDB, 0xF6A70E1D511B96FD), new AdvancedBlendEntry(AdvancedBlendOp.HslColor, AdvancedBlendOverlap.Conjoint, true,
                [new RgbFloat(0.3f, 0.59f, 0.11f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0xF0751AAE332B3ED1, 0xC40146F5C83C2533), new AdvancedBlendEntry(AdvancedBlendOp.HslLuminosity, AdvancedBlendOverlap.Conjoint, true,
                [new RgbFloat(0.3f, 0.59f, 0.11f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x579EB12F595F75AD, 0x151BF0504703B81B), new AdvancedBlendEntry(AdvancedBlendOp.DstOver, AdvancedBlendOverlap.Uncorrelated, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0xF9CA152C03AC8C62, 0x1581336205E5CF47), new AdvancedBlendEntry(AdvancedBlendOp.SrcIn, AdvancedBlendOverlap.Uncorrelated, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.DstAlphaGl, BlendFactor.ZeroGl)) },
            { new Hash128(0x98ACD8BB5E195D0F, 0x91F937672BE899F0), new AdvancedBlendEntry(AdvancedBlendOp.SrcOut, AdvancedBlendOverlap.Uncorrelated, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneMinusDstAlphaGl, BlendFactor.ZeroGl)) },
            { new Hash128(0xBF97F10FC301F44C, 0x75721789F0D48548), new AdvancedBlendEntry(AdvancedBlendOp.SrcAtop, AdvancedBlendOverlap.Uncorrelated, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl)) },
            { new Hash128(0x1B982263B8B08A10, 0x3350C76E2E1B27DF), new AdvancedBlendEntry(AdvancedBlendOp.DstAtop, AdvancedBlendOverlap.Uncorrelated, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.ZeroGl)) },
            { new Hash128(0xFF20AC79F64EDED8, 0xAF9025B2D97B9273), new AdvancedBlendEntry(AdvancedBlendOp.Xor, AdvancedBlendOverlap.Uncorrelated, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneMinusDstAlphaGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0x9FFD986600FB112F, 0x384FDDF4E060139A), new AdvancedBlendEntry(AdvancedBlendOp.PlusClamped, AdvancedBlendOverlap.Uncorrelated, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x0425E40B5B8B3B52, 0x5880CBED7CAB631C), new AdvancedBlendEntry(AdvancedBlendOp.PlusClampedAlpha, AdvancedBlendOverlap.Uncorrelated, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x16DAC8593F28623A, 0x233DBC82325B8AED), new AdvancedBlendEntry(AdvancedBlendOp.PlusDarker, AdvancedBlendOverlap.Uncorrelated, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0xB37E5F234B9F0948, 0xD5F957A2ECD98FD6), new AdvancedBlendEntry(AdvancedBlendOp.Multiply, AdvancedBlendOverlap.Uncorrelated, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0xCA0FDADD1D20DBE3, 0x1A5C15CCBF1AC538), new AdvancedBlendEntry(AdvancedBlendOp.Screen, AdvancedBlendOverlap.Uncorrelated, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0x1C48304D73A9DF3A, 0x891DB93FA36E3450), new AdvancedBlendEntry(AdvancedBlendOp.Overlay, AdvancedBlendOverlap.Uncorrelated, false,
                [new RgbFloat(0.5f, 0.5f, 0.5f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0x53200F2279B7FA39, 0x051C2462EBF6789C), new AdvancedBlendEntry(AdvancedBlendOp.Darken, AdvancedBlendOverlap.Uncorrelated, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0xB88BFB80714DCD5C, 0xEBD6938D744E6A41), new AdvancedBlendEntry(AdvancedBlendOp.Lighten, AdvancedBlendOverlap.Uncorrelated, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0xE33DC2A25FC1A976, 0x08B3DBB1F3027D45), new AdvancedBlendEntry(AdvancedBlendOp.ColorDodge, AdvancedBlendOverlap.Uncorrelated, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0xCE97E71615370316, 0xE131AE49D3A4D62B), new AdvancedBlendEntry(AdvancedBlendOp.ColorBurn, AdvancedBlendOverlap.Uncorrelated, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0xE059FD265149B256, 0x94AF817AC348F61F), new AdvancedBlendEntry(AdvancedBlendOp.HardLight, AdvancedBlendOverlap.Uncorrelated, false,
                [new RgbFloat(0.5f, 0.5f, 0.5f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0x16D31333D477E231, 0x9A98AAC84F72CC62), new AdvancedBlendEntry(AdvancedBlendOp.SoftLight, AdvancedBlendOverlap.Uncorrelated, false,
                [new RgbFloat(0.2605f, 0.2605f, 0.2605f), new RgbFloat(-0.7817f, -0.7817f, -0.7817f), new RgbFloat(0.3022f, 0.3022f, 0.3022f), new RgbFloat(0.2192f, 0.2192f, 0.2192f), new RgbFloat(0.25f, 0.25f, 0.25f), new RgbFloat(16f, 16f, 16f), new RgbFloat(12f, 12f, 12f), new RgbFloat(3f, 3f, 3f)
                ], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0x47FC3B0776366D3C, 0xE96D9BD83B277874), new AdvancedBlendEntry(AdvancedBlendOp.Difference, AdvancedBlendOverlap.Uncorrelated, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0x7230401E3FEA1F3B, 0xF0D15F05D3D1E309), new AdvancedBlendEntry(AdvancedBlendOp.Minus, AdvancedBlendOverlap.Uncorrelated, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.ReverseSubtractGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x188212F9303742F5, 0x100C51CB96E03591), new AdvancedBlendEntry(AdvancedBlendOp.MinusClamped, AdvancedBlendOverlap.Uncorrelated, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x52B755D296B44DC5, 0x4003B87275625973), new AdvancedBlendEntry(AdvancedBlendOp.Exclusion, AdvancedBlendOverlap.Uncorrelated, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0xD873ED973ADF7EAD, 0x73E68B57D92034E7), new AdvancedBlendEntry(AdvancedBlendOp.Contrast, AdvancedBlendOverlap.Uncorrelated, false,
                [new RgbFloat(2f, 2f, 2f), new RgbFloat(0.5f, 0.5f, 0.5f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl)) },
            { new Hash128(0x471F9FA34B945ACB, 0x10524D1410B3C402), new AdvancedBlendEntry(AdvancedBlendOp.InvertRGB, AdvancedBlendOverlap.Uncorrelated, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl)) },
            { new Hash128(0x99F569454EA0EF32, 0x6FC70A8B3A07DC8B), new AdvancedBlendEntry(AdvancedBlendOp.LinearDodge, AdvancedBlendOverlap.Uncorrelated, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0x5AD55F950067AC7E, 0x4BA60A4FBABDD0AC), new AdvancedBlendEntry(AdvancedBlendOp.LinearBurn, AdvancedBlendOverlap.Uncorrelated, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0x03FF2C858C9C4C5B, 0xE95AE7F561FB60E9), new AdvancedBlendEntry(AdvancedBlendOp.VividLight, AdvancedBlendOverlap.Uncorrelated, false,
                [new RgbFloat(0.5f, 0.5f, 0.5f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0x6DC0E510C7BCF9D2, 0xAE805D7CECDCB5C1), new AdvancedBlendEntry(AdvancedBlendOp.LinearLight, AdvancedBlendOverlap.Uncorrelated, false,
                [new RgbFloat(2f, 2f, 2f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0x44832332CED5C054, 0x2F8D5536C085B30A), new AdvancedBlendEntry(AdvancedBlendOp.PinLight, AdvancedBlendOverlap.Uncorrelated, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0x4AB4D387618AC51F, 0x495B46E0555F4B32), new AdvancedBlendEntry(AdvancedBlendOp.HardMix, AdvancedBlendOverlap.Uncorrelated, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0x99282B49405A01A8, 0xD6FA93F864F24A8E), new AdvancedBlendEntry(AdvancedBlendOp.Red, AdvancedBlendOverlap.Uncorrelated, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl)) },
            { new Hash128(0x37B30C1064FBD23E, 0x5D068366F42317C2), new AdvancedBlendEntry(AdvancedBlendOp.Green, AdvancedBlendOverlap.Uncorrelated, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl)) },
            { new Hash128(0x760FAE9D59E04BC2, 0xA40AD483EA01435E), new AdvancedBlendEntry(AdvancedBlendOp.Blue, AdvancedBlendOverlap.Uncorrelated, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl)) },
            { new Hash128(0xE786950FD9D1C6EF, 0xF9FDD5AF6451D239), new AdvancedBlendEntry(AdvancedBlendOp.HslHue, AdvancedBlendOverlap.Uncorrelated, false,
                [new RgbFloat(0.3f, 0.59f, 0.11f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0x052458BB4788B0CA, 0x8AC58FDCA1F45EF5), new AdvancedBlendEntry(AdvancedBlendOp.HslSaturation, AdvancedBlendOverlap.Uncorrelated, false,
                [new RgbFloat(0.3f, 0.59f, 0.11f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0x6AFC3837D1D31920, 0xB9D49C2FE49642C6), new AdvancedBlendEntry(AdvancedBlendOp.HslColor, AdvancedBlendOverlap.Uncorrelated, false,
                [new RgbFloat(0.3f, 0.59f, 0.11f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0xAFC2911949317E01, 0xD5B63636F5CB3422), new AdvancedBlendEntry(AdvancedBlendOp.HslLuminosity, AdvancedBlendOverlap.Uncorrelated, false,
                [new RgbFloat(0.3f, 0.59f, 0.11f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl)) },
            { new Hash128(0x13B46DF507CC2C53, 0x86DE26517E6BF0A7), new AdvancedBlendEntry(AdvancedBlendOp.Src, AdvancedBlendOverlap.Disjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.ZeroGl)) },
            { new Hash128(0x5C372442474BE410, 0x79ECD3C0C496EF2E), new AdvancedBlendEntry(AdvancedBlendOp.SrcOver, AdvancedBlendOverlap.Disjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x74AAB45DBF5336E9, 0x01BFC4E181DAD442), new AdvancedBlendEntry(AdvancedBlendOp.DstOver, AdvancedBlendOverlap.Disjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x43239E282A36C85C, 0x36FB65560E46AD0F), new AdvancedBlendEntry(AdvancedBlendOp.SrcIn, AdvancedBlendOverlap.Disjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x1A3BA8A7583B8F7A, 0xE64E41D548033180), new AdvancedBlendEntry(AdvancedBlendOp.SrcOut, AdvancedBlendOverlap.Disjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x32BBB9859E9B565D, 0x3D5CE94FE55F18B5), new AdvancedBlendEntry(AdvancedBlendOp.SrcAtop, AdvancedBlendOverlap.Disjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl)) },
            { new Hash128(0xD947A0766AE3C0FC, 0x391E5D53E86F4ED6), new AdvancedBlendEntry(AdvancedBlendOp.DstAtop, AdvancedBlendOverlap.Disjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.ZeroGl)) },
            { new Hash128(0xBD9A7C08BDFD8CE6, 0x905407634901355E), new AdvancedBlendEntry(AdvancedBlendOp.Xor, AdvancedBlendOverlap.Disjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x8395475BCB0D7A8C, 0x48AF5DD501D44A70), new AdvancedBlendEntry(AdvancedBlendOp.Plus, AdvancedBlendOverlap.Disjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x80AAC23FEBD4A3E5, 0xEA8C70F0B4DE52DE), new AdvancedBlendEntry(AdvancedBlendOp.Multiply, AdvancedBlendOverlap.Disjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x2F3AD1B0F1B3FD09, 0xC0EBC784BFAB8EA3), new AdvancedBlendEntry(AdvancedBlendOp.Screen, AdvancedBlendOverlap.Disjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x52B54032F2F70BFF, 0xC941D6FDED674765), new AdvancedBlendEntry(AdvancedBlendOp.Overlay, AdvancedBlendOverlap.Disjoint, false,
                [new RgbFloat(0.5f, 0.5f, 0.5f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0xCA7B86F72EC6A99B, 0x55868A131AFE359E), new AdvancedBlendEntry(AdvancedBlendOp.Darken, AdvancedBlendOverlap.Disjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x377919B60BD133CA, 0x0FD611627664EF40), new AdvancedBlendEntry(AdvancedBlendOp.Lighten, AdvancedBlendOverlap.Disjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x9D4A0C5EE1153887, 0x7B869EBA218C589B), new AdvancedBlendEntry(AdvancedBlendOp.ColorDodge, AdvancedBlendOverlap.Disjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x311F2A858545D123, 0xB4D09C802480AD62), new AdvancedBlendEntry(AdvancedBlendOp.ColorBurn, AdvancedBlendOverlap.Disjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0xCF78AA6A83AFA689, 0x9DC48B0C2182A3E1), new AdvancedBlendEntry(AdvancedBlendOp.HardLight, AdvancedBlendOverlap.Disjoint, false,
                [new RgbFloat(0.5f, 0.5f, 0.5f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0xC3018CD6F1CF62D1, 0x016E32DD9087B1BB), new AdvancedBlendEntry(AdvancedBlendOp.SoftLight, AdvancedBlendOverlap.Disjoint, false,
                [new RgbFloat(0.2605f, 0.2605f, 0.2605f), new RgbFloat(-0.7817f, -0.7817f, -0.7817f), new RgbFloat(0.3022f, 0.3022f, 0.3022f), new RgbFloat(0.2192f, 0.2192f, 0.2192f), new RgbFloat(0.25f, 0.25f, 0.25f), new RgbFloat(16f, 16f, 16f), new RgbFloat(12f, 12f, 12f), new RgbFloat(3f, 3f, 3f)
                ], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x9CB62CE0E956EE29, 0x0FB67F503E60B3AD), new AdvancedBlendEntry(AdvancedBlendOp.Difference, AdvancedBlendOverlap.Disjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x3589A13C16EF3BFA, 0x15B29BFC91F3BDFB), new AdvancedBlendEntry(AdvancedBlendOp.Exclusion, AdvancedBlendOverlap.Disjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x3502CA5FB7529917, 0xFA51BFD0D1688071), new AdvancedBlendEntry(AdvancedBlendOp.InvertRGB, AdvancedBlendOverlap.Disjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl)) },
            { new Hash128(0x62ADC25AD6D0A923, 0x76CB6D238276D3A3), new AdvancedBlendEntry(AdvancedBlendOp.LinearDodge, AdvancedBlendOverlap.Disjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x09FDEB1116A9D52C, 0x85BB8627CD5C2733), new AdvancedBlendEntry(AdvancedBlendOp.LinearBurn, AdvancedBlendOverlap.Disjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x0709FED1B65E18EB, 0x5BC3AA4D99EC19CF), new AdvancedBlendEntry(AdvancedBlendOp.VividLight, AdvancedBlendOverlap.Disjoint, false,
                [new RgbFloat(0.5f, 0.5f, 0.5f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0xB18D28AE5DE4C723, 0xE820AA2B75C9C02E), new AdvancedBlendEntry(AdvancedBlendOp.LinearLight, AdvancedBlendOverlap.Disjoint, false,
                [new RgbFloat(2f, 2f, 2f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x6743C51621497480, 0x4B164E40858834AE), new AdvancedBlendEntry(AdvancedBlendOp.PinLight, AdvancedBlendOverlap.Disjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x63D1E181E34A2944, 0x1AE292C9D9F12819), new AdvancedBlendEntry(AdvancedBlendOp.HardMix, AdvancedBlendOverlap.Disjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x079523298250BFF6, 0xC0C793510603CDB5), new AdvancedBlendEntry(AdvancedBlendOp.HslHue, AdvancedBlendOverlap.Disjoint, false,
                [new RgbFloat(0.3f, 0.59f, 0.11f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x4C9D0A973C805EA6, 0xD1FF59AD5156B93C), new AdvancedBlendEntry(AdvancedBlendOp.HslSaturation, AdvancedBlendOverlap.Disjoint, false,
                [new RgbFloat(0.3f, 0.59f, 0.11f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x1E914678F3057BCD, 0xD503AE389C12D229), new AdvancedBlendEntry(AdvancedBlendOp.HslColor, AdvancedBlendOverlap.Disjoint, false,
                [new RgbFloat(0.3f, 0.59f, 0.11f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0x9FDBADE5556C5311, 0x03F0CBC798FC5C94), new AdvancedBlendEntry(AdvancedBlendOp.HslLuminosity, AdvancedBlendOverlap.Disjoint, false,
                [new RgbFloat(0.3f, 0.59f, 0.11f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0xE39451534635403C, 0x606CC1CA1F452388), new AdvancedBlendEntry(AdvancedBlendOp.Src, AdvancedBlendOverlap.Conjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.ZeroGl)) },
            { new Hash128(0x1D39F0F0A1008AA6, 0xBFDF2B97E6C3F125), new AdvancedBlendEntry(AdvancedBlendOp.SrcOver, AdvancedBlendOverlap.Conjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0xDB81BED30D5BDBEA, 0xAF0B2856EB93AD2C), new AdvancedBlendEntry(AdvancedBlendOp.DstOver, AdvancedBlendOverlap.Conjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x83F69CCF1D0A79B6, 0x70D31332797430AC), new AdvancedBlendEntry(AdvancedBlendOp.SrcIn, AdvancedBlendOverlap.Conjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MinimumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x7B87F807AB7A8F5C, 0x1241A2A01FB31771), new AdvancedBlendEntry(AdvancedBlendOp.SrcOut, AdvancedBlendOverlap.Conjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0xF557172E20D5272D, 0xC1961F8C7A5D2820), new AdvancedBlendEntry(AdvancedBlendOp.SrcAtop, AdvancedBlendOverlap.Conjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl)) },
            { new Hash128(0xA8476B3944DBBC9B, 0x84A2F6AF97B15FDF), new AdvancedBlendEntry(AdvancedBlendOp.DstAtop, AdvancedBlendOverlap.Conjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.ZeroGl)) },
            { new Hash128(0x3259602B55414DA3, 0x72AACCC00B5A9D10), new AdvancedBlendEntry(AdvancedBlendOp.Xor, AdvancedBlendOverlap.Conjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, 0, 0, 0)) },
            { new Hash128(0xC0CB8C10F36EDCD6, 0x8C2D088AD8191E1C), new AdvancedBlendEntry(AdvancedBlendOp.Multiply, AdvancedBlendOverlap.Conjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x81806C451C6255EF, 0x5AA8AC9A08941A15), new AdvancedBlendEntry(AdvancedBlendOp.Screen, AdvancedBlendOverlap.Conjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0xE55A6537F4568198, 0xCA8735390B799B19), new AdvancedBlendEntry(AdvancedBlendOp.Overlay, AdvancedBlendOverlap.Conjoint, false,
                [new RgbFloat(0.5f, 0.5f, 0.5f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x5C044BA14536DDA3, 0xBCE0123ED7D510EC), new AdvancedBlendEntry(AdvancedBlendOp.Darken, AdvancedBlendOverlap.Conjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x6788346C405BE130, 0x372A4BB199C01F9F), new AdvancedBlendEntry(AdvancedBlendOp.Lighten, AdvancedBlendOverlap.Conjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x510EDC2A34E2856B, 0xE1727A407E294254), new AdvancedBlendEntry(AdvancedBlendOp.ColorDodge, AdvancedBlendOverlap.Conjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x4B7BE01BD398C7A8, 0x5BFF79BC00672C18), new AdvancedBlendEntry(AdvancedBlendOp.ColorBurn, AdvancedBlendOverlap.Conjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x213B43845540CFEC, 0xDA857411CF1CCFCE), new AdvancedBlendEntry(AdvancedBlendOp.HardLight, AdvancedBlendOverlap.Conjoint, false,
                [new RgbFloat(0.5f, 0.5f, 0.5f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x765AFA6732E783F1, 0x8F1CABF1BC78A014), new AdvancedBlendEntry(AdvancedBlendOp.SoftLight, AdvancedBlendOverlap.Conjoint, false,
                [new RgbFloat(0.2605f, 0.2605f, 0.2605f), new RgbFloat(-0.7817f, -0.7817f, -0.7817f), new RgbFloat(0.3022f, 0.3022f, 0.3022f), new RgbFloat(0.2192f, 0.2192f, 0.2192f), new RgbFloat(0.25f, 0.25f, 0.25f), new RgbFloat(16f, 16f, 16f), new RgbFloat(12f, 12f, 12f), new RgbFloat(3f, 3f, 3f)
                ], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0xA4A5DE1CC06F6CB1, 0xA0634A0011001709), new AdvancedBlendEntry(AdvancedBlendOp.Difference, AdvancedBlendOverlap.Conjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x81F32BD8816EA796, 0x697EE86683165170), new AdvancedBlendEntry(AdvancedBlendOp.Exclusion, AdvancedBlendOverlap.Conjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0xB870C209EAA5F092, 0xAF5FD923909CAA1F), new AdvancedBlendEntry(AdvancedBlendOp.InvertRGB, AdvancedBlendOverlap.Conjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl)) },
            { new Hash128(0x3649A9F5C936FB83, 0xDD7C834897AA182A), new AdvancedBlendEntry(AdvancedBlendOp.LinearDodge, AdvancedBlendOverlap.Conjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0xD72A2B1097A5995C, 0x3D41B2763A913654), new AdvancedBlendEntry(AdvancedBlendOp.LinearBurn, AdvancedBlendOverlap.Conjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x551E212B9F6C454A, 0xB0DFA05BEB3C37FA), new AdvancedBlendEntry(AdvancedBlendOp.VividLight, AdvancedBlendOverlap.Conjoint, false,
                [new RgbFloat(0.5f, 0.5f, 0.5f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x681B5A313B7416BF, 0xCB1CBAEEB4D81500), new AdvancedBlendEntry(AdvancedBlendOp.LinearLight, AdvancedBlendOverlap.Conjoint, false,
                [new RgbFloat(2f, 2f, 2f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x9343A18BD4B16777, 0xEDB4AC1C8972C3A4), new AdvancedBlendEntry(AdvancedBlendOp.PinLight, AdvancedBlendOverlap.Conjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0xC960BF6D8519DE28, 0x78D8557FD405D119), new AdvancedBlendEntry(AdvancedBlendOp.HardMix, AdvancedBlendOverlap.Conjoint, false, [], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x65A7B01FDC73A46C, 0x297E096ED5CC4D8A), new AdvancedBlendEntry(AdvancedBlendOp.HslHue, AdvancedBlendOverlap.Conjoint, false,
                [new RgbFloat(0.3f, 0.59f, 0.11f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0xD9C99BA4A6CDC13B, 0x3CFF0ACEDC2EE150), new AdvancedBlendEntry(AdvancedBlendOp.HslSaturation, AdvancedBlendOverlap.Conjoint, false,
                [new RgbFloat(0.3f, 0.59f, 0.11f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x6BC00DA6EB922BD1, 0x5FD4C11F2A685234), new AdvancedBlendEntry(AdvancedBlendOp.HslColor, AdvancedBlendOverlap.Conjoint, false,
                [new RgbFloat(0.3f, 0.59f, 0.11f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
            { new Hash128(0x8652300E32D93050, 0x9460E7B449132371), new AdvancedBlendEntry(AdvancedBlendOp.HslLuminosity, AdvancedBlendOverlap.Conjoint, false,
                [new RgbFloat(0.3f, 0.59f, 0.11f)], new FixedFunctionAlpha(BlendUcodeEnable.EnableRGB, BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl)) },
        };
    }
}

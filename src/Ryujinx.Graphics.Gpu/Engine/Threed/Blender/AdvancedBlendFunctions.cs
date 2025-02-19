using Ryujinx.Common;
using Ryujinx.Graphics.GAL;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace Ryujinx.Graphics.Gpu.Engine.Threed.Blender
{
    static class AdvancedBlendFunctions
    {
        public static readonly AdvancedBlendUcode[] Table =
        [
            new(AdvancedBlendOp.PlusClamped,      AdvancedBlendOverlap.Uncorrelated, true,  GenUncorrelatedPlusClampedPremul),
            new(AdvancedBlendOp.PlusClampedAlpha, AdvancedBlendOverlap.Uncorrelated, true,  GenUncorrelatedPlusClampedAlphaPremul),
            new(AdvancedBlendOp.PlusDarker,       AdvancedBlendOverlap.Uncorrelated, true,  GenUncorrelatedPlusDarkerPremul),
            new(AdvancedBlendOp.Multiply,         AdvancedBlendOverlap.Uncorrelated, true,  GenUncorrelatedMultiplyPremul),
            new(AdvancedBlendOp.Screen,           AdvancedBlendOverlap.Uncorrelated, true,  GenUncorrelatedScreenPremul),
            new(AdvancedBlendOp.Overlay,          AdvancedBlendOverlap.Uncorrelated, true,  GenUncorrelatedOverlayPremul),
            new(AdvancedBlendOp.Darken,           AdvancedBlendOverlap.Uncorrelated, true,  GenUncorrelatedDarkenPremul),
            new(AdvancedBlendOp.Lighten,          AdvancedBlendOverlap.Uncorrelated, true,  GenUncorrelatedLightenPremul),
            new(AdvancedBlendOp.ColorDodge,       AdvancedBlendOverlap.Uncorrelated, true,  GenUncorrelatedColorDodgePremul),
            new(AdvancedBlendOp.ColorBurn,        AdvancedBlendOverlap.Uncorrelated, true,  GenUncorrelatedColorBurnPremul),
            new(AdvancedBlendOp.HardLight,        AdvancedBlendOverlap.Uncorrelated, true,  GenUncorrelatedHardLightPremul),
            new(AdvancedBlendOp.SoftLight,        AdvancedBlendOverlap.Uncorrelated, true,  GenUncorrelatedSoftLightPremul),
            new(AdvancedBlendOp.Difference,       AdvancedBlendOverlap.Uncorrelated, true,  GenUncorrelatedDifferencePremul),
            new(AdvancedBlendOp.Minus,            AdvancedBlendOverlap.Uncorrelated, true,  GenUncorrelatedMinusPremul),
            new(AdvancedBlendOp.MinusClamped,     AdvancedBlendOverlap.Uncorrelated, true,  GenUncorrelatedMinusClampedPremul),
            new(AdvancedBlendOp.Exclusion,        AdvancedBlendOverlap.Uncorrelated, true,  GenUncorrelatedExclusionPremul),
            new(AdvancedBlendOp.Contrast,         AdvancedBlendOverlap.Uncorrelated, true,  GenUncorrelatedContrastPremul),
            new(AdvancedBlendOp.Invert,           AdvancedBlendOverlap.Uncorrelated, true,  GenUncorrelatedInvertPremul),
            new(AdvancedBlendOp.InvertRGB,        AdvancedBlendOverlap.Uncorrelated, true,  GenUncorrelatedInvertRGBPremul),
            new(AdvancedBlendOp.InvertOvg,        AdvancedBlendOverlap.Uncorrelated, true,  GenUncorrelatedInvertOvgPremul),
            new(AdvancedBlendOp.LinearDodge,      AdvancedBlendOverlap.Uncorrelated, true,  GenUncorrelatedLinearDodgePremul),
            new(AdvancedBlendOp.LinearBurn,       AdvancedBlendOverlap.Uncorrelated, true,  GenUncorrelatedLinearBurnPremul),
            new(AdvancedBlendOp.VividLight,       AdvancedBlendOverlap.Uncorrelated, true,  GenUncorrelatedVividLightPremul),
            new(AdvancedBlendOp.LinearLight,      AdvancedBlendOverlap.Uncorrelated, true,  GenUncorrelatedLinearLightPremul),
            new(AdvancedBlendOp.PinLight,         AdvancedBlendOverlap.Uncorrelated, true,  GenUncorrelatedPinLightPremul),
            new(AdvancedBlendOp.HardMix,          AdvancedBlendOverlap.Uncorrelated, true,  GenUncorrelatedHardMixPremul),
            new(AdvancedBlendOp.Red,              AdvancedBlendOverlap.Uncorrelated, true,  GenUncorrelatedRedPremul),
            new(AdvancedBlendOp.Green,            AdvancedBlendOverlap.Uncorrelated, true,  GenUncorrelatedGreenPremul),
            new(AdvancedBlendOp.Blue,             AdvancedBlendOverlap.Uncorrelated, true,  GenUncorrelatedBluePremul),
            new(AdvancedBlendOp.HslHue,           AdvancedBlendOverlap.Uncorrelated, true,  GenUncorrelatedHslHuePremul),
            new(AdvancedBlendOp.HslSaturation,    AdvancedBlendOverlap.Uncorrelated, true,  GenUncorrelatedHslSaturationPremul),
            new(AdvancedBlendOp.HslColor,         AdvancedBlendOverlap.Uncorrelated, true,  GenUncorrelatedHslColorPremul),
            new(AdvancedBlendOp.HslLuminosity,    AdvancedBlendOverlap.Uncorrelated, true,  GenUncorrelatedHslLuminosityPremul),
            new(AdvancedBlendOp.Src,              AdvancedBlendOverlap.Disjoint,     true,  GenDisjointSrcPremul),
            new(AdvancedBlendOp.Dst,              AdvancedBlendOverlap.Disjoint,     true,  GenDisjointDstPremul),
            new(AdvancedBlendOp.SrcOver,          AdvancedBlendOverlap.Disjoint,     true,  GenDisjointSrcOverPremul),
            new(AdvancedBlendOp.DstOver,          AdvancedBlendOverlap.Disjoint,     true,  GenDisjointDstOverPremul),
            new(AdvancedBlendOp.SrcIn,            AdvancedBlendOverlap.Disjoint,     true,  GenDisjointSrcInPremul),
            new(AdvancedBlendOp.DstIn,            AdvancedBlendOverlap.Disjoint,     true,  GenDisjointDstInPremul),
            new(AdvancedBlendOp.SrcOut,           AdvancedBlendOverlap.Disjoint,     true,  GenDisjointSrcOutPremul),
            new(AdvancedBlendOp.DstOut,           AdvancedBlendOverlap.Disjoint,     true,  GenDisjointDstOutPremul),
            new(AdvancedBlendOp.SrcAtop,          AdvancedBlendOverlap.Disjoint,     true,  GenDisjointSrcAtopPremul),
            new(AdvancedBlendOp.DstAtop,          AdvancedBlendOverlap.Disjoint,     true,  GenDisjointDstAtopPremul),
            new(AdvancedBlendOp.Xor,              AdvancedBlendOverlap.Disjoint,     true,  GenDisjointXorPremul),
            new(AdvancedBlendOp.Plus,             AdvancedBlendOverlap.Disjoint,     true,  GenDisjointPlusPremul),
            new(AdvancedBlendOp.Multiply,         AdvancedBlendOverlap.Disjoint,     true,  GenDisjointMultiplyPremul),
            new(AdvancedBlendOp.Screen,           AdvancedBlendOverlap.Disjoint,     true,  GenDisjointScreenPremul),
            new(AdvancedBlendOp.Overlay,          AdvancedBlendOverlap.Disjoint,     true,  GenDisjointOverlayPremul),
            new(AdvancedBlendOp.Darken,           AdvancedBlendOverlap.Disjoint,     true,  GenDisjointDarkenPremul),
            new(AdvancedBlendOp.Lighten,          AdvancedBlendOverlap.Disjoint,     true,  GenDisjointLightenPremul),
            new(AdvancedBlendOp.ColorDodge,       AdvancedBlendOverlap.Disjoint,     true,  GenDisjointColorDodgePremul),
            new(AdvancedBlendOp.ColorBurn,        AdvancedBlendOverlap.Disjoint,     true,  GenDisjointColorBurnPremul),
            new(AdvancedBlendOp.HardLight,        AdvancedBlendOverlap.Disjoint,     true,  GenDisjointHardLightPremul),
            new(AdvancedBlendOp.SoftLight,        AdvancedBlendOverlap.Disjoint,     true,  GenDisjointSoftLightPremul),
            new(AdvancedBlendOp.Difference,       AdvancedBlendOverlap.Disjoint,     true,  GenDisjointDifferencePremul),
            new(AdvancedBlendOp.Exclusion,        AdvancedBlendOverlap.Disjoint,     true,  GenDisjointExclusionPremul),
            new(AdvancedBlendOp.Invert,           AdvancedBlendOverlap.Disjoint,     true,  GenDisjointInvertPremul),
            new(AdvancedBlendOp.InvertRGB,        AdvancedBlendOverlap.Disjoint,     true,  GenDisjointInvertRGBPremul),
            new(AdvancedBlendOp.LinearDodge,      AdvancedBlendOverlap.Disjoint,     true,  GenDisjointLinearDodgePremul),
            new(AdvancedBlendOp.LinearBurn,       AdvancedBlendOverlap.Disjoint,     true,  GenDisjointLinearBurnPremul),
            new(AdvancedBlendOp.VividLight,       AdvancedBlendOverlap.Disjoint,     true,  GenDisjointVividLightPremul),
            new(AdvancedBlendOp.LinearLight,      AdvancedBlendOverlap.Disjoint,     true,  GenDisjointLinearLightPremul),
            new(AdvancedBlendOp.PinLight,         AdvancedBlendOverlap.Disjoint,     true,  GenDisjointPinLightPremul),
            new(AdvancedBlendOp.HardMix,          AdvancedBlendOverlap.Disjoint,     true,  GenDisjointHardMixPremul),
            new(AdvancedBlendOp.HslHue,           AdvancedBlendOverlap.Disjoint,     true,  GenDisjointHslHuePremul),
            new(AdvancedBlendOp.HslSaturation,    AdvancedBlendOverlap.Disjoint,     true,  GenDisjointHslSaturationPremul),
            new(AdvancedBlendOp.HslColor,         AdvancedBlendOverlap.Disjoint,     true,  GenDisjointHslColorPremul),
            new(AdvancedBlendOp.HslLuminosity,    AdvancedBlendOverlap.Disjoint,     true,  GenDisjointHslLuminosityPremul),
            new(AdvancedBlendOp.Src,              AdvancedBlendOverlap.Conjoint,     true,  GenConjointSrcPremul),
            new(AdvancedBlendOp.Dst,              AdvancedBlendOverlap.Conjoint,     true,  GenConjointDstPremul),
            new(AdvancedBlendOp.SrcOver,          AdvancedBlendOverlap.Conjoint,     true,  GenConjointSrcOverPremul),
            new(AdvancedBlendOp.DstOver,          AdvancedBlendOverlap.Conjoint,     true,  GenConjointDstOverPremul),
            new(AdvancedBlendOp.SrcIn,            AdvancedBlendOverlap.Conjoint,     true,  GenConjointSrcInPremul),
            new(AdvancedBlendOp.DstIn,            AdvancedBlendOverlap.Conjoint,     true,  GenConjointDstInPremul),
            new(AdvancedBlendOp.SrcOut,           AdvancedBlendOverlap.Conjoint,     true,  GenConjointSrcOutPremul),
            new(AdvancedBlendOp.DstOut,           AdvancedBlendOverlap.Conjoint,     true,  GenConjointDstOutPremul),
            new(AdvancedBlendOp.SrcAtop,          AdvancedBlendOverlap.Conjoint,     true,  GenConjointSrcAtopPremul),
            new(AdvancedBlendOp.DstAtop,          AdvancedBlendOverlap.Conjoint,     true,  GenConjointDstAtopPremul),
            new(AdvancedBlendOp.Xor,              AdvancedBlendOverlap.Conjoint,     true,  GenConjointXorPremul),
            new(AdvancedBlendOp.Multiply,         AdvancedBlendOverlap.Conjoint,     true,  GenConjointMultiplyPremul),
            new(AdvancedBlendOp.Screen,           AdvancedBlendOverlap.Conjoint,     true,  GenConjointScreenPremul),
            new(AdvancedBlendOp.Overlay,          AdvancedBlendOverlap.Conjoint,     true,  GenConjointOverlayPremul),
            new(AdvancedBlendOp.Darken,           AdvancedBlendOverlap.Conjoint,     true,  GenConjointDarkenPremul),
            new(AdvancedBlendOp.Lighten,          AdvancedBlendOverlap.Conjoint,     true,  GenConjointLightenPremul),
            new(AdvancedBlendOp.ColorDodge,       AdvancedBlendOverlap.Conjoint,     true,  GenConjointColorDodgePremul),
            new(AdvancedBlendOp.ColorBurn,        AdvancedBlendOverlap.Conjoint,     true,  GenConjointColorBurnPremul),
            new(AdvancedBlendOp.HardLight,        AdvancedBlendOverlap.Conjoint,     true,  GenConjointHardLightPremul),
            new(AdvancedBlendOp.SoftLight,        AdvancedBlendOverlap.Conjoint,     true,  GenConjointSoftLightPremul),
            new(AdvancedBlendOp.Difference,       AdvancedBlendOverlap.Conjoint,     true,  GenConjointDifferencePremul),
            new(AdvancedBlendOp.Exclusion,        AdvancedBlendOverlap.Conjoint,     true,  GenConjointExclusionPremul),
            new(AdvancedBlendOp.Invert,           AdvancedBlendOverlap.Conjoint,     true,  GenConjointInvertPremul),
            new(AdvancedBlendOp.InvertRGB,        AdvancedBlendOverlap.Conjoint,     true,  GenConjointInvertRGBPremul),
            new(AdvancedBlendOp.LinearDodge,      AdvancedBlendOverlap.Conjoint,     true,  GenConjointLinearDodgePremul),
            new(AdvancedBlendOp.LinearBurn,       AdvancedBlendOverlap.Conjoint,     true,  GenConjointLinearBurnPremul),
            new(AdvancedBlendOp.VividLight,       AdvancedBlendOverlap.Conjoint,     true,  GenConjointVividLightPremul),
            new(AdvancedBlendOp.LinearLight,      AdvancedBlendOverlap.Conjoint,     true,  GenConjointLinearLightPremul),
            new(AdvancedBlendOp.PinLight,         AdvancedBlendOverlap.Conjoint,     true,  GenConjointPinLightPremul),
            new(AdvancedBlendOp.HardMix,          AdvancedBlendOverlap.Conjoint,     true,  GenConjointHardMixPremul),
            new(AdvancedBlendOp.HslHue,           AdvancedBlendOverlap.Conjoint,     true,  GenConjointHslHuePremul),
            new(AdvancedBlendOp.HslSaturation,    AdvancedBlendOverlap.Conjoint,     true,  GenConjointHslSaturationPremul),
            new(AdvancedBlendOp.HslColor,         AdvancedBlendOverlap.Conjoint,     true,  GenConjointHslColorPremul),
            new(AdvancedBlendOp.HslLuminosity,    AdvancedBlendOverlap.Conjoint,     true,  GenConjointHslLuminosityPremul),
            new(AdvancedBlendOp.DstOver,          AdvancedBlendOverlap.Uncorrelated, false, GenUncorrelatedDstOver),
            new(AdvancedBlendOp.SrcIn,            AdvancedBlendOverlap.Uncorrelated, false, GenUncorrelatedSrcIn),
            new(AdvancedBlendOp.SrcOut,           AdvancedBlendOverlap.Uncorrelated, false, GenUncorrelatedSrcOut),
            new(AdvancedBlendOp.SrcAtop,          AdvancedBlendOverlap.Uncorrelated, false, GenUncorrelatedSrcAtop),
            new(AdvancedBlendOp.DstAtop,          AdvancedBlendOverlap.Uncorrelated, false, GenUncorrelatedDstAtop),
            new(AdvancedBlendOp.Xor,              AdvancedBlendOverlap.Uncorrelated, false, GenUncorrelatedXor),
            new(AdvancedBlendOp.PlusClamped,      AdvancedBlendOverlap.Uncorrelated, false, GenUncorrelatedPlusClamped),
            new(AdvancedBlendOp.PlusClampedAlpha, AdvancedBlendOverlap.Uncorrelated, false, GenUncorrelatedPlusClampedAlpha),
            new(AdvancedBlendOp.PlusDarker,       AdvancedBlendOverlap.Uncorrelated, false, GenUncorrelatedPlusDarker),
            new(AdvancedBlendOp.Multiply,         AdvancedBlendOverlap.Uncorrelated, false, GenUncorrelatedMultiply),
            new(AdvancedBlendOp.Screen,           AdvancedBlendOverlap.Uncorrelated, false, GenUncorrelatedScreen),
            new(AdvancedBlendOp.Overlay,          AdvancedBlendOverlap.Uncorrelated, false, GenUncorrelatedOverlay),
            new(AdvancedBlendOp.Darken,           AdvancedBlendOverlap.Uncorrelated, false, GenUncorrelatedDarken),
            new(AdvancedBlendOp.Lighten,          AdvancedBlendOverlap.Uncorrelated, false, GenUncorrelatedLighten),
            new(AdvancedBlendOp.ColorDodge,       AdvancedBlendOverlap.Uncorrelated, false, GenUncorrelatedColorDodge),
            new(AdvancedBlendOp.ColorBurn,        AdvancedBlendOverlap.Uncorrelated, false, GenUncorrelatedColorBurn),
            new(AdvancedBlendOp.HardLight,        AdvancedBlendOverlap.Uncorrelated, false, GenUncorrelatedHardLight),
            new(AdvancedBlendOp.SoftLight,        AdvancedBlendOverlap.Uncorrelated, false, GenUncorrelatedSoftLight),
            new(AdvancedBlendOp.Difference,       AdvancedBlendOverlap.Uncorrelated, false, GenUncorrelatedDifference),
            new(AdvancedBlendOp.Minus,            AdvancedBlendOverlap.Uncorrelated, false, GenUncorrelatedMinus),
            new(AdvancedBlendOp.MinusClamped,     AdvancedBlendOverlap.Uncorrelated, false, GenUncorrelatedMinusClamped),
            new(AdvancedBlendOp.Exclusion,        AdvancedBlendOverlap.Uncorrelated, false, GenUncorrelatedExclusion),
            new(AdvancedBlendOp.Contrast,         AdvancedBlendOverlap.Uncorrelated, false, GenUncorrelatedContrast),
            new(AdvancedBlendOp.InvertRGB,        AdvancedBlendOverlap.Uncorrelated, false, GenUncorrelatedInvertRGB),
            new(AdvancedBlendOp.LinearDodge,      AdvancedBlendOverlap.Uncorrelated, false, GenUncorrelatedLinearDodge),
            new(AdvancedBlendOp.LinearBurn,       AdvancedBlendOverlap.Uncorrelated, false, GenUncorrelatedLinearBurn),
            new(AdvancedBlendOp.VividLight,       AdvancedBlendOverlap.Uncorrelated, false, GenUncorrelatedVividLight),
            new(AdvancedBlendOp.LinearLight,      AdvancedBlendOverlap.Uncorrelated, false, GenUncorrelatedLinearLight),
            new(AdvancedBlendOp.PinLight,         AdvancedBlendOverlap.Uncorrelated, false, GenUncorrelatedPinLight),
            new(AdvancedBlendOp.HardMix,          AdvancedBlendOverlap.Uncorrelated, false, GenUncorrelatedHardMix),
            new(AdvancedBlendOp.Red,              AdvancedBlendOverlap.Uncorrelated, false, GenUncorrelatedRed),
            new(AdvancedBlendOp.Green,            AdvancedBlendOverlap.Uncorrelated, false, GenUncorrelatedGreen),
            new(AdvancedBlendOp.Blue,             AdvancedBlendOverlap.Uncorrelated, false, GenUncorrelatedBlue),
            new(AdvancedBlendOp.HslHue,           AdvancedBlendOverlap.Uncorrelated, false, GenUncorrelatedHslHue),
            new(AdvancedBlendOp.HslSaturation,    AdvancedBlendOverlap.Uncorrelated, false, GenUncorrelatedHslSaturation),
            new(AdvancedBlendOp.HslColor,         AdvancedBlendOverlap.Uncorrelated, false, GenUncorrelatedHslColor),
            new(AdvancedBlendOp.HslLuminosity,    AdvancedBlendOverlap.Uncorrelated, false, GenUncorrelatedHslLuminosity),
            new(AdvancedBlendOp.Src,              AdvancedBlendOverlap.Disjoint,     false, GenDisjointSrc),
            new(AdvancedBlendOp.SrcOver,          AdvancedBlendOverlap.Disjoint,     false, GenDisjointSrcOver),
            new(AdvancedBlendOp.DstOver,          AdvancedBlendOverlap.Disjoint,     false, GenDisjointDstOver),
            new(AdvancedBlendOp.SrcIn,            AdvancedBlendOverlap.Disjoint,     false, GenDisjointSrcIn),
            new(AdvancedBlendOp.SrcOut,           AdvancedBlendOverlap.Disjoint,     false, GenDisjointSrcOut),
            new(AdvancedBlendOp.SrcAtop,          AdvancedBlendOverlap.Disjoint,     false, GenDisjointSrcAtop),
            new(AdvancedBlendOp.DstAtop,          AdvancedBlendOverlap.Disjoint,     false, GenDisjointDstAtop),
            new(AdvancedBlendOp.Xor,              AdvancedBlendOverlap.Disjoint,     false, GenDisjointXor),
            new(AdvancedBlendOp.Plus,             AdvancedBlendOverlap.Disjoint,     false, GenDisjointPlus),
            new(AdvancedBlendOp.Multiply,         AdvancedBlendOverlap.Disjoint,     false, GenDisjointMultiply),
            new(AdvancedBlendOp.Screen,           AdvancedBlendOverlap.Disjoint,     false, GenDisjointScreen),
            new(AdvancedBlendOp.Overlay,          AdvancedBlendOverlap.Disjoint,     false, GenDisjointOverlay),
            new(AdvancedBlendOp.Darken,           AdvancedBlendOverlap.Disjoint,     false, GenDisjointDarken),
            new(AdvancedBlendOp.Lighten,          AdvancedBlendOverlap.Disjoint,     false, GenDisjointLighten),
            new(AdvancedBlendOp.ColorDodge,       AdvancedBlendOverlap.Disjoint,     false, GenDisjointColorDodge),
            new(AdvancedBlendOp.ColorBurn,        AdvancedBlendOverlap.Disjoint,     false, GenDisjointColorBurn),
            new(AdvancedBlendOp.HardLight,        AdvancedBlendOverlap.Disjoint,     false, GenDisjointHardLight),
            new(AdvancedBlendOp.SoftLight,        AdvancedBlendOverlap.Disjoint,     false, GenDisjointSoftLight),
            new(AdvancedBlendOp.Difference,       AdvancedBlendOverlap.Disjoint,     false, GenDisjointDifference),
            new(AdvancedBlendOp.Exclusion,        AdvancedBlendOverlap.Disjoint,     false, GenDisjointExclusion),
            new(AdvancedBlendOp.InvertRGB,        AdvancedBlendOverlap.Disjoint,     false, GenDisjointInvertRGB),
            new(AdvancedBlendOp.LinearDodge,      AdvancedBlendOverlap.Disjoint,     false, GenDisjointLinearDodge),
            new(AdvancedBlendOp.LinearBurn,       AdvancedBlendOverlap.Disjoint,     false, GenDisjointLinearBurn),
            new(AdvancedBlendOp.VividLight,       AdvancedBlendOverlap.Disjoint,     false, GenDisjointVividLight),
            new(AdvancedBlendOp.LinearLight,      AdvancedBlendOverlap.Disjoint,     false, GenDisjointLinearLight),
            new(AdvancedBlendOp.PinLight,         AdvancedBlendOverlap.Disjoint,     false, GenDisjointPinLight),
            new(AdvancedBlendOp.HardMix,          AdvancedBlendOverlap.Disjoint,     false, GenDisjointHardMix),
            new(AdvancedBlendOp.HslHue,           AdvancedBlendOverlap.Disjoint,     false, GenDisjointHslHue),
            new(AdvancedBlendOp.HslSaturation,    AdvancedBlendOverlap.Disjoint,     false, GenDisjointHslSaturation),
            new(AdvancedBlendOp.HslColor,         AdvancedBlendOverlap.Disjoint,     false, GenDisjointHslColor),
            new(AdvancedBlendOp.HslLuminosity,    AdvancedBlendOverlap.Disjoint,     false, GenDisjointHslLuminosity),
            new(AdvancedBlendOp.Src,              AdvancedBlendOverlap.Conjoint,     false, GenConjointSrc),
            new(AdvancedBlendOp.SrcOver,          AdvancedBlendOverlap.Conjoint,     false, GenConjointSrcOver),
            new(AdvancedBlendOp.DstOver,          AdvancedBlendOverlap.Conjoint,     false, GenConjointDstOver),
            new(AdvancedBlendOp.SrcIn,            AdvancedBlendOverlap.Conjoint,     false, GenConjointSrcIn),
            new(AdvancedBlendOp.SrcOut,           AdvancedBlendOverlap.Conjoint,     false, GenConjointSrcOut),
            new(AdvancedBlendOp.SrcAtop,          AdvancedBlendOverlap.Conjoint,     false, GenConjointSrcAtop),
            new(AdvancedBlendOp.DstAtop,          AdvancedBlendOverlap.Conjoint,     false, GenConjointDstAtop),
            new(AdvancedBlendOp.Xor,              AdvancedBlendOverlap.Conjoint,     false, GenConjointXor),
            new(AdvancedBlendOp.Multiply,         AdvancedBlendOverlap.Conjoint,     false, GenConjointMultiply),
            new(AdvancedBlendOp.Screen,           AdvancedBlendOverlap.Conjoint,     false, GenConjointScreen),
            new(AdvancedBlendOp.Overlay,          AdvancedBlendOverlap.Conjoint,     false, GenConjointOverlay),
            new(AdvancedBlendOp.Darken,           AdvancedBlendOverlap.Conjoint,     false, GenConjointDarken),
            new(AdvancedBlendOp.Lighten,          AdvancedBlendOverlap.Conjoint,     false, GenConjointLighten),
            new(AdvancedBlendOp.ColorDodge,       AdvancedBlendOverlap.Conjoint,     false, GenConjointColorDodge),
            new(AdvancedBlendOp.ColorBurn,        AdvancedBlendOverlap.Conjoint,     false, GenConjointColorBurn),
            new(AdvancedBlendOp.HardLight,        AdvancedBlendOverlap.Conjoint,     false, GenConjointHardLight),
            new(AdvancedBlendOp.SoftLight,        AdvancedBlendOverlap.Conjoint,     false, GenConjointSoftLight),
            new(AdvancedBlendOp.Difference,       AdvancedBlendOverlap.Conjoint,     false, GenConjointDifference),
            new(AdvancedBlendOp.Exclusion,        AdvancedBlendOverlap.Conjoint,     false, GenConjointExclusion),
            new(AdvancedBlendOp.InvertRGB,        AdvancedBlendOverlap.Conjoint,     false, GenConjointInvertRGB),
            new(AdvancedBlendOp.LinearDodge,      AdvancedBlendOverlap.Conjoint,     false, GenConjointLinearDodge),
            new(AdvancedBlendOp.LinearBurn,       AdvancedBlendOverlap.Conjoint,     false, GenConjointLinearBurn),
            new(AdvancedBlendOp.VividLight,       AdvancedBlendOverlap.Conjoint,     false, GenConjointVividLight),
            new(AdvancedBlendOp.LinearLight,      AdvancedBlendOverlap.Conjoint,     false, GenConjointLinearLight),
            new(AdvancedBlendOp.PinLight,         AdvancedBlendOverlap.Conjoint,     false, GenConjointPinLight),
            new(AdvancedBlendOp.HardMix,          AdvancedBlendOverlap.Conjoint,     false, GenConjointHardMix),
            new(AdvancedBlendOp.HslHue,           AdvancedBlendOverlap.Conjoint,     false, GenConjointHslHue),
            new(AdvancedBlendOp.HslSaturation,    AdvancedBlendOverlap.Conjoint,     false, GenConjointHslSaturation),
            new(AdvancedBlendOp.HslColor,         AdvancedBlendOverlap.Conjoint,     false, GenConjointHslColor),
            new(AdvancedBlendOp.HslLuminosity,    AdvancedBlendOverlap.Conjoint,     false, GenConjointHslLuminosity)
        ];

        public static string GenTable()
        {
            // This can be used to generate the table on AdvancedBlendPreGenTable.

            StringBuilder sb = new();

            sb.AppendLine($"private static Dictionary<Hash128, AdvancedBlendEntry> _entries = new()");
            sb.AppendLine("{");

            foreach (AdvancedBlendUcode entry in Table)
            {
                Hash128 hash = Hash128.ComputeHash(MemoryMarshal.Cast<uint, byte>(entry.Code));

                string[] constants = new string[entry.Constants != null ? entry.Constants.Length : 0];

                for (int i = 0; i < constants.Length; i++)
                {
                    RgbFloat rgb = entry.Constants[i];

                    constants[i] = string.Format(CultureInfo.InvariantCulture, "new " + nameof(RgbFloat) + "({0}f, {1}f, {2}f)", rgb.R, rgb.G, rgb.B);
                }

                string constantList = constants.Length > 0 ? $"new[] {{ {string.Join(", ", constants)} }}" : $"Array.Empty<{nameof(RgbFloat)}>()";

                static string EnumValue(string name, object value)
                {
                    if (value.ToString() == "0")
                    {
                        return "0";
                    }

                    return $"{name}.{value}";
                }

                string alpha = $"new {nameof(FixedFunctionAlpha)}({EnumValue(nameof(BlendUcodeEnable), entry.Alpha.Enable)}, {EnumValue(nameof(BlendOp), entry.Alpha.AlphaOp)}, {EnumValue(nameof(BlendFactor), entry.Alpha.AlphaSrcFactor)}, {EnumValue(nameof(BlendFactor), entry.Alpha.AlphaDstFactor)})";

                sb.AppendLine($"    {{ new Hash128(0x{hash.Low:X16}, 0x{hash.High:X16}), new AdvancedBlendEntry({nameof(AdvancedBlendOp)}.{entry.Op}, {nameof(AdvancedBlendOverlap)}.{entry.Overlap}, {(entry.SrcPreMultiplied ? "true" : "false")}, {constantList}, {alpha}) }},");
            }

            sb.AppendLine("};");

            return sb.ToString();
        }

        private static FixedFunctionAlpha GenUncorrelatedPlusClampedPremul(ref UcodeAssembler asm)
        {
            asm.Add(CC.T, Dest.PBR, OpBD.DstRGB, OpBD.SrcRGB);
            asm.Min(CC.T, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenUncorrelatedPlusClampedAlphaPremul(ref UcodeAssembler asm)
        {
            asm.Add(CC.T, Dest.Temp0, OpBD.DstRGB, OpBD.SrcRGB);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.PBR, OpAC.PBR, OpBD.ConstantOne);
            asm.Min(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenUncorrelatedPlusDarkerPremul(ref UcodeAssembler asm)
        {
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.PBR, OpAC.PBR, OpBD.ConstantOne);
            asm.Add(CC.T, Dest.PBR, OpBD.PBR, OpBD.SrcRGB);
            asm.Add(CC.T, Dest.PBR, OpBD.PBR, OpBD.DstRGB);
            asm.Sub(CC.T, Dest.PBR, OpBD.PBR, OpBD.SrcAAA);
            asm.Sub(CC.T, Dest.PBR, OpBD.PBR, OpBD.DstAAA);
            asm.Max(CC.T, Dest.Temp0, OpAC.PBR, OpBD.ConstantZero);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenUncorrelatedMultiplyPremul(ref UcodeAssembler asm)
        {
            asm.Mul(CC.T, Dest.Temp0, OpAC.SrcRGB, OpBD.DstRGB);
            asm.Mmadd(CC.T, Dest.PBR, OpAC.SrcRGB, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Add(CC.T, Dest.Temp0, OpBD.Temp0, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedScreenPremul(ref UcodeAssembler asm)
        {
            asm.Mmadd(CC.T, Dest.PBR, OpAC.SrcRGB, OpBD.DstAAA, OpAC.DstRGB, OpBD.SrcAAA);
            asm.Mmsub(CC.T, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne, OpAC.SrcRGB, OpBD.DstRGB);
            asm.Mmadd(CC.T, Dest.PBR, OpAC.SrcRGB, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Add(CC.T, Dest.Temp0, OpBD.Temp0, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedOverlayPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.5f, 0.5f, 0.5f);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.PBR, OpBD.ConstantRGB);
            asm.Mmadd(CC.LE, Dest.Temp0, OpAC.Temp2, OpBD.Temp1, OpAC.Temp2, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.Temp0, OpBD.ConstantOne, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.PBR, OpBD.ConstantOne, OpBD.Temp2);
            asm.Mmadd(CC.GT, Dest.PBR, OpAC.Temp0, OpBD.PBR, OpAC.Temp0, OpBD.PBR);
            asm.Sub(CC.GT, Dest.Temp0, OpBD.ConstantOne, OpBD.PBR);
            asm.Mmadd(CC.T, Dest.Temp1, OpAC.SrcRGB, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.DstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedDarkenPremul(ref UcodeAssembler asm)
        {
            asm.Mul(CC.T, Dest.Temp0, OpAC.SrcRGB, OpBD.DstAAA);
            asm.Mul(CC.T, Dest.PBR, OpAC.DstRGB, OpBD.SrcAAA);
            asm.Min(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR);
            asm.Mmadd(CC.T, Dest.PBR, OpAC.SrcRGB, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Add(CC.T, Dest.Temp0, OpBD.Temp0, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedLightenPremul(ref UcodeAssembler asm)
        {
            asm.Mul(CC.T, Dest.Temp0, OpAC.SrcRGB, OpBD.DstAAA);
            asm.Mul(CC.T, Dest.PBR, OpAC.DstRGB, OpBD.SrcAAA);
            asm.Max(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR);
            asm.Mmadd(CC.T, Dest.PBR, OpAC.SrcRGB, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Add(CC.T, Dest.Temp0, OpBD.Temp0, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedColorDodgePremul(ref UcodeAssembler asm)
        {
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.SrcRGB);
            asm.Rcp(CC.GT, Dest.PBR, OpAC.PBR);
            asm.Mul(CC.GT, Dest.PBR, OpAC.PBR, OpBD.SrcAAA);
            asm.Mul(CC.GT, Dest.PBR, OpAC.PBR, OpBD.DstRGB);
            asm.Min(CC.GT, Dest.PBR, OpAC.DstAAA, OpBD.PBR);
            asm.Mul(CC.GT, Dest.Temp0, OpAC.PBR, OpBD.SrcAAA);
            asm.Mul(CC.LE, Dest.Temp0, OpAC.SrcAAA, OpBD.DstAAA);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.DstRGB, OpBD.ConstantZero);
            asm.Mul(CC.LE, Dest.Temp0, OpAC.SrcAAA, OpBD.ConstantZero);
            asm.Mmadd(CC.T, Dest.PBR, OpAC.SrcRGB, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Add(CC.T, Dest.Temp0, OpBD.Temp0, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedColorBurnPremul(ref UcodeAssembler asm)
        {
            asm.Mmsub(CC.T, Dest.Temp0, OpAC.DstAAA, OpBD.SrcAAA, OpAC.SrcAAA, OpBD.DstRGB);
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcRGB);
            asm.Mul(CC.T, Dest.PBR, OpAC.Temp0, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.PBR);
            asm.Mmsub(CC.T, Dest.Temp0, OpAC.SrcAAA, OpBD.DstAAA, OpAC.SrcAAA, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcRGB, OpBD.ConstantZero);
            asm.Mul(CC.LE, Dest.Temp0, OpAC.SrcAAA, OpBD.ConstantZero);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.DstAAA, OpBD.DstRGB);
            asm.Mul(CC.LE, Dest.Temp0, OpAC.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.T, Dest.PBR, OpAC.SrcRGB, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Add(CC.T, Dest.Temp0, OpBD.Temp0, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedHardLightPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.5f, 0.5f, 0.5f);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.Temp2, OpBD.ConstantRGB);
            asm.Mmadd(CC.LE, Dest.Temp0, OpAC.Temp2, OpBD.Temp1, OpAC.Temp2, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.Temp0, OpBD.ConstantOne, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.PBR, OpBD.ConstantOne, OpBD.Temp2);
            asm.Mmadd(CC.GT, Dest.PBR, OpAC.Temp0, OpBD.PBR, OpAC.Temp0, OpBD.PBR);
            asm.Sub(CC.GT, Dest.Temp0, OpBD.ConstantOne, OpBD.PBR);
            asm.Mmadd(CC.T, Dest.Temp1, OpAC.SrcRGB, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.DstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedSoftLightPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(4, 0.25f, 0.25f, 0.25f);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.ConstantRGB);
            asm.SetConstant(0, 0.2605f, 0.2605f, 0.2605f);
            asm.Mul(CC.GT, Dest.PBR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.SetConstant(1, -0.7817f, -0.7817f, -0.7817f);
            asm.Mmadd(CC.GT, Dest.PBR, OpAC.Temp1, OpBD.PBR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.SetConstant(2, 0.3022f, 0.3022f, 0.3022f);
            asm.Mmadd(CC.GT, Dest.PBR, OpAC.Temp1, OpBD.PBR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.SetConstant(3, 0.2192f, 0.2192f, 0.2192f);
            asm.Add(CC.GT, Dest.Temp0, OpBD.PBR, OpBD.ConstantRGB);
            asm.SetConstant(5, 16f, 16f, 16f);
            asm.Mul(CC.LE, Dest.PBR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.SetConstant(6, 12f, 12f, 12f);
            asm.Mmsub(CC.LE, Dest.PBR, OpAC.Temp1, OpBD.PBR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.SetConstant(7, 3f, 3f, 3f);
            asm.Mmadd(CC.LE, Dest.Temp0, OpAC.Temp1, OpBD.PBR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.Add(CC.T, Dest.PBR, OpBD.Temp2, OpBD.Temp2);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.ConstantOne);
            asm.Mmsub(CC.LE, Dest.Temp0, OpAC.Temp1, OpBD.ConstantOne, OpAC.Temp1, OpBD.Temp1);
            asm.Add(CC.T, Dest.PBR, OpBD.Temp2, OpBD.Temp2);
            asm.Sub(CC.T, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            asm.Mmadd(CC.T, Dest.Temp1, OpAC.SrcRGB, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.DstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedDifferencePremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.PBR, OpBD.Temp2);
            asm.Sub(CC.LT, Dest.Temp0, OpBD.Temp2, OpBD.Temp1);
            asm.Mmadd(CC.T, Dest.Temp1, OpAC.SrcRGB, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.DstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedMinusPremul(ref UcodeAssembler asm)
        {
            asm.Sub(CC.T, Dest.Temp0, OpBD.DstRGB, OpBD.SrcRGB);
            return new FixedFunctionAlpha(BlendOp.ReverseSubtractGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedMinusClampedPremul(ref UcodeAssembler asm)
        {
            asm.Sub(CC.T, Dest.PBR, OpBD.DstRGB, OpBD.SrcRGB);
            asm.Max(CC.T, Dest.Temp0, OpAC.PBR, OpBD.ConstantZero);
            asm.Sub(CC.T, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Max(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantZero);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenUncorrelatedExclusionPremul(ref UcodeAssembler asm)
        {
            asm.Mmadd(CC.T, Dest.PBR, OpAC.SrcRGB, OpBD.DstAAA, OpAC.DstRGB, OpBD.SrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.ConstantOne, OpAC.SrcRGB, OpBD.DstRGB);
            asm.Mmsub(CC.T, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne, OpAC.SrcRGB, OpBD.DstRGB);
            asm.Mmadd(CC.T, Dest.PBR, OpAC.SrcRGB, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Add(CC.T, Dest.Temp0, OpBD.Temp0, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedContrastPremul(ref UcodeAssembler asm)
        {
            asm.SetConstant(0, 2f, 2f, 2f);
            asm.Mmsub(CC.T, Dest.Temp0, OpAC.DstRGB, OpBD.ConstantRGB, OpAC.DstAAA, OpBD.ConstantOne);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.SrcRGB, OpBD.ConstantRGB, OpAC.SrcAAA, OpBD.ConstantOne);
            asm.Mul(CC.T, Dest.PBR, OpAC.Temp0, OpBD.PBR);
            asm.Add(CC.T, Dest.PBR, OpBD.PBR, OpBD.DstAAA);
            asm.SetConstant(1, 0.5f, 0.5f, 0.5f);
            asm.Mul(CC.T, Dest.Temp0, OpAC.PBR, OpBD.ConstantRGB);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedInvertPremul(ref UcodeAssembler asm)
        {
            asm.Mmsub(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.DstAAA, OpAC.SrcAAA, OpBD.DstRGB);
            asm.Madd(CC.T, Dest.Temp0, OpAC.DstRGB, OpBD.OneMinusSrcAAA, OpAC.PBR);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedInvertRGBPremul(ref UcodeAssembler asm)
        {
            asm.Mmsub(CC.T, Dest.PBR, OpAC.SrcRGB, OpBD.DstAAA, OpAC.SrcRGB, OpBD.DstRGB);
            asm.Madd(CC.T, Dest.Temp0, OpAC.DstRGB, OpBD.OneMinusSrcAAA, OpAC.PBR);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedInvertOvgPremul(ref UcodeAssembler asm)
        {
            asm.Sub(CC.T, Dest.PBR, OpBD.ConstantOne, OpBD.DstRGB);
            asm.Mmadd(CC.T, Dest.Temp0, OpAC.SrcAAA, OpBD.PBR, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedLinearDodgePremul(ref UcodeAssembler asm)
        {
            asm.Mmadd(CC.T, Dest.Temp0, OpAC.SrcRGB, OpBD.DstAAA, OpAC.DstRGB, OpBD.SrcAAA);
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR);
            asm.Mmadd(CC.T, Dest.PBR, OpAC.SrcRGB, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Add(CC.T, Dest.Temp0, OpBD.Temp0, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedLinearBurnPremul(ref UcodeAssembler asm)
        {
            asm.Mmadd(CC.T, Dest.PBR, OpAC.SrcRGB, OpBD.DstAAA, OpAC.DstRGB, OpBD.SrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.ConstantOne, OpAC.SrcAAA, OpBD.DstAAA);
            asm.Max(CC.T, Dest.Temp0, OpAC.PBR, OpBD.ConstantZero);
            asm.Mmadd(CC.T, Dest.PBR, OpAC.SrcRGB, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Add(CC.T, Dest.Temp0, OpBD.Temp0, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedVividLightPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.5f, 0.5f, 0.5f);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.Temp2, OpBD.ConstantRGB);
            asm.Sub(CC.GE, Dest.PBR, OpBD.ConstantOne, OpBD.Temp2);
            asm.Add(CC.GE, Dest.PBR, OpBD.PBR, OpBD.PBR);
            asm.Rcp(CC.GE, Dest.PBR, OpAC.PBR);
            asm.Mul(CC.GE, Dest.PBR, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.GE, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne);
            asm.Add(CC.LT, Dest.PBR, OpBD.Temp2, OpBD.Temp2);
            asm.Rcp(CC.LT, Dest.PBR, OpAC.PBR);
            asm.Mmsub(CC.LT, Dest.PBR, OpAC.PBR, OpBD.ConstantOne, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.LT, Dest.PBR, OpAC.PBR, OpBD.ConstantOne);
            asm.Sub(CC.LT, Dest.Temp0, OpBD.ConstantOne, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.Temp2, OpBD.ConstantZero);
            asm.Mul(CC.LE, Dest.Temp0, OpAC.SrcAAA, OpBD.ConstantZero);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.Temp2, OpBD.ConstantOne);
            asm.Mov(CC.GE, Dest.Temp0, OpBD.ConstantOne);
            asm.Mmadd(CC.T, Dest.Temp1, OpAC.SrcRGB, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.DstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedLinearLightPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.PBR, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 2f, 2f, 2f);
            asm.Madd(CC.T, Dest.PBR, OpAC.Temp2, OpBD.ConstantRGB, OpAC.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Max(CC.T, Dest.PBR, OpAC.PBR, OpBD.ConstantZero);
            asm.Min(CC.T, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne);
            asm.Mmadd(CC.T, Dest.Temp1, OpAC.SrcRGB, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.DstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedPinLightPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Add(CC.T, Dest.PBR, OpBD.Temp2, OpBD.Temp2);
            asm.Sub(CC.T, Dest.Temp0, OpBD.PBR, OpBD.ConstantOne);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.Temp1);
            asm.Max(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.ConstantZero);
            asm.Add(CC.LE, Dest.PBR, OpBD.Temp2, OpBD.Temp2);
            asm.Min(CC.LE, Dest.Temp0, OpAC.PBR, OpBD.Temp1);
            asm.Mmadd(CC.T, Dest.Temp1, OpAC.SrcRGB, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.DstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedHardMixPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.PBR, OpAC.DstRGB, OpBD.PBR);
            asm.Add(CC.T, Dest.PBR, OpBD.Temp2, OpBD.PBR);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.PBR, OpBD.ConstantOne);
            asm.Mul(CC.LT, Dest.Temp0, OpAC.SrcAAA, OpBD.ConstantZero);
            asm.Mov(CC.GE, Dest.Temp0, OpBD.ConstantOne);
            asm.Mmadd(CC.T, Dest.Temp1, OpAC.SrcRGB, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.DstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedRedPremul(ref UcodeAssembler asm)
        {
            asm.Mov(CC.T, Dest.Temp0, OpBD.DstRGB);
            asm.Mov(CC.T, Dest.Temp0.R, OpBD.SrcRGB);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedGreenPremul(ref UcodeAssembler asm)
        {
            asm.Mov(CC.T, Dest.Temp0, OpBD.DstRGB);
            asm.Mov(CC.T, Dest.Temp0.G, OpBD.SrcRGB);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedBluePremul(ref UcodeAssembler asm)
        {
            asm.Mov(CC.T, Dest.Temp0, OpBD.DstRGB);
            asm.Mov(CC.T, Dest.Temp0.B, OpBD.SrcRGB);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedHslHuePremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.Temp0.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.PBR, OpBD.Temp0);
            asm.Rcp(CC.GT, Dest.Temp0, OpAC.Temp0);
            asm.Mov(CC.GT, Dest.PBR.GBR, OpBD.Temp2);
            asm.Min(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Min(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.Temp2, OpAC.Temp0, OpBD.PBR);
            asm.Mov(CC.GT, Dest.PBR.GBR, OpBD.Temp1);
            asm.Min(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.GT, Dest.Temp2.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Mov(CC.GT, Dest.PBR.GBR, OpBD.Temp1);
            asm.Max(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Max(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp0, OpBD.Temp2);
            asm.Mul(CC.LE, Dest.Temp0, OpAC.SrcAAA, OpBD.ConstantZero);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.3f, 0.59f, 0.11f);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.PBR, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.Temp1.BBB, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.Temp0, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp0, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.PBR.BBB, OpAC.Temp0, OpBD.ConstantRGB, OpAC.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.Temp1, OpBD.PBR);
            asm.Add(CC.T, Dest.Temp2, OpBD.Temp0, OpBD.PBR);
            asm.Mov(CC.T, Dest.Temp0, OpBD.PBR);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.ConstantOne);
            asm.Add(CC.GT, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Sub(CC.GT, Dest.PBR, OpBD.PBR, OpBD.Temp1);
            asm.Rcp(CC.GT, Dest.PBR, OpAC.PBR);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.PBR, OpBD.Temp2, OpBD.Temp1);
            asm.Madd(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR.CC, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.LT, Dest.PBR, OpBD.Temp1, OpBD.PBR);
            asm.Rcp(CC.LT, Dest.Temp0, OpAC.PBR);
            asm.Mmsub(CC.LT, Dest.PBR, OpAC.Temp2, OpBD.Temp1, OpAC.Temp1, OpBD.Temp1);
            asm.Madd(CC.LT, Dest.Temp0, OpAC.PBR, OpBD.Temp0, OpAC.Temp1);
            asm.Mmadd(CC.T, Dest.Temp1, OpAC.SrcRGB, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.DstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedHslSaturationPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.T, Dest.Temp0.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp1);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.PBR, OpBD.Temp0);
            asm.Rcp(CC.GT, Dest.Temp0, OpAC.Temp0);
            asm.Mov(CC.GT, Dest.PBR.GBR, OpBD.Temp1);
            asm.Min(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.Temp1, OpAC.Temp0, OpBD.PBR);
            asm.Mov(CC.GT, Dest.PBR.GBR, OpBD.Temp2);
            asm.Min(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Min(CC.GT, Dest.Temp1.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Mov(CC.GT, Dest.PBR.GBR, OpBD.Temp2);
            asm.Max(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Max(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp0, OpBD.Temp1);
            asm.Mul(CC.LE, Dest.Temp0, OpAC.SrcAAA, OpBD.ConstantZero);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.3f, 0.59f, 0.11f);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.PBR, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.Temp1.BBB, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.Temp0, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp0, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.PBR.BBB, OpAC.Temp0, OpBD.ConstantRGB, OpAC.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.Temp1, OpBD.PBR);
            asm.Add(CC.T, Dest.Temp2, OpBD.Temp0, OpBD.PBR);
            asm.Mov(CC.T, Dest.Temp0, OpBD.PBR);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.ConstantOne);
            asm.Add(CC.GT, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Sub(CC.GT, Dest.PBR, OpBD.PBR, OpBD.Temp1);
            asm.Rcp(CC.GT, Dest.PBR, OpAC.PBR);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.PBR, OpBD.Temp2, OpBD.Temp1);
            asm.Madd(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR.CC, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.LT, Dest.PBR, OpBD.Temp1, OpBD.PBR);
            asm.Rcp(CC.LT, Dest.Temp0, OpAC.PBR);
            asm.Mmsub(CC.LT, Dest.PBR, OpAC.Temp2, OpBD.Temp1, OpAC.Temp1, OpBD.Temp1);
            asm.Madd(CC.LT, Dest.Temp0, OpAC.PBR, OpBD.Temp0, OpAC.Temp1);
            asm.Mmadd(CC.T, Dest.Temp1, OpAC.SrcRGB, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.DstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedHslColorPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.3f, 0.59f, 0.11f);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.PBR, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.Temp1.BBB, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.Temp2, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp2, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.PBR.BBB, OpAC.Temp2, OpBD.ConstantRGB, OpAC.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.Temp1, OpBD.PBR);
            asm.Add(CC.T, Dest.Temp2, OpBD.Temp2, OpBD.PBR);
            asm.Mov(CC.T, Dest.Temp0, OpBD.PBR);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.ConstantOne);
            asm.Add(CC.GT, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Sub(CC.GT, Dest.PBR, OpBD.PBR, OpBD.Temp1);
            asm.Rcp(CC.GT, Dest.PBR, OpAC.PBR);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.PBR, OpBD.Temp2, OpBD.Temp1);
            asm.Madd(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR.CC, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.LT, Dest.PBR, OpBD.Temp1, OpBD.PBR);
            asm.Rcp(CC.LT, Dest.Temp0, OpAC.PBR);
            asm.Mmsub(CC.LT, Dest.PBR, OpAC.Temp2, OpBD.Temp1, OpAC.Temp1, OpBD.Temp1);
            asm.Madd(CC.LT, Dest.Temp0, OpAC.PBR, OpBD.Temp0, OpAC.Temp1);
            asm.Mmadd(CC.T, Dest.Temp1, OpAC.SrcRGB, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.DstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedHslLuminosityPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.3f, 0.59f, 0.11f);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.Temp2, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp2, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.Temp2.BBB, OpAC.Temp2, OpBD.ConstantRGB, OpAC.PBR);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.PBR.BBB, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.Temp2, OpBD.PBR);
            asm.Add(CC.T, Dest.Temp1, OpBD.Temp1, OpBD.PBR);
            asm.Mov(CC.T, Dest.Temp0, OpBD.PBR);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp1);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.ConstantOne);
            asm.Add(CC.GT, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Sub(CC.GT, Dest.PBR, OpBD.PBR, OpBD.Temp2);
            asm.Rcp(CC.GT, Dest.PBR, OpAC.PBR);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.GT, Dest.PBR, OpBD.Temp1, OpBD.Temp2);
            asm.Madd(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp2);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp1);
            asm.Min(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.T, Dest.PBR.GBR.CC, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.LT, Dest.PBR, OpBD.Temp2, OpBD.PBR);
            asm.Rcp(CC.LT, Dest.Temp0, OpAC.PBR);
            asm.Mmsub(CC.LT, Dest.PBR, OpAC.Temp1, OpBD.Temp2, OpAC.Temp2, OpBD.Temp2);
            asm.Madd(CC.LT, Dest.Temp0, OpAC.PBR, OpBD.Temp0, OpAC.Temp2);
            asm.Mmadd(CC.T, Dest.Temp1, OpAC.SrcRGB, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.DstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenDisjointSrcPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.Temp0, OpAC.Temp2, OpBD.DstAAA, OpAC.Temp2, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp2, OpBD.PBR, OpAC.Temp0);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.ZeroGl);
        }

        private static FixedFunctionAlpha GenDisjointDstPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.Temp0, OpAC.Temp1, OpBD.DstAAA, OpAC.Temp1, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp1, OpBD.PBR, OpAC.Temp0);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenDisjointSrcOverPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp2);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp2, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.Temp2, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointDstOverPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp1);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp1, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.Temp2, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointSrcInPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.Temp0, OpAC.Temp2, OpBD.DstAAA, OpAC.Temp2, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Sub(CC.T, Dest.Temp1.RToA, OpBD.DstAAA, OpBD.PBR);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointDstInPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.Temp0, OpAC.Temp1, OpBD.DstAAA, OpAC.Temp1, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Sub(CC.T, Dest.Temp1.RToA, OpBD.DstAAA, OpBD.PBR);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointSrcOutPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Mul(CC.T, Dest.Temp0, OpAC.Temp2, OpBD.PBR);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointDstOutPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mul(CC.T, Dest.Temp0, OpAC.Temp1, OpBD.PBR);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointSrcAtopPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.Temp0, OpAC.Temp2, OpBD.DstAAA, OpAC.Temp2, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp1, OpBD.PBR, OpAC.Temp0);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenDisjointDstAtopPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.Temp0, OpAC.Temp1, OpBD.DstAAA, OpAC.Temp1, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp2, OpBD.PBR, OpAC.Temp0);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.ZeroGl);
        }

        private static FixedFunctionAlpha GenDisjointXorPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Mul(CC.T, Dest.Temp0, OpAC.Temp2, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp1, OpBD.PBR, OpAC.Temp0);
            asm.Min(CC.T, Dest.Temp1, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Add(CC.T, Dest.Temp1.RToA, OpBD.Temp1, OpBD.PBR);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointPlusPremul(ref UcodeAssembler asm)
        {
            asm.Add(CC.T, Dest.Temp0, OpBD.DstRGB, OpBD.SrcRGB);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenDisjointMultiplyPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Mul(CC.T, Dest.Temp0, OpAC.Temp2, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.Temp2, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointScreenPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Add(CC.T, Dest.PBR, OpBD.Temp2, OpBD.PBR);
            asm.Mmsub(CC.T, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne, OpAC.Temp2, OpBD.Temp1);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.Temp2, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointOverlayPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.5f, 0.5f, 0.5f);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.PBR, OpBD.ConstantRGB);
            asm.Mmadd(CC.LE, Dest.Temp0, OpAC.Temp2, OpBD.Temp1, OpAC.Temp2, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.Temp0, OpBD.ConstantOne, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.PBR, OpBD.ConstantOne, OpBD.Temp2);
            asm.Mmadd(CC.GT, Dest.PBR, OpAC.Temp0, OpBD.PBR, OpAC.Temp0, OpBD.PBR);
            asm.Sub(CC.GT, Dest.Temp0, OpBD.ConstantOne, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.Temp2, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointDarkenPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.Temp0, OpAC.Temp2, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.Temp2, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointLightenPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Max(CC.T, Dest.Temp0, OpAC.Temp2, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.Temp2, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointColorDodgePremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.ConstantOne, OpBD.Temp2);
            asm.Rcp(CC.GT, Dest.PBR, OpAC.Temp0);
            asm.Mul(CC.GT, Dest.PBR, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.GT, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.LE, Dest.Temp0, OpBD.ConstantOne);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.Temp1, OpBD.ConstantZero);
            asm.Mov(CC.LE, Dest.Temp0, OpBD.ConstantZero);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.Temp2, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointColorBurnPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.Temp2, OpBD.ConstantZero);
            asm.Rcp(CC.GT, Dest.PBR, OpAC.Temp2);
            asm.Mmsub(CC.GT, Dest.PBR, OpAC.PBR, OpBD.ConstantOne, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.Temp0, OpBD.ConstantOne, OpBD.PBR);
            asm.Max(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.ConstantZero);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.ConstantOne, OpBD.Temp1);
            asm.Mov(CC.LE, Dest.Temp0, OpBD.ConstantOne);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.Temp2, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointHardLightPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.5f, 0.5f, 0.5f);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.Temp2, OpBD.ConstantRGB);
            asm.Mmadd(CC.LE, Dest.Temp0, OpAC.Temp2, OpBD.Temp1, OpAC.Temp2, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.Temp0, OpBD.ConstantOne, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.PBR, OpBD.ConstantOne, OpBD.Temp2);
            asm.Mmadd(CC.GT, Dest.PBR, OpAC.Temp0, OpBD.PBR, OpAC.Temp0, OpBD.PBR);
            asm.Sub(CC.GT, Dest.Temp0, OpBD.ConstantOne, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.Temp2, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointSoftLightPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(4, 0.25f, 0.25f, 0.25f);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.ConstantRGB);
            asm.SetConstant(0, 0.2605f, 0.2605f, 0.2605f);
            asm.Mul(CC.GT, Dest.PBR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.SetConstant(1, -0.7817f, -0.7817f, -0.7817f);
            asm.Mmadd(CC.GT, Dest.PBR, OpAC.Temp1, OpBD.PBR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.SetConstant(2, 0.3022f, 0.3022f, 0.3022f);
            asm.Mmadd(CC.GT, Dest.PBR, OpAC.Temp1, OpBD.PBR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.SetConstant(3, 0.2192f, 0.2192f, 0.2192f);
            asm.Add(CC.GT, Dest.Temp0, OpBD.PBR, OpBD.ConstantRGB);
            asm.SetConstant(5, 16f, 16f, 16f);
            asm.Mul(CC.LE, Dest.PBR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.SetConstant(6, 12f, 12f, 12f);
            asm.Mmsub(CC.LE, Dest.PBR, OpAC.Temp1, OpBD.PBR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.SetConstant(7, 3f, 3f, 3f);
            asm.Mmadd(CC.LE, Dest.Temp0, OpAC.Temp1, OpBD.PBR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.Add(CC.T, Dest.PBR, OpBD.Temp2, OpBD.Temp2);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.ConstantOne);
            asm.Mmsub(CC.LE, Dest.Temp0, OpAC.Temp1, OpBD.ConstantOne, OpAC.Temp1, OpBD.Temp1);
            asm.Add(CC.T, Dest.PBR, OpBD.Temp2, OpBD.Temp2);
            asm.Sub(CC.T, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.Temp2, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointDifferencePremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.PBR, OpBD.Temp2);
            asm.Sub(CC.LT, Dest.Temp0, OpBD.Temp2, OpBD.Temp1);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.Temp2, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointExclusionPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Add(CC.T, Dest.PBR, OpBD.Temp2, OpBD.PBR);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.ConstantOne, OpAC.Temp2, OpBD.Temp1);
            asm.Mmsub(CC.T, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne, OpAC.Temp2, OpBD.Temp1);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.Temp2, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointInvertPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.Temp0, OpBD.ConstantOne, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.Temp0, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp1, OpBD.PBR, OpAC.Temp0);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenDisjointInvertRGBPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Mmsub(CC.T, Dest.Temp0, OpAC.Temp2, OpBD.ConstantOne, OpAC.Temp2, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.Temp0, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp1, OpBD.PBR, OpAC.Temp0);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenDisjointLinearDodgePremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Add(CC.T, Dest.PBR, OpBD.Temp2, OpBD.PBR);
            asm.Min(CC.T, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.Temp2, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointLinearBurnPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Add(CC.T, Dest.PBR, OpBD.Temp2, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Max(CC.T, Dest.Temp0, OpAC.PBR, OpBD.ConstantZero);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.Temp2, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointVividLightPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.5f, 0.5f, 0.5f);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.Temp2, OpBD.ConstantRGB);
            asm.Sub(CC.GE, Dest.PBR, OpBD.ConstantOne, OpBD.Temp2);
            asm.Add(CC.GE, Dest.PBR, OpBD.PBR, OpBD.PBR);
            asm.Rcp(CC.GE, Dest.PBR, OpAC.PBR);
            asm.Mul(CC.GE, Dest.PBR, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.GE, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne);
            asm.Add(CC.LT, Dest.PBR, OpBD.Temp2, OpBD.Temp2);
            asm.Rcp(CC.LT, Dest.PBR, OpAC.PBR);
            asm.Mmsub(CC.LT, Dest.PBR, OpAC.PBR, OpBD.ConstantOne, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.LT, Dest.PBR, OpAC.PBR, OpBD.ConstantOne);
            asm.Sub(CC.LT, Dest.Temp0, OpBD.ConstantOne, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.Temp2, OpBD.ConstantZero);
            asm.Mul(CC.LE, Dest.Temp0, OpAC.SrcAAA, OpBD.ConstantZero);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.Temp2, OpBD.ConstantOne);
            asm.Mov(CC.GE, Dest.Temp0, OpBD.ConstantOne);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.Temp2, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointLinearLightPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 2f, 2f, 2f);
            asm.Madd(CC.T, Dest.PBR, OpAC.Temp2, OpBD.ConstantRGB, OpAC.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Max(CC.T, Dest.PBR, OpAC.PBR, OpBD.ConstantZero);
            asm.Min(CC.T, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.Temp2, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointPinLightPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Add(CC.T, Dest.PBR, OpBD.Temp2, OpBD.Temp2);
            asm.Sub(CC.T, Dest.Temp0, OpBD.PBR, OpBD.ConstantOne);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.Temp1);
            asm.Max(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.ConstantZero);
            asm.Add(CC.LE, Dest.PBR, OpBD.Temp2, OpBD.Temp2);
            asm.Min(CC.LE, Dest.Temp0, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.Temp2, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointHardMixPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Add(CC.T, Dest.PBR, OpBD.Temp2, OpBD.PBR);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.PBR, OpBD.ConstantOne);
            asm.Mul(CC.LT, Dest.Temp0, OpAC.SrcAAA, OpBD.ConstantZero);
            asm.Mov(CC.GE, Dest.Temp0, OpBD.ConstantOne);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.Temp2, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointHslHuePremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.Temp0.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.PBR, OpBD.Temp0);
            asm.Rcp(CC.GT, Dest.Temp0, OpAC.Temp0);
            asm.Mov(CC.GT, Dest.PBR.GBR, OpBD.Temp2);
            asm.Min(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Min(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.Temp2, OpAC.Temp0, OpBD.PBR);
            asm.Mov(CC.GT, Dest.PBR.GBR, OpBD.Temp1);
            asm.Min(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.GT, Dest.Temp2.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Mov(CC.GT, Dest.PBR.GBR, OpBD.Temp1);
            asm.Max(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Max(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp0, OpBD.Temp2);
            asm.Mul(CC.LE, Dest.Temp0, OpAC.SrcAAA, OpBD.ConstantZero);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.3f, 0.59f, 0.11f);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.PBR, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.Temp1.BBB, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.Temp0, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp0, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.PBR.BBB, OpAC.Temp0, OpBD.ConstantRGB, OpAC.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.Temp1, OpBD.PBR);
            asm.Add(CC.T, Dest.Temp2, OpBD.Temp0, OpBD.PBR);
            asm.Mov(CC.T, Dest.Temp0, OpBD.PBR);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.ConstantOne);
            asm.Add(CC.GT, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Sub(CC.GT, Dest.PBR, OpBD.PBR, OpBD.Temp1);
            asm.Rcp(CC.GT, Dest.PBR, OpAC.PBR);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.PBR, OpBD.Temp2, OpBD.Temp1);
            asm.Madd(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR.CC, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.LT, Dest.PBR, OpBD.Temp1, OpBD.PBR);
            asm.Rcp(CC.LT, Dest.Temp0, OpAC.PBR);
            asm.Mmsub(CC.LT, Dest.PBR, OpAC.Temp2, OpBD.Temp1, OpAC.Temp1, OpBD.Temp1);
            asm.Madd(CC.LT, Dest.Temp0, OpAC.PBR, OpBD.Temp0, OpAC.Temp1);
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.Temp2, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointHslSaturationPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.T, Dest.Temp0.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp1);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.PBR, OpBD.Temp0);
            asm.Rcp(CC.GT, Dest.Temp0, OpAC.Temp0);
            asm.Mov(CC.GT, Dest.PBR.GBR, OpBD.Temp1);
            asm.Min(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.Temp1, OpAC.Temp0, OpBD.PBR);
            asm.Mov(CC.GT, Dest.PBR.GBR, OpBD.Temp2);
            asm.Min(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Min(CC.GT, Dest.Temp1.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Mov(CC.GT, Dest.PBR.GBR, OpBD.Temp2);
            asm.Max(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Max(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp0, OpBD.Temp1);
            asm.Mul(CC.LE, Dest.Temp0, OpAC.SrcAAA, OpBD.ConstantZero);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.3f, 0.59f, 0.11f);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.PBR, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.Temp1.BBB, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.Temp0, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp0, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.PBR.BBB, OpAC.Temp0, OpBD.ConstantRGB, OpAC.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.Temp1, OpBD.PBR);
            asm.Add(CC.T, Dest.Temp2, OpBD.Temp0, OpBD.PBR);
            asm.Mov(CC.T, Dest.Temp0, OpBD.PBR);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.ConstantOne);
            asm.Add(CC.GT, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Sub(CC.GT, Dest.PBR, OpBD.PBR, OpBD.Temp1);
            asm.Rcp(CC.GT, Dest.PBR, OpAC.PBR);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.PBR, OpBD.Temp2, OpBD.Temp1);
            asm.Madd(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR.CC, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.LT, Dest.PBR, OpBD.Temp1, OpBD.PBR);
            asm.Rcp(CC.LT, Dest.Temp0, OpAC.PBR);
            asm.Mmsub(CC.LT, Dest.PBR, OpAC.Temp2, OpBD.Temp1, OpAC.Temp1, OpBD.Temp1);
            asm.Madd(CC.LT, Dest.Temp0, OpAC.PBR, OpBD.Temp0, OpAC.Temp1);
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.Temp2, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointHslColorPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.3f, 0.59f, 0.11f);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.PBR, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.Temp1.BBB, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.Temp2, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp2, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.PBR.BBB, OpAC.Temp2, OpBD.ConstantRGB, OpAC.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.Temp1, OpBD.PBR);
            asm.Add(CC.T, Dest.Temp2, OpBD.Temp2, OpBD.PBR);
            asm.Mov(CC.T, Dest.Temp0, OpBD.PBR);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.ConstantOne);
            asm.Add(CC.GT, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Sub(CC.GT, Dest.PBR, OpBD.PBR, OpBD.Temp1);
            asm.Rcp(CC.GT, Dest.PBR, OpAC.PBR);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.PBR, OpBD.Temp2, OpBD.Temp1);
            asm.Madd(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR.CC, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.LT, Dest.PBR, OpBD.Temp1, OpBD.PBR);
            asm.Rcp(CC.LT, Dest.Temp0, OpAC.PBR);
            asm.Mmsub(CC.LT, Dest.PBR, OpAC.Temp2, OpBD.Temp1, OpAC.Temp1, OpBD.Temp1);
            asm.Madd(CC.LT, Dest.Temp0, OpAC.PBR, OpBD.Temp0, OpAC.Temp1);
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.Temp2, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointHslLuminosityPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.3f, 0.59f, 0.11f);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.Temp2, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp2, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.Temp2.BBB, OpAC.Temp2, OpBD.ConstantRGB, OpAC.PBR);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.PBR.BBB, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.Temp2, OpBD.PBR);
            asm.Add(CC.T, Dest.Temp1, OpBD.Temp1, OpBD.PBR);
            asm.Mov(CC.T, Dest.Temp0, OpBD.PBR);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp1);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.ConstantOne);
            asm.Add(CC.GT, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Sub(CC.GT, Dest.PBR, OpBD.PBR, OpBD.Temp2);
            asm.Rcp(CC.GT, Dest.PBR, OpAC.PBR);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.GT, Dest.PBR, OpBD.Temp1, OpBD.Temp2);
            asm.Madd(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp2);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp1);
            asm.Min(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.T, Dest.PBR.GBR.CC, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.LT, Dest.PBR, OpBD.Temp2, OpBD.PBR);
            asm.Rcp(CC.LT, Dest.Temp0, OpAC.PBR);
            asm.Mmsub(CC.LT, Dest.PBR, OpAC.Temp1, OpBD.Temp2, OpAC.Temp2, OpBD.Temp2);
            asm.Madd(CC.LT, Dest.Temp0, OpAC.PBR, OpBD.Temp0, OpAC.Temp2);
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.Temp2, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenConjointSrcPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.SrcAAA);
            asm.Mul(CC.T, Dest.Temp0, OpAC.Temp2, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Max(CC.T, Dest.PBR, OpAC.PBR, OpBD.ConstantZero);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp2, OpBD.PBR, OpAC.Temp0);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.ZeroGl);
        }

        private static FixedFunctionAlpha GenConjointDstPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.SrcAAA);
            asm.Mul(CC.T, Dest.Temp0, OpAC.Temp1, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Max(CC.T, Dest.PBR, OpAC.PBR, OpBD.ConstantZero);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp1, OpBD.PBR, OpAC.Temp0);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointSrcOverPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp2);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp2, OpBD.DstAAA, OpAC.Temp2, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointDstOverPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Mov(CC.T, Dest.Temp0, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp1, OpBD.DstAAA, OpAC.Temp2, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointSrcInPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.SrcAAA);
            asm.Mul(CC.T, Dest.Temp0, OpAC.Temp2, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MinimumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointDstInPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.SrcAAA);
            asm.Mul(CC.T, Dest.Temp0, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MinimumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointSrcOutPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Max(CC.T, Dest.PBR, OpAC.PBR, OpBD.ConstantZero);
            asm.Mul(CC.T, Dest.Temp0, OpAC.Temp2, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Max(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantZero);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenConjointDstOutPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Max(CC.T, Dest.PBR, OpAC.PBR, OpBD.ConstantZero);
            asm.Mul(CC.T, Dest.Temp0, OpAC.Temp1, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Max(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantZero);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenConjointSrcAtopPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.SrcAAA);
            asm.Mul(CC.T, Dest.Temp0, OpAC.Temp2, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Max(CC.T, Dest.PBR, OpAC.PBR, OpBD.ConstantZero);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp1, OpBD.PBR, OpAC.Temp0);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointDstAtopPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.SrcAAA);
            asm.Mul(CC.T, Dest.Temp0, OpAC.Temp1, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Max(CC.T, Dest.PBR, OpAC.PBR, OpBD.ConstantZero);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp2, OpBD.PBR, OpAC.Temp0);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.ZeroGl);
        }

        private static FixedFunctionAlpha GenConjointXorPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Max(CC.T, Dest.PBR, OpAC.PBR, OpBD.ConstantZero);
            asm.Mul(CC.T, Dest.Temp0, OpAC.Temp2, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Max(CC.T, Dest.PBR, OpAC.PBR, OpBD.ConstantZero);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp1, OpBD.PBR, OpAC.Temp0);
            asm.Sub(CC.T, Dest.Temp1.CC, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Sub(CC.LT, Dest.Temp1, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mov(CC.T, Dest.Temp1.RToA, OpBD.Temp1);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenConjointMultiplyPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Mul(CC.T, Dest.Temp0, OpAC.Temp2, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.Temp2, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointScreenPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Add(CC.T, Dest.PBR, OpBD.Temp2, OpBD.PBR);
            asm.Mmsub(CC.T, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne, OpAC.Temp2, OpBD.Temp1);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.Temp2, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointOverlayPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.5f, 0.5f, 0.5f);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.PBR, OpBD.ConstantRGB);
            asm.Mmadd(CC.LE, Dest.Temp0, OpAC.Temp2, OpBD.Temp1, OpAC.Temp2, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.Temp0, OpBD.ConstantOne, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.PBR, OpBD.ConstantOne, OpBD.Temp2);
            asm.Mmadd(CC.GT, Dest.PBR, OpAC.Temp0, OpBD.PBR, OpAC.Temp0, OpBD.PBR);
            asm.Sub(CC.GT, Dest.Temp0, OpBD.ConstantOne, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.Temp2, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointDarkenPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.Temp0, OpAC.Temp2, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.Temp2, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointLightenPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Max(CC.T, Dest.Temp0, OpAC.Temp2, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.Temp2, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointColorDodgePremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.ConstantOne, OpBD.Temp2);
            asm.Rcp(CC.GT, Dest.PBR, OpAC.Temp0);
            asm.Mul(CC.GT, Dest.PBR, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.GT, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.LE, Dest.Temp0, OpBD.ConstantOne);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.Temp1, OpBD.ConstantZero);
            asm.Mov(CC.LE, Dest.Temp0, OpBD.ConstantZero);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.Temp2, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointColorBurnPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.Temp2, OpBD.ConstantZero);
            asm.Rcp(CC.GT, Dest.PBR, OpAC.Temp2);
            asm.Mmsub(CC.GT, Dest.PBR, OpAC.PBR, OpBD.ConstantOne, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.Temp0, OpBD.ConstantOne, OpBD.PBR);
            asm.Max(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.ConstantZero);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.ConstantOne, OpBD.Temp1);
            asm.Mov(CC.LE, Dest.Temp0, OpBD.ConstantOne);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.Temp2, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointHardLightPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.5f, 0.5f, 0.5f);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.Temp2, OpBD.ConstantRGB);
            asm.Mmadd(CC.LE, Dest.Temp0, OpAC.Temp2, OpBD.Temp1, OpAC.Temp2, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.Temp0, OpBD.ConstantOne, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.PBR, OpBD.ConstantOne, OpBD.Temp2);
            asm.Mmadd(CC.GT, Dest.PBR, OpAC.Temp0, OpBD.PBR, OpAC.Temp0, OpBD.PBR);
            asm.Sub(CC.GT, Dest.Temp0, OpBD.ConstantOne, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.Temp2, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointSoftLightPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(4, 0.25f, 0.25f, 0.25f);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.ConstantRGB);
            asm.SetConstant(0, 0.2605f, 0.2605f, 0.2605f);
            asm.Mul(CC.GT, Dest.PBR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.SetConstant(1, -0.7817f, -0.7817f, -0.7817f);
            asm.Mmadd(CC.GT, Dest.PBR, OpAC.Temp1, OpBD.PBR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.SetConstant(2, 0.3022f, 0.3022f, 0.3022f);
            asm.Mmadd(CC.GT, Dest.PBR, OpAC.Temp1, OpBD.PBR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.SetConstant(3, 0.2192f, 0.2192f, 0.2192f);
            asm.Add(CC.GT, Dest.Temp0, OpBD.PBR, OpBD.ConstantRGB);
            asm.SetConstant(5, 16f, 16f, 16f);
            asm.Mul(CC.LE, Dest.PBR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.SetConstant(6, 12f, 12f, 12f);
            asm.Mmsub(CC.LE, Dest.PBR, OpAC.Temp1, OpBD.PBR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.SetConstant(7, 3f, 3f, 3f);
            asm.Mmadd(CC.LE, Dest.Temp0, OpAC.Temp1, OpBD.PBR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.Add(CC.T, Dest.PBR, OpBD.Temp2, OpBD.Temp2);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.ConstantOne);
            asm.Mmsub(CC.LE, Dest.Temp0, OpAC.Temp1, OpBD.ConstantOne, OpAC.Temp1, OpBD.Temp1);
            asm.Add(CC.T, Dest.PBR, OpBD.Temp2, OpBD.Temp2);
            asm.Sub(CC.T, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.Temp2, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointDifferencePremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.PBR, OpBD.Temp2);
            asm.Sub(CC.LT, Dest.Temp0, OpBD.Temp2, OpBD.Temp1);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.Temp2, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointExclusionPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Add(CC.T, Dest.PBR, OpBD.Temp2, OpBD.PBR);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.ConstantOne, OpAC.Temp2, OpBD.Temp1);
            asm.Mmsub(CC.T, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne, OpAC.Temp2, OpBD.Temp1);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.Temp2, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointInvertPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.Temp0, OpBD.ConstantOne, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.SrcAAA);
            asm.Mul(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Max(CC.T, Dest.PBR, OpAC.PBR, OpBD.ConstantZero);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp1, OpBD.PBR, OpAC.Temp0);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointInvertRGBPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Mmsub(CC.T, Dest.Temp0, OpAC.Temp2, OpBD.ConstantOne, OpAC.Temp2, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.SrcAAA);
            asm.Mul(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Max(CC.T, Dest.PBR, OpAC.PBR, OpBD.ConstantZero);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp1, OpBD.PBR, OpAC.Temp0);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointLinearDodgePremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Add(CC.T, Dest.PBR, OpBD.Temp2, OpBD.PBR);
            asm.Min(CC.T, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.Temp2, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointLinearBurnPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Add(CC.T, Dest.PBR, OpBD.Temp2, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Max(CC.T, Dest.Temp0, OpAC.PBR, OpBD.ConstantZero);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.Temp2, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointVividLightPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.5f, 0.5f, 0.5f);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.Temp2, OpBD.ConstantRGB);
            asm.Sub(CC.GE, Dest.PBR, OpBD.ConstantOne, OpBD.Temp2);
            asm.Add(CC.GE, Dest.PBR, OpBD.PBR, OpBD.PBR);
            asm.Rcp(CC.GE, Dest.PBR, OpAC.PBR);
            asm.Mul(CC.GE, Dest.PBR, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.GE, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne);
            asm.Add(CC.LT, Dest.PBR, OpBD.Temp2, OpBD.Temp2);
            asm.Rcp(CC.LT, Dest.PBR, OpAC.PBR);
            asm.Mmsub(CC.LT, Dest.PBR, OpAC.PBR, OpBD.ConstantOne, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.LT, Dest.PBR, OpAC.PBR, OpBD.ConstantOne);
            asm.Sub(CC.LT, Dest.Temp0, OpBD.ConstantOne, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.Temp2, OpBD.ConstantZero);
            asm.Mul(CC.LE, Dest.Temp0, OpAC.SrcAAA, OpBD.ConstantZero);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.Temp2, OpBD.ConstantOne);
            asm.Mov(CC.GE, Dest.Temp0, OpBD.ConstantOne);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.Temp2, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointLinearLightPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 2f, 2f, 2f);
            asm.Madd(CC.T, Dest.PBR, OpAC.Temp2, OpBD.ConstantRGB, OpAC.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Max(CC.T, Dest.PBR, OpAC.PBR, OpBD.ConstantZero);
            asm.Min(CC.T, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.Temp2, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointPinLightPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Add(CC.T, Dest.PBR, OpBD.Temp2, OpBD.Temp2);
            asm.Sub(CC.T, Dest.Temp0, OpBD.PBR, OpBD.ConstantOne);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.Temp1);
            asm.Max(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.ConstantZero);
            asm.Add(CC.LE, Dest.PBR, OpBD.Temp2, OpBD.Temp2);
            asm.Min(CC.LE, Dest.Temp0, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.Temp2, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointHardMixPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Add(CC.T, Dest.PBR, OpBD.Temp2, OpBD.PBR);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.PBR, OpBD.ConstantOne);
            asm.Mul(CC.LT, Dest.Temp0, OpAC.SrcAAA, OpBD.ConstantZero);
            asm.Mov(CC.GE, Dest.Temp0, OpBD.ConstantOne);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.Temp2, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointHslHuePremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.Temp0.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.PBR, OpBD.Temp0);
            asm.Rcp(CC.GT, Dest.Temp0, OpAC.Temp0);
            asm.Mov(CC.GT, Dest.PBR.GBR, OpBD.Temp2);
            asm.Min(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Min(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.Temp2, OpAC.Temp0, OpBD.PBR);
            asm.Mov(CC.GT, Dest.PBR.GBR, OpBD.Temp1);
            asm.Min(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.GT, Dest.Temp2.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Mov(CC.GT, Dest.PBR.GBR, OpBD.Temp1);
            asm.Max(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Max(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp0, OpBD.Temp2);
            asm.Mul(CC.LE, Dest.Temp0, OpAC.SrcAAA, OpBD.ConstantZero);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.3f, 0.59f, 0.11f);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.PBR, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.Temp1.BBB, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.Temp0, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp0, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.PBR.BBB, OpAC.Temp0, OpBD.ConstantRGB, OpAC.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.Temp1, OpBD.PBR);
            asm.Add(CC.T, Dest.Temp2, OpBD.Temp0, OpBD.PBR);
            asm.Mov(CC.T, Dest.Temp0, OpBD.PBR);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.ConstantOne);
            asm.Add(CC.GT, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Sub(CC.GT, Dest.PBR, OpBD.PBR, OpBD.Temp1);
            asm.Rcp(CC.GT, Dest.PBR, OpAC.PBR);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.PBR, OpBD.Temp2, OpBD.Temp1);
            asm.Madd(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR.CC, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.LT, Dest.PBR, OpBD.Temp1, OpBD.PBR);
            asm.Rcp(CC.LT, Dest.Temp0, OpAC.PBR);
            asm.Mmsub(CC.LT, Dest.PBR, OpAC.Temp2, OpBD.Temp1, OpAC.Temp1, OpBD.Temp1);
            asm.Madd(CC.LT, Dest.Temp0, OpAC.PBR, OpBD.Temp0, OpAC.Temp1);
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.Temp2, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointHslSaturationPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.T, Dest.Temp0.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp1);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.PBR, OpBD.Temp0);
            asm.Rcp(CC.GT, Dest.Temp0, OpAC.Temp0);
            asm.Mov(CC.GT, Dest.PBR.GBR, OpBD.Temp1);
            asm.Min(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.Temp1, OpAC.Temp0, OpBD.PBR);
            asm.Mov(CC.GT, Dest.PBR.GBR, OpBD.Temp2);
            asm.Min(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Min(CC.GT, Dest.Temp1.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Mov(CC.GT, Dest.PBR.GBR, OpBD.Temp2);
            asm.Max(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Max(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp0, OpBD.Temp1);
            asm.Mul(CC.LE, Dest.Temp0, OpAC.SrcAAA, OpBD.ConstantZero);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.3f, 0.59f, 0.11f);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.PBR, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.Temp1.BBB, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.Temp0, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp0, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.PBR.BBB, OpAC.Temp0, OpBD.ConstantRGB, OpAC.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.Temp1, OpBD.PBR);
            asm.Add(CC.T, Dest.Temp2, OpBD.Temp0, OpBD.PBR);
            asm.Mov(CC.T, Dest.Temp0, OpBD.PBR);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.ConstantOne);
            asm.Add(CC.GT, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Sub(CC.GT, Dest.PBR, OpBD.PBR, OpBD.Temp1);
            asm.Rcp(CC.GT, Dest.PBR, OpAC.PBR);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.PBR, OpBD.Temp2, OpBD.Temp1);
            asm.Madd(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR.CC, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.LT, Dest.PBR, OpBD.Temp1, OpBD.PBR);
            asm.Rcp(CC.LT, Dest.Temp0, OpAC.PBR);
            asm.Mmsub(CC.LT, Dest.PBR, OpAC.Temp2, OpBD.Temp1, OpAC.Temp1, OpBD.Temp1);
            asm.Madd(CC.LT, Dest.Temp0, OpAC.PBR, OpBD.Temp0, OpAC.Temp1);
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.Temp2, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointHslColorPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.3f, 0.59f, 0.11f);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.PBR, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.Temp1.BBB, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.Temp2, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp2, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.PBR.BBB, OpAC.Temp2, OpBD.ConstantRGB, OpAC.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.Temp1, OpBD.PBR);
            asm.Add(CC.T, Dest.Temp2, OpBD.Temp2, OpBD.PBR);
            asm.Mov(CC.T, Dest.Temp0, OpBD.PBR);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.ConstantOne);
            asm.Add(CC.GT, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Sub(CC.GT, Dest.PBR, OpBD.PBR, OpBD.Temp1);
            asm.Rcp(CC.GT, Dest.PBR, OpAC.PBR);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.PBR, OpBD.Temp2, OpBD.Temp1);
            asm.Madd(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR.CC, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.LT, Dest.PBR, OpBD.Temp1, OpBD.PBR);
            asm.Rcp(CC.LT, Dest.Temp0, OpAC.PBR);
            asm.Mmsub(CC.LT, Dest.PBR, OpAC.Temp2, OpBD.Temp1, OpAC.Temp1, OpBD.Temp1);
            asm.Madd(CC.LT, Dest.Temp0, OpAC.PBR, OpBD.Temp0, OpAC.Temp1);
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.Temp2, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointHslLuminosityPremul(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.3f, 0.59f, 0.11f);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.Temp2, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp2, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.Temp2.BBB, OpAC.Temp2, OpBD.ConstantRGB, OpAC.PBR);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.PBR.BBB, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.Temp2, OpBD.PBR);
            asm.Add(CC.T, Dest.Temp1, OpBD.Temp1, OpBD.PBR);
            asm.Mov(CC.T, Dest.Temp0, OpBD.PBR);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp1);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.ConstantOne);
            asm.Add(CC.GT, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Sub(CC.GT, Dest.PBR, OpBD.PBR, OpBD.Temp2);
            asm.Rcp(CC.GT, Dest.PBR, OpAC.PBR);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.GT, Dest.PBR, OpBD.Temp1, OpBD.Temp2);
            asm.Madd(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp2);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp1);
            asm.Min(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.T, Dest.PBR.GBR.CC, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.LT, Dest.PBR, OpBD.Temp2, OpBD.PBR);
            asm.Rcp(CC.LT, Dest.Temp0, OpAC.PBR);
            asm.Mmsub(CC.LT, Dest.PBR, OpAC.Temp1, OpBD.Temp2, OpAC.Temp2, OpBD.Temp2);
            asm.Madd(CC.LT, Dest.Temp0, OpAC.PBR, OpBD.Temp0, OpAC.Temp2);
            asm.Rcp(CC.T, Dest.PBR, OpAC.SrcAAA);
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.PBR);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.Temp2, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedDstOver(ref UcodeAssembler asm)
        {
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.SrcAAA);
            asm.Mul(CC.T, Dest.Temp0, OpAC.DstRGB, OpBD.SrcAAA);
            asm.Mmadd(CC.T, Dest.PBR, OpAC.Temp2, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Add(CC.T, Dest.Temp0, OpBD.Temp0, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedSrcIn(ref UcodeAssembler asm)
        {
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcRGB, OpBD.SrcAAA);
            asm.Mul(CC.T, Dest.PBR, OpAC.PBR, OpBD.DstAAA);
            asm.Mov(CC.T, Dest.Temp0, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.DstAlphaGl, BlendFactor.ZeroGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedSrcOut(ref UcodeAssembler asm)
        {
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcRGB, OpBD.SrcAAA);
            asm.Mul(CC.T, Dest.Temp0, OpAC.PBR, OpBD.OneMinusDstAAA);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneMinusDstAlphaGl, BlendFactor.ZeroGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedSrcAtop(ref UcodeAssembler asm)
        {
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcRGB, OpBD.SrcAAA);
            asm.Mul(CC.T, Dest.PBR, OpAC.PBR, OpBD.DstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.DstRGB, OpBD.OneMinusSrcAAA, OpAC.PBR);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedDstAtop(ref UcodeAssembler asm)
        {
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.SrcAAA);
            asm.Mul(CC.T, Dest.PBR, OpAC.DstRGB, OpBD.SrcAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp2, OpBD.OneMinusDstAAA, OpAC.PBR);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.ZeroGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedXor(ref UcodeAssembler asm)
        {
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcRGB, OpBD.SrcAAA);
            asm.Mul(CC.T, Dest.PBR, OpAC.PBR, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.DstRGB, OpBD.OneMinusSrcAAA, OpAC.PBR);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneMinusDstAlphaGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedPlusClamped(ref UcodeAssembler asm)
        {
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcRGB, OpBD.SrcAAA);
            asm.Add(CC.T, Dest.PBR, OpBD.DstRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenUncorrelatedPlusClampedAlpha(ref UcodeAssembler asm)
        {
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcRGB, OpBD.SrcAAA);
            asm.Add(CC.T, Dest.Temp0, OpBD.DstRGB, OpBD.PBR);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.PBR, OpAC.PBR, OpBD.ConstantOne);
            asm.Min(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenUncorrelatedPlusDarker(ref UcodeAssembler asm)
        {
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.SrcAAA);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.PBR, OpAC.PBR, OpBD.ConstantOne);
            asm.Add(CC.T, Dest.PBR, OpBD.PBR, OpBD.Temp2);
            asm.Add(CC.T, Dest.PBR, OpBD.PBR, OpBD.DstRGB);
            asm.Sub(CC.T, Dest.PBR, OpBD.PBR, OpBD.SrcAAA);
            asm.Sub(CC.T, Dest.PBR, OpBD.PBR, OpBD.DstAAA);
            asm.Max(CC.T, Dest.Temp0, OpAC.PBR, OpBD.ConstantZero);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenUncorrelatedMultiply(ref UcodeAssembler asm)
        {
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.SrcAAA);
            asm.Mul(CC.T, Dest.Temp0, OpAC.PBR, OpBD.DstRGB);
            asm.Mmadd(CC.T, Dest.PBR, OpAC.Temp2, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Add(CC.T, Dest.Temp0, OpBD.Temp0, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedScreen(ref UcodeAssembler asm)
        {
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.SrcAAA);
            asm.Mmadd(CC.T, Dest.PBR, OpAC.PBR, OpBD.DstAAA, OpAC.DstRGB, OpBD.SrcAAA);
            asm.Mmsub(CC.T, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne, OpAC.Temp2, OpBD.DstRGB);
            asm.Mmadd(CC.T, Dest.PBR, OpAC.Temp2, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Add(CC.T, Dest.Temp0, OpBD.Temp0, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedOverlay(ref UcodeAssembler asm)
        {
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.SrcAAA);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.5f, 0.5f, 0.5f);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.PBR, OpBD.ConstantRGB);
            asm.Mmadd(CC.LE, Dest.Temp0, OpAC.SrcRGB, OpBD.Temp1, OpAC.SrcRGB, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.Temp0, OpBD.ConstantOne, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.PBR, OpBD.ConstantOne, OpBD.SrcRGB);
            asm.Mmadd(CC.GT, Dest.PBR, OpAC.Temp0, OpBD.PBR, OpAC.Temp0, OpBD.PBR);
            asm.Sub(CC.GT, Dest.Temp0, OpBD.ConstantOne, OpBD.PBR);
            asm.Mmadd(CC.T, Dest.Temp1, OpAC.Temp2, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.DstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedDarken(ref UcodeAssembler asm)
        {
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.SrcAAA);
            asm.Mul(CC.T, Dest.Temp0, OpAC.PBR, OpBD.DstAAA);
            asm.Mul(CC.T, Dest.PBR, OpAC.DstRGB, OpBD.SrcAAA);
            asm.Min(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR);
            asm.Mmadd(CC.T, Dest.PBR, OpAC.Temp2, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Add(CC.T, Dest.Temp0, OpBD.Temp0, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedLighten(ref UcodeAssembler asm)
        {
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.SrcAAA);
            asm.Mul(CC.T, Dest.Temp0, OpAC.PBR, OpBD.DstAAA);
            asm.Mul(CC.T, Dest.PBR, OpAC.DstRGB, OpBD.SrcAAA);
            asm.Max(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR);
            asm.Mmadd(CC.T, Dest.PBR, OpAC.Temp2, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Add(CC.T, Dest.Temp0, OpBD.Temp0, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedColorDodge(ref UcodeAssembler asm)
        {
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.SrcAAA);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.PBR);
            asm.Rcp(CC.GT, Dest.PBR, OpAC.PBR);
            asm.Mul(CC.GT, Dest.PBR, OpAC.PBR, OpBD.SrcAAA);
            asm.Mul(CC.GT, Dest.PBR, OpAC.PBR, OpBD.DstRGB);
            asm.Min(CC.GT, Dest.PBR, OpAC.DstAAA, OpBD.PBR);
            asm.Mul(CC.GT, Dest.Temp0, OpAC.PBR, OpBD.SrcAAA);
            asm.Mul(CC.LE, Dest.Temp0, OpAC.SrcAAA, OpBD.DstAAA);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.DstRGB, OpBD.ConstantZero);
            asm.Mul(CC.LE, Dest.Temp0, OpAC.SrcAAA, OpBD.ConstantZero);
            asm.Mmadd(CC.T, Dest.PBR, OpAC.Temp2, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Add(CC.T, Dest.Temp0, OpBD.Temp0, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedColorBurn(ref UcodeAssembler asm)
        {
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.SrcAAA);
            asm.Mmsub(CC.T, Dest.Temp0, OpAC.DstAAA, OpBD.SrcAAA, OpAC.SrcAAA, OpBD.DstRGB);
            asm.Rcp(CC.T, Dest.PBR, OpAC.Temp2);
            asm.Mul(CC.T, Dest.PBR, OpAC.Temp0, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.PBR);
            asm.Mmsub(CC.T, Dest.Temp0, OpAC.SrcAAA, OpBD.DstAAA, OpAC.SrcAAA, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.Temp2, OpBD.ConstantZero);
            asm.Mul(CC.LE, Dest.Temp0, OpAC.SrcAAA, OpBD.ConstantZero);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.DstAAA, OpBD.DstRGB);
            asm.Mul(CC.LE, Dest.Temp0, OpAC.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.T, Dest.PBR, OpAC.Temp2, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Add(CC.T, Dest.Temp0, OpBD.Temp0, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedHardLight(ref UcodeAssembler asm)
        {
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.SrcAAA);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.5f, 0.5f, 0.5f);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.SrcRGB, OpBD.ConstantRGB);
            asm.Mmadd(CC.LE, Dest.Temp0, OpAC.SrcRGB, OpBD.Temp1, OpAC.SrcRGB, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.Temp0, OpBD.ConstantOne, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.PBR, OpBD.ConstantOne, OpBD.SrcRGB);
            asm.Mmadd(CC.GT, Dest.PBR, OpAC.Temp0, OpBD.PBR, OpAC.Temp0, OpBD.PBR);
            asm.Sub(CC.GT, Dest.Temp0, OpBD.ConstantOne, OpBD.PBR);
            asm.Mmadd(CC.T, Dest.Temp1, OpAC.Temp2, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.DstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedSoftLight(ref UcodeAssembler asm)
        {
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.SrcAAA);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(4, 0.25f, 0.25f, 0.25f);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.ConstantRGB);
            asm.SetConstant(0, 0.2605f, 0.2605f, 0.2605f);
            asm.Mul(CC.GT, Dest.PBR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.SetConstant(1, -0.7817f, -0.7817f, -0.7817f);
            asm.Mmadd(CC.GT, Dest.PBR, OpAC.Temp1, OpBD.PBR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.SetConstant(2, 0.3022f, 0.3022f, 0.3022f);
            asm.Mmadd(CC.GT, Dest.PBR, OpAC.Temp1, OpBD.PBR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.SetConstant(3, 0.2192f, 0.2192f, 0.2192f);
            asm.Add(CC.GT, Dest.Temp0, OpBD.PBR, OpBD.ConstantRGB);
            asm.SetConstant(5, 16f, 16f, 16f);
            asm.Mul(CC.LE, Dest.PBR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.SetConstant(6, 12f, 12f, 12f);
            asm.Mmsub(CC.LE, Dest.PBR, OpAC.Temp1, OpBD.PBR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.SetConstant(7, 3f, 3f, 3f);
            asm.Mmadd(CC.LE, Dest.Temp0, OpAC.Temp1, OpBD.PBR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcRGB, OpBD.SrcRGB);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.ConstantOne);
            asm.Mmsub(CC.LE, Dest.Temp0, OpAC.Temp1, OpBD.ConstantOne, OpAC.Temp1, OpBD.Temp1);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcRGB, OpBD.SrcRGB);
            asm.Sub(CC.T, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            asm.Mmadd(CC.T, Dest.Temp1, OpAC.Temp2, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.DstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedDifference(ref UcodeAssembler asm)
        {
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.SrcAAA);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.PBR, OpBD.SrcRGB);
            asm.Sub(CC.LT, Dest.Temp0, OpBD.SrcRGB, OpBD.Temp1);
            asm.Mmadd(CC.T, Dest.Temp1, OpAC.Temp2, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.DstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedMinus(ref UcodeAssembler asm)
        {
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcRGB, OpBD.SrcAAA);
            asm.Sub(CC.T, Dest.Temp0, OpBD.DstRGB, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.ReverseSubtractGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedMinusClamped(ref UcodeAssembler asm)
        {
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcRGB, OpBD.SrcAAA);
            asm.Sub(CC.T, Dest.PBR, OpBD.DstRGB, OpBD.PBR);
            asm.Max(CC.T, Dest.Temp0, OpAC.PBR, OpBD.ConstantZero);
            asm.Sub(CC.T, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Max(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantZero);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenUncorrelatedExclusion(ref UcodeAssembler asm)
        {
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.SrcAAA);
            asm.Mmadd(CC.T, Dest.PBR, OpAC.PBR, OpBD.DstAAA, OpAC.DstRGB, OpBD.SrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.ConstantOne, OpAC.Temp2, OpBD.DstRGB);
            asm.Mmsub(CC.T, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne, OpAC.Temp2, OpBD.DstRGB);
            asm.Mmadd(CC.T, Dest.PBR, OpAC.Temp2, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Add(CC.T, Dest.Temp0, OpBD.Temp0, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedContrast(ref UcodeAssembler asm)
        {
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.SrcAAA);
            asm.SetConstant(0, 2f, 2f, 2f);
            asm.Mmsub(CC.T, Dest.Temp0, OpAC.DstRGB, OpBD.ConstantRGB, OpAC.DstAAA, OpBD.ConstantOne);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.Temp2, OpBD.ConstantRGB, OpAC.SrcAAA, OpBD.ConstantOne);
            asm.Mul(CC.T, Dest.PBR, OpAC.Temp0, OpBD.PBR);
            asm.Add(CC.T, Dest.PBR, OpBD.PBR, OpBD.DstAAA);
            asm.SetConstant(1, 0.5f, 0.5f, 0.5f);
            asm.Mul(CC.T, Dest.Temp0, OpAC.PBR, OpBD.ConstantRGB);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedInvertRGB(ref UcodeAssembler asm)
        {
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcRGB, OpBD.SrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.DstAAA, OpAC.PBR, OpBD.DstRGB);
            asm.Madd(CC.T, Dest.Temp0, OpAC.DstRGB, OpBD.OneMinusSrcAAA, OpAC.PBR);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedLinearDodge(ref UcodeAssembler asm)
        {
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.SrcAAA);
            asm.Mmadd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.DstAAA, OpAC.DstRGB, OpBD.SrcAAA);
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR);
            asm.Mmadd(CC.T, Dest.PBR, OpAC.Temp2, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Add(CC.T, Dest.Temp0, OpBD.Temp0, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedLinearBurn(ref UcodeAssembler asm)
        {
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.SrcAAA);
            asm.Mmadd(CC.T, Dest.PBR, OpAC.PBR, OpBD.DstAAA, OpAC.DstRGB, OpBD.SrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.ConstantOne, OpAC.SrcAAA, OpBD.DstAAA);
            asm.Max(CC.T, Dest.Temp0, OpAC.PBR, OpBD.ConstantZero);
            asm.Mmadd(CC.T, Dest.PBR, OpAC.Temp2, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Add(CC.T, Dest.Temp0, OpBD.Temp0, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedVividLight(ref UcodeAssembler asm)
        {
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.SrcAAA);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.5f, 0.5f, 0.5f);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcRGB, OpBD.ConstantRGB);
            asm.Sub(CC.GE, Dest.PBR, OpBD.ConstantOne, OpBD.SrcRGB);
            asm.Add(CC.GE, Dest.PBR, OpBD.PBR, OpBD.PBR);
            asm.Rcp(CC.GE, Dest.PBR, OpAC.PBR);
            asm.Mul(CC.GE, Dest.PBR, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.GE, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne);
            asm.Add(CC.LT, Dest.PBR, OpBD.SrcRGB, OpBD.SrcRGB);
            asm.Rcp(CC.LT, Dest.PBR, OpAC.PBR);
            asm.Mmsub(CC.LT, Dest.PBR, OpAC.PBR, OpBD.ConstantOne, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.LT, Dest.PBR, OpAC.PBR, OpBD.ConstantOne);
            asm.Sub(CC.LT, Dest.Temp0, OpBD.ConstantOne, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcRGB, OpBD.ConstantZero);
            asm.Mul(CC.LE, Dest.Temp0, OpAC.SrcAAA, OpBD.ConstantZero);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcRGB, OpBD.ConstantOne);
            asm.Mov(CC.GE, Dest.Temp0, OpBD.ConstantOne);
            asm.Mmadd(CC.T, Dest.Temp1, OpAC.Temp2, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.DstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedLinearLight(ref UcodeAssembler asm)
        {
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.SrcAAA);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.PBR, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 2f, 2f, 2f);
            asm.Madd(CC.T, Dest.PBR, OpAC.SrcRGB, OpBD.ConstantRGB, OpAC.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Max(CC.T, Dest.PBR, OpAC.PBR, OpBD.ConstantZero);
            asm.Min(CC.T, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne);
            asm.Mmadd(CC.T, Dest.Temp1, OpAC.Temp2, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.DstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedPinLight(ref UcodeAssembler asm)
        {
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.SrcAAA);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcRGB, OpBD.SrcRGB);
            asm.Sub(CC.T, Dest.Temp0, OpBD.PBR, OpBD.ConstantOne);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.Temp1);
            asm.Max(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.ConstantZero);
            asm.Add(CC.LE, Dest.PBR, OpBD.SrcRGB, OpBD.SrcRGB);
            asm.Min(CC.LE, Dest.Temp0, OpAC.PBR, OpBD.Temp1);
            asm.Mmadd(CC.T, Dest.Temp1, OpAC.Temp2, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.DstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedHardMix(ref UcodeAssembler asm)
        {
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.SrcAAA);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.PBR, OpAC.DstRGB, OpBD.PBR);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.PBR, OpBD.ConstantOne);
            asm.Mul(CC.LT, Dest.Temp0, OpAC.SrcAAA, OpBD.ConstantZero);
            asm.Mov(CC.GE, Dest.Temp0, OpBD.ConstantOne);
            asm.Mmadd(CC.T, Dest.Temp1, OpAC.Temp2, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.DstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedRed(ref UcodeAssembler asm)
        {
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.SrcAAA);
            asm.Mov(CC.T, Dest.Temp0, OpBD.DstRGB);
            asm.Mov(CC.T, Dest.Temp0.R, OpBD.Temp2);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedGreen(ref UcodeAssembler asm)
        {
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.SrcAAA);
            asm.Mov(CC.T, Dest.Temp0, OpBD.DstRGB);
            asm.Mov(CC.T, Dest.Temp0.G, OpBD.Temp2);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedBlue(ref UcodeAssembler asm)
        {
            asm.Mul(CC.T, Dest.Temp2, OpAC.SrcRGB, OpBD.SrcAAA);
            asm.Mov(CC.T, Dest.Temp0, OpBD.DstRGB);
            asm.Mov(CC.T, Dest.Temp0.B, OpBD.Temp2);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedHslHue(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.SrcRGB);
            asm.Min(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.SrcRGB);
            asm.Min(CC.T, Dest.Temp0.GBR, OpAC.PBR, OpBD.SrcRGB);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.SrcRGB);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.SrcRGB);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.SrcRGB);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.PBR, OpBD.Temp0);
            asm.Rcp(CC.GT, Dest.Temp0, OpAC.Temp0);
            asm.Mov(CC.GT, Dest.PBR.GBR, OpBD.SrcRGB);
            asm.Min(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.SrcRGB);
            asm.Min(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.SrcRGB);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.SrcRGB, OpAC.Temp0, OpBD.PBR);
            asm.Mov(CC.GT, Dest.PBR.GBR, OpBD.Temp1);
            asm.Min(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.GT, Dest.Temp2.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Mov(CC.GT, Dest.PBR.GBR, OpBD.Temp1);
            asm.Max(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Max(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp0, OpBD.Temp2);
            asm.Mul(CC.LE, Dest.Temp0, OpAC.SrcAAA, OpBD.ConstantZero);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.3f, 0.59f, 0.11f);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.PBR, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.Temp1.BBB, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.Temp0, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp0, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.PBR.BBB, OpAC.Temp0, OpBD.ConstantRGB, OpAC.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.Temp1, OpBD.PBR);
            asm.Add(CC.T, Dest.Temp2, OpBD.Temp0, OpBD.PBR);
            asm.Mov(CC.T, Dest.Temp0, OpBD.PBR);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.ConstantOne);
            asm.Add(CC.GT, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Sub(CC.GT, Dest.PBR, OpBD.PBR, OpBD.Temp1);
            asm.Rcp(CC.GT, Dest.PBR, OpAC.PBR);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.PBR, OpBD.Temp2, OpBD.Temp1);
            asm.Madd(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR.CC, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.LT, Dest.PBR, OpBD.Temp1, OpBD.PBR);
            asm.Rcp(CC.LT, Dest.Temp0, OpAC.PBR);
            asm.Mmsub(CC.LT, Dest.PBR, OpAC.Temp2, OpBD.Temp1, OpAC.Temp1, OpBD.Temp1);
            asm.Madd(CC.LT, Dest.Temp0, OpAC.PBR, OpBD.Temp0, OpAC.Temp1);
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcRGB, OpBD.SrcAAA);
            asm.Mmadd(CC.T, Dest.Temp1, OpAC.PBR, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.DstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedHslSaturation(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.T, Dest.Temp0.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp1);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.PBR, OpBD.Temp0);
            asm.Rcp(CC.GT, Dest.Temp0, OpAC.Temp0);
            asm.Mov(CC.GT, Dest.PBR.GBR, OpBD.Temp1);
            asm.Min(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.Temp1, OpAC.Temp0, OpBD.PBR);
            asm.Mov(CC.GT, Dest.PBR.GBR, OpBD.SrcRGB);
            asm.Min(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.SrcRGB);
            asm.Min(CC.GT, Dest.Temp1.GBR, OpAC.PBR, OpBD.SrcRGB);
            asm.Mov(CC.GT, Dest.PBR.GBR, OpBD.SrcRGB);
            asm.Max(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.SrcRGB);
            asm.Max(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.SrcRGB);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp0, OpBD.Temp1);
            asm.Mul(CC.LE, Dest.Temp0, OpAC.SrcAAA, OpBD.ConstantZero);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.3f, 0.59f, 0.11f);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.PBR, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.Temp1.BBB, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.Temp0, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp0, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.PBR.BBB, OpAC.Temp0, OpBD.ConstantRGB, OpAC.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.Temp1, OpBD.PBR);
            asm.Add(CC.T, Dest.Temp2, OpBD.Temp0, OpBD.PBR);
            asm.Mov(CC.T, Dest.Temp0, OpBD.PBR);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.ConstantOne);
            asm.Add(CC.GT, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Sub(CC.GT, Dest.PBR, OpBD.PBR, OpBD.Temp1);
            asm.Rcp(CC.GT, Dest.PBR, OpAC.PBR);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.PBR, OpBD.Temp2, OpBD.Temp1);
            asm.Madd(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR.CC, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.LT, Dest.PBR, OpBD.Temp1, OpBD.PBR);
            asm.Rcp(CC.LT, Dest.Temp0, OpAC.PBR);
            asm.Mmsub(CC.LT, Dest.PBR, OpAC.Temp2, OpBD.Temp1, OpAC.Temp1, OpBD.Temp1);
            asm.Madd(CC.LT, Dest.Temp0, OpAC.PBR, OpBD.Temp0, OpAC.Temp1);
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcRGB, OpBD.SrcAAA);
            asm.Mmadd(CC.T, Dest.Temp1, OpAC.PBR, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.DstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedHslColor(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.3f, 0.59f, 0.11f);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.PBR, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.Temp1.BBB, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.SrcRGB, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.SrcRGB, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.PBR.BBB, OpAC.SrcRGB, OpBD.ConstantRGB, OpAC.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.Temp1, OpBD.PBR);
            asm.Add(CC.T, Dest.Temp2, OpBD.SrcRGB, OpBD.PBR);
            asm.Mov(CC.T, Dest.Temp0, OpBD.PBR);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.ConstantOne);
            asm.Add(CC.GT, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Sub(CC.GT, Dest.PBR, OpBD.PBR, OpBD.Temp1);
            asm.Rcp(CC.GT, Dest.PBR, OpAC.PBR);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.PBR, OpBD.Temp2, OpBD.Temp1);
            asm.Madd(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR.CC, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.LT, Dest.PBR, OpBD.Temp1, OpBD.PBR);
            asm.Rcp(CC.LT, Dest.Temp0, OpAC.PBR);
            asm.Mmsub(CC.LT, Dest.PBR, OpAC.Temp2, OpBD.Temp1, OpAC.Temp1, OpBD.Temp1);
            asm.Madd(CC.LT, Dest.Temp0, OpAC.PBR, OpBD.Temp0, OpAC.Temp1);
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcRGB, OpBD.SrcAAA);
            asm.Mmadd(CC.T, Dest.Temp1, OpAC.PBR, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.DstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenUncorrelatedHslLuminosity(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.3f, 0.59f, 0.11f);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.SrcRGB, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.SrcRGB, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.Temp2.BBB, OpAC.SrcRGB, OpBD.ConstantRGB, OpAC.PBR);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.PBR.BBB, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.Temp2, OpBD.PBR);
            asm.Add(CC.T, Dest.Temp1, OpBD.Temp1, OpBD.PBR);
            asm.Mov(CC.T, Dest.Temp0, OpBD.PBR);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp1);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.ConstantOne);
            asm.Add(CC.GT, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Sub(CC.GT, Dest.PBR, OpBD.PBR, OpBD.Temp2);
            asm.Rcp(CC.GT, Dest.PBR, OpAC.PBR);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.GT, Dest.PBR, OpBD.Temp1, OpBD.Temp2);
            asm.Madd(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp2);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp1);
            asm.Min(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.T, Dest.PBR.GBR.CC, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.LT, Dest.PBR, OpBD.Temp2, OpBD.PBR);
            asm.Rcp(CC.LT, Dest.Temp0, OpAC.PBR);
            asm.Mmsub(CC.LT, Dest.PBR, OpAC.Temp1, OpBD.Temp2, OpAC.Temp2, OpBD.Temp2);
            asm.Madd(CC.LT, Dest.Temp0, OpAC.PBR, OpBD.Temp0, OpAC.Temp2);
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcRGB, OpBD.SrcAAA);
            asm.Mmadd(CC.T, Dest.Temp1, OpAC.PBR, OpBD.OneMinusDstAAA, OpAC.DstRGB, OpBD.OneMinusSrcAAA);
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.DstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneMinusSrcAlphaGl);
        }

        private static FixedFunctionAlpha GenDisjointSrc(ref UcodeAssembler asm)
        {
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.Temp0, OpAC.SrcRGB, OpBD.DstAAA, OpAC.SrcRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.SrcRGB, OpBD.PBR, OpAC.Temp0);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.ZeroGl);
        }

        private static FixedFunctionAlpha GenDisjointSrcOver(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.SrcRGB);
            asm.Madd(CC.T, Dest.Temp0, OpAC.SrcRGB, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.SrcRGB, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointDstOver(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp1);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp1, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.SrcRGB, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointSrcIn(ref UcodeAssembler asm)
        {
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.Temp0, OpAC.SrcRGB, OpBD.DstAAA, OpAC.SrcRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Sub(CC.T, Dest.Temp1.RToA, OpBD.DstAAA, OpBD.PBR);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointSrcOut(ref UcodeAssembler asm)
        {
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Mul(CC.T, Dest.Temp0, OpAC.SrcRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointSrcAtop(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.Temp0, OpAC.SrcRGB, OpBD.DstAAA, OpAC.SrcRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp1, OpBD.PBR, OpAC.Temp0);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenDisjointDstAtop(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.Temp0, OpAC.Temp1, OpBD.DstAAA, OpAC.Temp1, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.SrcRGB, OpBD.PBR, OpAC.Temp0);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.ZeroGl);
        }

        private static FixedFunctionAlpha GenDisjointXor(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Mul(CC.T, Dest.Temp0, OpAC.SrcRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp1, OpBD.PBR, OpAC.Temp0);
            asm.Min(CC.T, Dest.Temp1, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Add(CC.T, Dest.Temp1.RToA, OpBD.Temp1, OpBD.PBR);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointPlus(ref UcodeAssembler asm)
        {
            asm.Mul(CC.T, Dest.PBR, OpAC.SrcRGB, OpBD.SrcAAA);
            asm.Add(CC.T, Dest.Temp0, OpBD.DstRGB, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenDisjointMultiply(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Mul(CC.T, Dest.Temp0, OpAC.SrcRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.SrcRGB, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointScreen(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcRGB, OpBD.PBR);
            asm.Mmsub(CC.T, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne, OpAC.SrcRGB, OpBD.Temp1);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.SrcRGB, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointOverlay(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.5f, 0.5f, 0.5f);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.PBR, OpBD.ConstantRGB);
            asm.Mmadd(CC.LE, Dest.Temp0, OpAC.SrcRGB, OpBD.Temp1, OpAC.SrcRGB, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.Temp0, OpBD.ConstantOne, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.PBR, OpBD.ConstantOne, OpBD.SrcRGB);
            asm.Mmadd(CC.GT, Dest.PBR, OpAC.Temp0, OpBD.PBR, OpAC.Temp0, OpBD.PBR);
            asm.Sub(CC.GT, Dest.Temp0, OpBD.ConstantOne, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.SrcRGB, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointDarken(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.Temp0, OpAC.SrcRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.SrcRGB, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointLighten(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Max(CC.T, Dest.Temp0, OpAC.SrcRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.SrcRGB, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointColorDodge(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.ConstantOne, OpBD.SrcRGB);
            asm.Rcp(CC.GT, Dest.PBR, OpAC.Temp0);
            asm.Mul(CC.GT, Dest.PBR, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.GT, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.LE, Dest.Temp0, OpBD.ConstantOne);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.Temp1, OpBD.ConstantZero);
            asm.Mov(CC.LE, Dest.Temp0, OpBD.ConstantZero);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.SrcRGB, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointColorBurn(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.SrcRGB, OpBD.ConstantZero);
            asm.Rcp(CC.GT, Dest.PBR, OpAC.SrcRGB);
            asm.Mmsub(CC.GT, Dest.PBR, OpAC.PBR, OpBD.ConstantOne, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.Temp0, OpBD.ConstantOne, OpBD.PBR);
            asm.Max(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.ConstantZero);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.ConstantOne, OpBD.Temp1);
            asm.Mov(CC.LE, Dest.Temp0, OpBD.ConstantOne);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.SrcRGB, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointHardLight(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.5f, 0.5f, 0.5f);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.SrcRGB, OpBD.ConstantRGB);
            asm.Mmadd(CC.LE, Dest.Temp0, OpAC.SrcRGB, OpBD.Temp1, OpAC.SrcRGB, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.Temp0, OpBD.ConstantOne, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.PBR, OpBD.ConstantOne, OpBD.SrcRGB);
            asm.Mmadd(CC.GT, Dest.PBR, OpAC.Temp0, OpBD.PBR, OpAC.Temp0, OpBD.PBR);
            asm.Sub(CC.GT, Dest.Temp0, OpBD.ConstantOne, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.SrcRGB, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointSoftLight(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(4, 0.25f, 0.25f, 0.25f);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.ConstantRGB);
            asm.SetConstant(0, 0.2605f, 0.2605f, 0.2605f);
            asm.Mul(CC.GT, Dest.PBR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.SetConstant(1, -0.7817f, -0.7817f, -0.7817f);
            asm.Mmadd(CC.GT, Dest.PBR, OpAC.Temp1, OpBD.PBR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.SetConstant(2, 0.3022f, 0.3022f, 0.3022f);
            asm.Mmadd(CC.GT, Dest.PBR, OpAC.Temp1, OpBD.PBR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.SetConstant(3, 0.2192f, 0.2192f, 0.2192f);
            asm.Add(CC.GT, Dest.Temp0, OpBD.PBR, OpBD.ConstantRGB);
            asm.SetConstant(5, 16f, 16f, 16f);
            asm.Mul(CC.LE, Dest.PBR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.SetConstant(6, 12f, 12f, 12f);
            asm.Mmsub(CC.LE, Dest.PBR, OpAC.Temp1, OpBD.PBR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.SetConstant(7, 3f, 3f, 3f);
            asm.Mmadd(CC.LE, Dest.Temp0, OpAC.Temp1, OpBD.PBR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcRGB, OpBD.SrcRGB);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.ConstantOne);
            asm.Mmsub(CC.LE, Dest.Temp0, OpAC.Temp1, OpBD.ConstantOne, OpAC.Temp1, OpBD.Temp1);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcRGB, OpBD.SrcRGB);
            asm.Sub(CC.T, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.SrcRGB, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointDifference(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.PBR, OpBD.SrcRGB);
            asm.Sub(CC.LT, Dest.Temp0, OpBD.SrcRGB, OpBD.Temp1);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.SrcRGB, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointExclusion(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcRGB, OpBD.PBR);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.ConstantOne, OpAC.SrcRGB, OpBD.Temp1);
            asm.Mmsub(CC.T, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne, OpAC.SrcRGB, OpBD.Temp1);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.SrcRGB, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointInvertRGB(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Mmsub(CC.T, Dest.Temp0, OpAC.SrcRGB, OpBD.ConstantOne, OpAC.SrcRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.Temp0, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp1, OpBD.PBR, OpAC.Temp0);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenDisjointLinearDodge(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.SrcRGB, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointLinearBurn(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Max(CC.T, Dest.Temp0, OpAC.PBR, OpBD.ConstantZero);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.SrcRGB, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointVividLight(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.5f, 0.5f, 0.5f);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcRGB, OpBD.ConstantRGB);
            asm.Sub(CC.GE, Dest.PBR, OpBD.ConstantOne, OpBD.SrcRGB);
            asm.Add(CC.GE, Dest.PBR, OpBD.PBR, OpBD.PBR);
            asm.Rcp(CC.GE, Dest.PBR, OpAC.PBR);
            asm.Mul(CC.GE, Dest.PBR, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.GE, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne);
            asm.Add(CC.LT, Dest.PBR, OpBD.SrcRGB, OpBD.SrcRGB);
            asm.Rcp(CC.LT, Dest.PBR, OpAC.PBR);
            asm.Mmsub(CC.LT, Dest.PBR, OpAC.PBR, OpBD.ConstantOne, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.LT, Dest.PBR, OpAC.PBR, OpBD.ConstantOne);
            asm.Sub(CC.LT, Dest.Temp0, OpBD.ConstantOne, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcRGB, OpBD.ConstantZero);
            asm.Mul(CC.LE, Dest.Temp0, OpAC.SrcAAA, OpBD.ConstantZero);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcRGB, OpBD.ConstantOne);
            asm.Mov(CC.GE, Dest.Temp0, OpBD.ConstantOne);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.SrcRGB, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointLinearLight(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 2f, 2f, 2f);
            asm.Madd(CC.T, Dest.PBR, OpAC.SrcRGB, OpBD.ConstantRGB, OpAC.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Max(CC.T, Dest.PBR, OpAC.PBR, OpBD.ConstantZero);
            asm.Min(CC.T, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.SrcRGB, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointPinLight(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcRGB, OpBD.SrcRGB);
            asm.Sub(CC.T, Dest.Temp0, OpBD.PBR, OpBD.ConstantOne);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.Temp1);
            asm.Max(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.ConstantZero);
            asm.Add(CC.LE, Dest.PBR, OpBD.SrcRGB, OpBD.SrcRGB);
            asm.Min(CC.LE, Dest.Temp0, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.SrcRGB, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointHardMix(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.PBR, OpBD.ConstantOne);
            asm.Mul(CC.LT, Dest.Temp0, OpAC.SrcAAA, OpBD.ConstantZero);
            asm.Mov(CC.GE, Dest.Temp0, OpBD.ConstantOne);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.SrcRGB, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointHslHue(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.SrcRGB);
            asm.Min(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.SrcRGB);
            asm.Min(CC.T, Dest.Temp0.GBR, OpAC.PBR, OpBD.SrcRGB);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.SrcRGB);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.SrcRGB);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.SrcRGB);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.PBR, OpBD.Temp0);
            asm.Rcp(CC.GT, Dest.Temp0, OpAC.Temp0);
            asm.Mov(CC.GT, Dest.PBR.GBR, OpBD.SrcRGB);
            asm.Min(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.SrcRGB);
            asm.Min(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.SrcRGB);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.SrcRGB, OpAC.Temp0, OpBD.PBR);
            asm.Mov(CC.GT, Dest.PBR.GBR, OpBD.Temp1);
            asm.Min(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.GT, Dest.Temp2.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Mov(CC.GT, Dest.PBR.GBR, OpBD.Temp1);
            asm.Max(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Max(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp0, OpBD.Temp2);
            asm.Mul(CC.LE, Dest.Temp0, OpAC.SrcAAA, OpBD.ConstantZero);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.3f, 0.59f, 0.11f);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.PBR, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.Temp1.BBB, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.Temp0, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp0, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.PBR.BBB, OpAC.Temp0, OpBD.ConstantRGB, OpAC.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.Temp1, OpBD.PBR);
            asm.Add(CC.T, Dest.Temp2, OpBD.Temp0, OpBD.PBR);
            asm.Mov(CC.T, Dest.Temp0, OpBD.PBR);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.ConstantOne);
            asm.Add(CC.GT, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Sub(CC.GT, Dest.PBR, OpBD.PBR, OpBD.Temp1);
            asm.Rcp(CC.GT, Dest.PBR, OpAC.PBR);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.PBR, OpBD.Temp2, OpBD.Temp1);
            asm.Madd(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR.CC, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.LT, Dest.PBR, OpBD.Temp1, OpBD.PBR);
            asm.Rcp(CC.LT, Dest.Temp0, OpAC.PBR);
            asm.Mmsub(CC.LT, Dest.PBR, OpAC.Temp2, OpBD.Temp1, OpAC.Temp1, OpBD.Temp1);
            asm.Madd(CC.LT, Dest.Temp0, OpAC.PBR, OpBD.Temp0, OpAC.Temp1);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.SrcRGB, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointHslSaturation(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.T, Dest.Temp0.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp1);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.PBR, OpBD.Temp0);
            asm.Rcp(CC.GT, Dest.Temp0, OpAC.Temp0);
            asm.Mov(CC.GT, Dest.PBR.GBR, OpBD.Temp1);
            asm.Min(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.Temp1, OpAC.Temp0, OpBD.PBR);
            asm.Mov(CC.GT, Dest.PBR.GBR, OpBD.SrcRGB);
            asm.Min(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.SrcRGB);
            asm.Min(CC.GT, Dest.Temp1.GBR, OpAC.PBR, OpBD.SrcRGB);
            asm.Mov(CC.GT, Dest.PBR.GBR, OpBD.SrcRGB);
            asm.Max(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.SrcRGB);
            asm.Max(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.SrcRGB);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp0, OpBD.Temp1);
            asm.Mul(CC.LE, Dest.Temp0, OpAC.SrcAAA, OpBD.ConstantZero);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.3f, 0.59f, 0.11f);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.PBR, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.Temp1.BBB, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.Temp0, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp0, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.PBR.BBB, OpAC.Temp0, OpBD.ConstantRGB, OpAC.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.Temp1, OpBD.PBR);
            asm.Add(CC.T, Dest.Temp2, OpBD.Temp0, OpBD.PBR);
            asm.Mov(CC.T, Dest.Temp0, OpBD.PBR);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.ConstantOne);
            asm.Add(CC.GT, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Sub(CC.GT, Dest.PBR, OpBD.PBR, OpBD.Temp1);
            asm.Rcp(CC.GT, Dest.PBR, OpAC.PBR);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.PBR, OpBD.Temp2, OpBD.Temp1);
            asm.Madd(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR.CC, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.LT, Dest.PBR, OpBD.Temp1, OpBD.PBR);
            asm.Rcp(CC.LT, Dest.Temp0, OpAC.PBR);
            asm.Mmsub(CC.LT, Dest.PBR, OpAC.Temp2, OpBD.Temp1, OpAC.Temp1, OpBD.Temp1);
            asm.Madd(CC.LT, Dest.Temp0, OpAC.PBR, OpBD.Temp0, OpAC.Temp1);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.SrcRGB, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointHslColor(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.3f, 0.59f, 0.11f);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.PBR, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.Temp1.BBB, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.SrcRGB, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.SrcRGB, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.PBR.BBB, OpAC.SrcRGB, OpBD.ConstantRGB, OpAC.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.Temp1, OpBD.PBR);
            asm.Add(CC.T, Dest.Temp2, OpBD.SrcRGB, OpBD.PBR);
            asm.Mov(CC.T, Dest.Temp0, OpBD.PBR);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.ConstantOne);
            asm.Add(CC.GT, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Sub(CC.GT, Dest.PBR, OpBD.PBR, OpBD.Temp1);
            asm.Rcp(CC.GT, Dest.PBR, OpAC.PBR);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.PBR, OpBD.Temp2, OpBD.Temp1);
            asm.Madd(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR.CC, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.LT, Dest.PBR, OpBD.Temp1, OpBD.PBR);
            asm.Rcp(CC.LT, Dest.Temp0, OpAC.PBR);
            asm.Mmsub(CC.LT, Dest.PBR, OpAC.Temp2, OpBD.Temp1, OpAC.Temp1, OpBD.Temp1);
            asm.Madd(CC.LT, Dest.Temp0, OpAC.PBR, OpBD.Temp0, OpAC.Temp1);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.SrcRGB, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenDisjointHslLuminosity(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.3f, 0.59f, 0.11f);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.SrcRGB, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.SrcRGB, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.Temp2.BBB, OpAC.SrcRGB, OpBD.ConstantRGB, OpAC.PBR);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.PBR.BBB, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.Temp2, OpBD.PBR);
            asm.Add(CC.T, Dest.Temp1, OpBD.Temp1, OpBD.PBR);
            asm.Mov(CC.T, Dest.Temp0, OpBD.PBR);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp1);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.ConstantOne);
            asm.Add(CC.GT, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Sub(CC.GT, Dest.PBR, OpBD.PBR, OpBD.Temp2);
            asm.Rcp(CC.GT, Dest.PBR, OpAC.PBR);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.GT, Dest.PBR, OpBD.Temp1, OpBD.Temp2);
            asm.Madd(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp2);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp1);
            asm.Min(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.T, Dest.PBR.GBR.CC, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.LT, Dest.PBR, OpBD.Temp2, OpBD.PBR);
            asm.Rcp(CC.LT, Dest.Temp0, OpAC.PBR);
            asm.Mmsub(CC.LT, Dest.PBR, OpAC.Temp1, OpBD.Temp2, OpAC.Temp2, OpBD.Temp2);
            asm.Madd(CC.LT, Dest.Temp0, OpAC.PBR, OpBD.Temp0, OpAC.Temp2);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.OneMinusSrcAAA);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.Temp1, OpAC.PBR, OpBD.Temp0);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.SrcAAA, OpBD.OneMinusDstAAA);
            asm.Madd(CC.T, Dest.Temp0, OpAC.PBR, OpBD.SrcRGB, OpAC.Temp0);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Min(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenConjointSrc(ref UcodeAssembler asm)
        {
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.SrcAAA);
            asm.Mul(CC.T, Dest.Temp0, OpAC.SrcRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Max(CC.T, Dest.PBR, OpAC.PBR, OpBD.ConstantZero);
            asm.Madd(CC.T, Dest.Temp0, OpAC.SrcRGB, OpBD.PBR, OpAC.Temp0);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.ZeroGl);
        }

        private static FixedFunctionAlpha GenConjointSrcOver(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Mov(CC.T, Dest.Temp0, OpBD.SrcRGB);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.SrcRGB, OpBD.DstAAA, OpAC.SrcRGB, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointDstOver(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Mov(CC.T, Dest.Temp0, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp1, OpBD.DstAAA, OpAC.SrcRGB, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointSrcIn(ref UcodeAssembler asm)
        {
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.SrcAAA);
            asm.Mul(CC.T, Dest.Temp0, OpAC.SrcRGB, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MinimumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointSrcOut(ref UcodeAssembler asm)
        {
            asm.Sub(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Max(CC.T, Dest.PBR, OpAC.PBR, OpBD.ConstantZero);
            asm.Mul(CC.T, Dest.Temp0, OpAC.SrcRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Max(CC.T, Dest.Temp1.RToA, OpAC.PBR, OpBD.ConstantZero);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenConjointSrcAtop(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.SrcAAA);
            asm.Mul(CC.T, Dest.Temp0, OpAC.SrcRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Max(CC.T, Dest.PBR, OpAC.PBR, OpBD.ConstantZero);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp1, OpBD.PBR, OpAC.Temp0);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointDstAtop(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.SrcAAA);
            asm.Mul(CC.T, Dest.Temp0, OpAC.Temp1, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Max(CC.T, Dest.PBR, OpAC.PBR, OpBD.ConstantZero);
            asm.Madd(CC.T, Dest.Temp0, OpAC.SrcRGB, OpBD.PBR, OpAC.Temp0);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.OneGl, BlendFactor.ZeroGl);
        }

        private static FixedFunctionAlpha GenConjointXor(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Max(CC.T, Dest.PBR, OpAC.PBR, OpBD.ConstantZero);
            asm.Mul(CC.T, Dest.Temp0, OpAC.SrcRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Max(CC.T, Dest.PBR, OpAC.PBR, OpBD.ConstantZero);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp1, OpBD.PBR, OpAC.Temp0);
            asm.Sub(CC.T, Dest.Temp1.CC, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Sub(CC.LT, Dest.Temp1, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mov(CC.T, Dest.Temp1.RToA, OpBD.Temp1);
            asm.Mov(CC.T, Dest.Temp0, OpBD.Temp0);
            return FixedFunctionAlpha.Disabled;
        }

        private static FixedFunctionAlpha GenConjointMultiply(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Mul(CC.T, Dest.Temp0, OpAC.SrcRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.SrcRGB, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointScreen(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcRGB, OpBD.PBR);
            asm.Mmsub(CC.T, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne, OpAC.SrcRGB, OpBD.Temp1);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.SrcRGB, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointOverlay(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.5f, 0.5f, 0.5f);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.PBR, OpBD.ConstantRGB);
            asm.Mmadd(CC.LE, Dest.Temp0, OpAC.SrcRGB, OpBD.Temp1, OpAC.SrcRGB, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.Temp0, OpBD.ConstantOne, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.PBR, OpBD.ConstantOne, OpBD.SrcRGB);
            asm.Mmadd(CC.GT, Dest.PBR, OpAC.Temp0, OpBD.PBR, OpAC.Temp0, OpBD.PBR);
            asm.Sub(CC.GT, Dest.Temp0, OpBD.ConstantOne, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.SrcRGB, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointDarken(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.Temp0, OpAC.SrcRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.SrcRGB, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointLighten(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Max(CC.T, Dest.Temp0, OpAC.SrcRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.SrcRGB, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointColorDodge(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.ConstantOne, OpBD.SrcRGB);
            asm.Rcp(CC.GT, Dest.PBR, OpAC.Temp0);
            asm.Mul(CC.GT, Dest.PBR, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.GT, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne);
            asm.Mov(CC.LE, Dest.Temp0, OpBD.ConstantOne);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.Temp1, OpBD.ConstantZero);
            asm.Mov(CC.LE, Dest.Temp0, OpBD.ConstantZero);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.SrcRGB, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointColorBurn(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.SrcRGB, OpBD.ConstantZero);
            asm.Rcp(CC.GT, Dest.PBR, OpAC.SrcRGB);
            asm.Mmsub(CC.GT, Dest.PBR, OpAC.PBR, OpBD.ConstantOne, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.Temp0, OpBD.ConstantOne, OpBD.PBR);
            asm.Max(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.ConstantZero);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.ConstantOne, OpBD.Temp1);
            asm.Mov(CC.LE, Dest.Temp0, OpBD.ConstantOne);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.SrcRGB, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointHardLight(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.5f, 0.5f, 0.5f);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.SrcRGB, OpBD.ConstantRGB);
            asm.Mmadd(CC.LE, Dest.Temp0, OpAC.SrcRGB, OpBD.Temp1, OpAC.SrcRGB, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.Temp0, OpBD.ConstantOne, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.PBR, OpBD.ConstantOne, OpBD.SrcRGB);
            asm.Mmadd(CC.GT, Dest.PBR, OpAC.Temp0, OpBD.PBR, OpAC.Temp0, OpBD.PBR);
            asm.Sub(CC.GT, Dest.Temp0, OpBD.ConstantOne, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.SrcRGB, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointSoftLight(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(4, 0.25f, 0.25f, 0.25f);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.ConstantRGB);
            asm.SetConstant(0, 0.2605f, 0.2605f, 0.2605f);
            asm.Mul(CC.GT, Dest.PBR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.SetConstant(1, -0.7817f, -0.7817f, -0.7817f);
            asm.Mmadd(CC.GT, Dest.PBR, OpAC.Temp1, OpBD.PBR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.SetConstant(2, 0.3022f, 0.3022f, 0.3022f);
            asm.Mmadd(CC.GT, Dest.PBR, OpAC.Temp1, OpBD.PBR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.SetConstant(3, 0.2192f, 0.2192f, 0.2192f);
            asm.Add(CC.GT, Dest.Temp0, OpBD.PBR, OpBD.ConstantRGB);
            asm.SetConstant(5, 16f, 16f, 16f);
            asm.Mul(CC.LE, Dest.PBR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.SetConstant(6, 12f, 12f, 12f);
            asm.Mmsub(CC.LE, Dest.PBR, OpAC.Temp1, OpBD.PBR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.SetConstant(7, 3f, 3f, 3f);
            asm.Mmadd(CC.LE, Dest.Temp0, OpAC.Temp1, OpBD.PBR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcRGB, OpBD.SrcRGB);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.ConstantOne);
            asm.Mmsub(CC.LE, Dest.Temp0, OpAC.Temp1, OpBD.ConstantOne, OpAC.Temp1, OpBD.Temp1);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcRGB, OpBD.SrcRGB);
            asm.Sub(CC.T, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.SrcRGB, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointDifference(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.PBR, OpBD.SrcRGB);
            asm.Sub(CC.LT, Dest.Temp0, OpBD.SrcRGB, OpBD.Temp1);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.SrcRGB, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointExclusion(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcRGB, OpBD.PBR);
            asm.Mmsub(CC.T, Dest.PBR, OpAC.PBR, OpBD.ConstantOne, OpAC.SrcRGB, OpBD.Temp1);
            asm.Mmsub(CC.T, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne, OpAC.SrcRGB, OpBD.Temp1);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.SrcRGB, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointInvertRGB(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Mmsub(CC.T, Dest.Temp0, OpAC.SrcRGB, OpBD.ConstantOne, OpAC.SrcRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR, OpAC.DstAAA, OpBD.SrcAAA);
            asm.Mul(CC.T, Dest.Temp0, OpAC.Temp0, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Max(CC.T, Dest.PBR, OpAC.PBR, OpBD.ConstantZero);
            asm.Madd(CC.T, Dest.Temp0, OpAC.Temp1, OpBD.PBR, OpAC.Temp0);
            return new FixedFunctionAlpha(BlendOp.AddGl, BlendFactor.ZeroGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointLinearDodge(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcRGB, OpBD.PBR);
            asm.Min(CC.T, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.SrcRGB, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointLinearBurn(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Max(CC.T, Dest.Temp0, OpAC.PBR, OpBD.ConstantZero);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.SrcRGB, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointVividLight(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.5f, 0.5f, 0.5f);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcRGB, OpBD.ConstantRGB);
            asm.Sub(CC.GE, Dest.PBR, OpBD.ConstantOne, OpBD.SrcRGB);
            asm.Add(CC.GE, Dest.PBR, OpBD.PBR, OpBD.PBR);
            asm.Rcp(CC.GE, Dest.PBR, OpAC.PBR);
            asm.Mul(CC.GE, Dest.PBR, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.GE, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne);
            asm.Add(CC.LT, Dest.PBR, OpBD.SrcRGB, OpBD.SrcRGB);
            asm.Rcp(CC.LT, Dest.PBR, OpAC.PBR);
            asm.Mmsub(CC.LT, Dest.PBR, OpAC.PBR, OpBD.ConstantOne, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.LT, Dest.PBR, OpAC.PBR, OpBD.ConstantOne);
            asm.Sub(CC.LT, Dest.Temp0, OpBD.ConstantOne, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcRGB, OpBD.ConstantZero);
            asm.Mul(CC.LE, Dest.Temp0, OpAC.SrcAAA, OpBD.ConstantZero);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcRGB, OpBD.ConstantOne);
            asm.Mov(CC.GE, Dest.Temp0, OpBD.ConstantOne);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.SrcRGB, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointLinearLight(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 2f, 2f, 2f);
            asm.Madd(CC.T, Dest.PBR, OpAC.SrcRGB, OpBD.ConstantRGB, OpAC.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Max(CC.T, Dest.PBR, OpAC.PBR, OpBD.ConstantZero);
            asm.Min(CC.T, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.SrcRGB, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointPinLight(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcRGB, OpBD.SrcRGB);
            asm.Sub(CC.T, Dest.Temp0, OpBD.PBR, OpBD.ConstantOne);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.Temp1);
            asm.Max(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.ConstantZero);
            asm.Add(CC.LE, Dest.PBR, OpBD.SrcRGB, OpBD.SrcRGB);
            asm.Min(CC.LE, Dest.Temp0, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.SrcRGB, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointHardMix(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Add(CC.T, Dest.PBR, OpBD.SrcRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.PBR, OpBD.ConstantOne);
            asm.Mul(CC.LT, Dest.Temp0, OpAC.SrcAAA, OpBD.ConstantZero);
            asm.Mov(CC.GE, Dest.Temp0, OpBD.ConstantOne);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.SrcRGB, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointHslHue(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.SrcRGB);
            asm.Min(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.SrcRGB);
            asm.Min(CC.T, Dest.Temp0.GBR, OpAC.PBR, OpBD.SrcRGB);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.SrcRGB);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.SrcRGB);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.SrcRGB);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.PBR, OpBD.Temp0);
            asm.Rcp(CC.GT, Dest.Temp0, OpAC.Temp0);
            asm.Mov(CC.GT, Dest.PBR.GBR, OpBD.SrcRGB);
            asm.Min(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.SrcRGB);
            asm.Min(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.SrcRGB);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.SrcRGB, OpAC.Temp0, OpBD.PBR);
            asm.Mov(CC.GT, Dest.PBR.GBR, OpBD.Temp1);
            asm.Min(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.GT, Dest.Temp2.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Mov(CC.GT, Dest.PBR.GBR, OpBD.Temp1);
            asm.Max(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Max(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp0, OpBD.Temp2);
            asm.Mul(CC.LE, Dest.Temp0, OpAC.SrcAAA, OpBD.ConstantZero);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.3f, 0.59f, 0.11f);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.PBR, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.Temp1.BBB, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.Temp0, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp0, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.PBR.BBB, OpAC.Temp0, OpBD.ConstantRGB, OpAC.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.Temp1, OpBD.PBR);
            asm.Add(CC.T, Dest.Temp2, OpBD.Temp0, OpBD.PBR);
            asm.Mov(CC.T, Dest.Temp0, OpBD.PBR);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.ConstantOne);
            asm.Add(CC.GT, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Sub(CC.GT, Dest.PBR, OpBD.PBR, OpBD.Temp1);
            asm.Rcp(CC.GT, Dest.PBR, OpAC.PBR);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.PBR, OpBD.Temp2, OpBD.Temp1);
            asm.Madd(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR.CC, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.LT, Dest.PBR, OpBD.Temp1, OpBD.PBR);
            asm.Rcp(CC.LT, Dest.Temp0, OpAC.PBR);
            asm.Mmsub(CC.LT, Dest.PBR, OpAC.Temp2, OpBD.Temp1, OpAC.Temp1, OpBD.Temp1);
            asm.Madd(CC.LT, Dest.Temp0, OpAC.PBR, OpBD.Temp0, OpAC.Temp1);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.SrcRGB, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointHslSaturation(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.PBR);
            asm.Min(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.T, Dest.Temp0.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp1);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.T, Dest.Temp0.CC, OpBD.PBR, OpBD.Temp0);
            asm.Rcp(CC.GT, Dest.Temp0, OpAC.Temp0);
            asm.Mov(CC.GT, Dest.PBR.GBR, OpBD.Temp1);
            asm.Min(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.Temp1, OpAC.Temp0, OpBD.PBR);
            asm.Mov(CC.GT, Dest.PBR.GBR, OpBD.SrcRGB);
            asm.Min(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.SrcRGB);
            asm.Min(CC.GT, Dest.Temp1.GBR, OpAC.PBR, OpBD.SrcRGB);
            asm.Mov(CC.GT, Dest.PBR.GBR, OpBD.SrcRGB);
            asm.Max(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.SrcRGB);
            asm.Max(CC.GT, Dest.PBR.GBR, OpAC.PBR, OpBD.SrcRGB);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp0, OpBD.Temp1);
            asm.Mul(CC.LE, Dest.Temp0, OpAC.SrcAAA, OpBD.ConstantZero);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.3f, 0.59f, 0.11f);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.PBR, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.Temp1.BBB, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.Temp0, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp0, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.PBR.BBB, OpAC.Temp0, OpBD.ConstantRGB, OpAC.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.Temp1, OpBD.PBR);
            asm.Add(CC.T, Dest.Temp2, OpBD.Temp0, OpBD.PBR);
            asm.Mov(CC.T, Dest.Temp0, OpBD.PBR);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.ConstantOne);
            asm.Add(CC.GT, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Sub(CC.GT, Dest.PBR, OpBD.PBR, OpBD.Temp1);
            asm.Rcp(CC.GT, Dest.PBR, OpAC.PBR);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.PBR, OpBD.Temp2, OpBD.Temp1);
            asm.Madd(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR.CC, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.LT, Dest.PBR, OpBD.Temp1, OpBD.PBR);
            asm.Rcp(CC.LT, Dest.Temp0, OpAC.PBR);
            asm.Mmsub(CC.LT, Dest.PBR, OpAC.Temp2, OpBD.Temp1, OpAC.Temp1, OpBD.Temp1);
            asm.Madd(CC.LT, Dest.Temp0, OpAC.PBR, OpBD.Temp0, OpAC.Temp1);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.SrcRGB, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointHslColor(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.3f, 0.59f, 0.11f);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.PBR, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.Temp1.BBB, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.SrcRGB, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.SrcRGB, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.PBR.BBB, OpAC.SrcRGB, OpBD.ConstantRGB, OpAC.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.Temp1, OpBD.PBR);
            asm.Add(CC.T, Dest.Temp2, OpBD.SrcRGB, OpBD.PBR);
            asm.Mov(CC.T, Dest.Temp0, OpBD.PBR);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.ConstantOne);
            asm.Add(CC.GT, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Sub(CC.GT, Dest.PBR, OpBD.PBR, OpBD.Temp1);
            asm.Rcp(CC.GT, Dest.PBR, OpAC.PBR);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.GT, Dest.PBR, OpBD.Temp2, OpBD.Temp1);
            asm.Madd(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp1);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp2);
            asm.Min(CC.T, Dest.PBR.GBR.CC, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.LT, Dest.PBR, OpBD.Temp1, OpBD.PBR);
            asm.Rcp(CC.LT, Dest.Temp0, OpAC.PBR);
            asm.Mmsub(CC.LT, Dest.PBR, OpAC.Temp2, OpBD.Temp1, OpAC.Temp1, OpBD.Temp1);
            asm.Madd(CC.LT, Dest.Temp0, OpAC.PBR, OpBD.Temp0, OpAC.Temp1);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.SrcRGB, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }

        private static FixedFunctionAlpha GenConjointHslLuminosity(ref UcodeAssembler asm)
        {
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.SetConstant(0, 0.3f, 0.59f, 0.11f);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.SrcRGB, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.SrcRGB, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.Temp2.BBB, OpAC.SrcRGB, OpBD.ConstantRGB, OpAC.PBR);
            asm.Mul(CC.T, Dest.PBR.RRR, OpAC.Temp1, OpBD.ConstantRGB);
            asm.Madd(CC.T, Dest.PBR.GGG, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Madd(CC.T, Dest.PBR.BBB, OpAC.Temp1, OpBD.ConstantRGB, OpAC.PBR);
            asm.Sub(CC.T, Dest.PBR, OpBD.Temp2, OpBD.PBR);
            asm.Add(CC.T, Dest.Temp1, OpBD.Temp1, OpBD.PBR);
            asm.Mov(CC.T, Dest.Temp0, OpBD.PBR);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp1);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Max(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.PBR, OpBD.ConstantOne);
            asm.Add(CC.GT, Dest.PBR, OpBD.PBR, OpBD.ConstantOne);
            asm.Sub(CC.GT, Dest.PBR, OpBD.PBR, OpBD.Temp2);
            asm.Rcp(CC.GT, Dest.PBR, OpAC.PBR);
            asm.Mmsub(CC.GT, Dest.Temp0, OpAC.PBR, OpBD.ConstantOne, OpAC.PBR, OpBD.Temp2);
            asm.Sub(CC.GT, Dest.PBR, OpBD.Temp1, OpBD.Temp2);
            asm.Madd(CC.GT, Dest.Temp0, OpAC.Temp0, OpBD.PBR, OpAC.Temp2);
            asm.Mov(CC.T, Dest.PBR.GBR, OpBD.Temp1);
            asm.Min(CC.T, Dest.PBR.GBR, OpAC.PBR, OpBD.Temp1);
            asm.Min(CC.T, Dest.PBR.GBR.CC, OpAC.PBR, OpBD.Temp1);
            asm.Sub(CC.LT, Dest.PBR, OpBD.Temp2, OpBD.PBR);
            asm.Rcp(CC.LT, Dest.Temp0, OpAC.PBR);
            asm.Mmsub(CC.LT, Dest.PBR, OpAC.Temp1, OpBD.Temp2, OpAC.Temp2, OpBD.Temp2);
            asm.Madd(CC.LT, Dest.Temp0, OpAC.PBR, OpBD.Temp0, OpAC.Temp2);
            asm.Rcp(CC.T, Dest.PBR, OpAC.DstAAA);
            asm.Mul(CC.T, Dest.Temp1, OpAC.DstRGB, OpBD.PBR);
            asm.Sub(CC.T, Dest.PBR.CC, OpBD.SrcAAA, OpBD.DstAAA);
            asm.Mmadd(CC.GE, Dest.Temp0, OpAC.Temp0, OpBD.DstAAA, OpAC.SrcRGB, OpBD.PBR);
            asm.Sub(CC.LT, Dest.PBR, OpBD.DstAAA, OpBD.SrcAAA);
            asm.Mmadd(CC.LT, Dest.Temp0, OpAC.Temp0, OpBD.SrcAAA, OpAC.Temp1, OpBD.PBR);
            return new FixedFunctionAlpha(BlendOp.MaximumGl, BlendFactor.OneGl, BlendFactor.OneGl);
        }
    }
}

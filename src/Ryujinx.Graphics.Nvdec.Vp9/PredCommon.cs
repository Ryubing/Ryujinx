using Ryujinx.Graphics.Nvdec.Vp9.Types;
using System.Diagnostics;

namespace Ryujinx.Graphics.Nvdec.Vp9
{
    internal static class PredCommon
    {
        public static int GetReferenceModeContext(ref Vp9Common cm, ref MacroBlockD xd)
        {
            int ctx;
            // Note:
            // The mode info data structure has a one element border above and to the
            // left of the entries corresponding to real macroblocks.
            // The prediction flags in these dummy entries are initialized to 0.
            if (!xd.AboveMi.IsNull && !xd.LeftMi.IsNull)
            {
                // both edges available
                if (!xd.AboveMi.Value.HasSecondRef() && !xd.LeftMi.Value.HasSecondRef())
                {
                    // Neither edge uses comp pred (0/1)
                    ctx = (xd.AboveMi.Value.RefFrame[0] == cm.CompFixedRef ? 1 : 0) ^
                          (xd.LeftMi.Value.RefFrame[0] == cm.CompFixedRef ? 1 : 0);
                }
                else if (!xd.AboveMi.Value.HasSecondRef())
                {
                    // One of two edges uses comp pred (2/3)
                    ctx = 2 + (xd.AboveMi.Value.RefFrame[0] == cm.CompFixedRef || !xd.AboveMi.Value.IsInterBlock()
                        ? 1
                        : 0);
                }
                else if (!xd.LeftMi.Value.HasSecondRef())
                {
                    // One of two edges uses comp pred (2/3)
                    ctx = 2 +
                          (xd.LeftMi.Value.RefFrame[0] == cm.CompFixedRef || !xd.LeftMi.Value.IsInterBlock() ? 1 : 0);
                }
                else // Both edges use comp pred (4)
                {
                    ctx = 4;
                }
            }
            else if (!xd.AboveMi.IsNull || !xd.LeftMi.IsNull)
            {
                // One edge available
                ref ModeInfo edgeMi = ref !xd.AboveMi.IsNull ? ref xd.AboveMi.Value : ref xd.LeftMi.Value;

                if (!edgeMi.HasSecondRef())
                {
                    // Edge does not use comp pred (0/1)
                    ctx = edgeMi.RefFrame[0] == cm.CompFixedRef ? 1 : 0;
                }
                else
                {
                    // Edge uses comp pred (3)
                    ctx = 3;
                }
            }
            else
            {
                // No edges available (1)
                ctx = 1;
            }

            Debug.Assert(ctx >= 0 && ctx < Constants.CompInterContexts);
            return ctx;
        }

        // Returns a context number for the given MB prediction signal
        public static int GetPredContextCompRefP(ref Vp9Common cm, ref MacroBlockD xd)
        {
            int predContext;
            // Note:
            // The mode info data structure has a one element border above and to the
            // left of the entries corresponding to real macroblocks.
            // The prediction flags in these dummy entries are initialized to 0.
            int fixRefIdx = cm.RefFrameSignBias[cm.CompFixedRef];
            int varRefIdx = fixRefIdx == 0 ? 1 : 0;

            if (!xd.AboveMi.IsNull && !xd.LeftMi.IsNull)
            {
                // Both edges available
                bool aboveIntra = !xd.AboveMi.Value.IsInterBlock();
                bool leftIntra = !xd.LeftMi.Value.IsInterBlock();

                if (aboveIntra && leftIntra)
                {
                    // Intra/Intra (2)
                    predContext = 2;
                }
                else if (aboveIntra || leftIntra)
                {
                    // Intra/Inter
                    ref ModeInfo edgeMi = ref aboveIntra ? ref xd.LeftMi.Value : ref xd.AboveMi.Value;

                    if (!edgeMi.HasSecondRef()) // single pred (1/3)
                    {
                        predContext = 1 + (2 * (edgeMi.RefFrame[0] != cm.CompVarRef[1] ? 1 : 0));
                    }
                    else // Comp pred (1/3)
                    {
                        predContext = 1 + (2 * (edgeMi.RefFrame[varRefIdx] != cm.CompVarRef[1] ? 1 : 0));
                    }
                }
                else
                {
                    // Inter/Inter
                    bool lSg = !xd.LeftMi.Value.HasSecondRef();
                    bool aSg = !xd.AboveMi.Value.HasSecondRef();
                    sbyte vrfa = aSg ? xd.AboveMi.Value.RefFrame[0] : xd.AboveMi.Value.RefFrame[varRefIdx];
                    sbyte vrfl = lSg ? xd.LeftMi.Value.RefFrame[0] : xd.LeftMi.Value.RefFrame[varRefIdx];

                    if (vrfa == vrfl && cm.CompVarRef[1] == vrfa)
                    {
                        predContext = 0;
                    }
                    else if (lSg && aSg)
                    {
                        // Single/Single
                        if ((vrfa == cm.CompFixedRef && vrfl == cm.CompVarRef[0]) ||
                            (vrfl == cm.CompFixedRef && vrfa == cm.CompVarRef[0]))
                        {
                            predContext = 4;
                        }
                        else if (vrfa == vrfl)
                        {
                            predContext = 3;
                        }
                        else
                        {
                            predContext = 1;
                        }
                    }
                    else if (lSg || aSg)
                    {
                        // Single/Comp
                        sbyte vrfc = lSg ? vrfa : vrfl;
                        sbyte rfs = aSg ? vrfa : vrfl;
                        if (vrfc == cm.CompVarRef[1] && rfs != cm.CompVarRef[1])
                        {
                            predContext = 1;
                        }
                        else if (rfs == cm.CompVarRef[1] && vrfc != cm.CompVarRef[1])
                        {
                            predContext = 2;
                        }
                        else
                        {
                            predContext = 4;
                        }
                    }
                    else if (vrfa == vrfl)
                    {
                        // Comp/Comp
                        predContext = 4;
                    }
                    else
                    {
                        predContext = 2;
                    }
                }
            }
            else if (!xd.AboveMi.IsNull || !xd.LeftMi.IsNull)
            {
                // One edge available
                ref ModeInfo edgeMi = ref !xd.AboveMi.IsNull ? ref xd.AboveMi.Value : ref xd.LeftMi.Value;

                if (!edgeMi.IsInterBlock())
                {
                    predContext = 2;
                }
                else
                {
                    if (edgeMi.HasSecondRef())
                    {
                        predContext = 4 * (edgeMi.RefFrame[varRefIdx] != cm.CompVarRef[1] ? 1 : 0);
                    }
                    else
                    {
                        predContext = 3 * (edgeMi.RefFrame[0] != cm.CompVarRef[1] ? 1 : 0);
                    }
                }
            }
            else
            {
                // No edges available (2)
                predContext = 2;
            }

            Debug.Assert(predContext >= 0 && predContext < Constants.RefContexts);
            return predContext;
        }

        public static int GetPredContextSingleRefP1(ref MacroBlockD xd)
        {
            int predContext;
            // Note:
            // The mode info data structure has a one element border above and to the
            // left of the entries corresponding to real macroblocks.
            // The prediction flags in these dummy entries are initialized to 0.
            if (!xd.AboveMi.IsNull && !xd.LeftMi.IsNull)
            {
                // Both edges available
                bool aboveIntra = !xd.AboveMi.Value.IsInterBlock();
                bool leftIntra = !xd.LeftMi.Value.IsInterBlock();

                if (aboveIntra && leftIntra)
                {
                    // Intra/Intra
                    predContext = 2;
                }
                else if (aboveIntra || leftIntra)
                {
                    // Intra/Inter or Inter/Intra
                    ref ModeInfo edgeMi = ref aboveIntra ? ref xd.LeftMi.Value : ref xd.AboveMi.Value;
                    if (!edgeMi.HasSecondRef())
                    {
                        predContext = 4 * (edgeMi.RefFrame[0] == Constants.LastFrame ? 1 : 0);
                    }
                    else
                    {
                        predContext = 1 + (edgeMi.RefFrame[0] == Constants.LastFrame ||
                                           edgeMi.RefFrame[1] == Constants.LastFrame
                            ? 1
                            : 0);
                    }
                }
                else
                {
                    // Inter/Inter
                    bool aboveHasSecond = xd.AboveMi.Value.HasSecondRef();
                    bool leftHasSecond = xd.LeftMi.Value.HasSecondRef();
                    sbyte above0 = xd.AboveMi.Value.RefFrame[0];
                    sbyte above1 = xd.AboveMi.Value.RefFrame[1];
                    sbyte left0 = xd.LeftMi.Value.RefFrame[0];
                    sbyte left1 = xd.LeftMi.Value.RefFrame[1];

                    if (aboveHasSecond && leftHasSecond)
                    {
                        predContext = 1 + (above0 == Constants.LastFrame || above1 == Constants.LastFrame ||
                                           left0 == Constants.LastFrame || left1 == Constants.LastFrame
                            ? 1
                            : 0);
                    }
                    else if (aboveHasSecond || leftHasSecond)
                    {
                        sbyte rfs = !aboveHasSecond ? above0 : left0;
                        sbyte crf1 = aboveHasSecond ? above0 : left0;
                        sbyte crf2 = aboveHasSecond ? above1 : left1;

                        if (rfs == Constants.LastFrame)
                        {
                            predContext = 3 + (crf1 == Constants.LastFrame || crf2 == Constants.LastFrame ? 1 : 0);
                        }
                        else
                        {
                            predContext = crf1 == Constants.LastFrame || crf2 == Constants.LastFrame ? 1 : 0;
                        }
                    }
                    else
                    {
                        predContext = (2 * (above0 == Constants.LastFrame ? 1 : 0)) +
                                      (2 * (left0 == Constants.LastFrame ? 1 : 0));
                    }
                }
            }
            else if (!xd.AboveMi.IsNull || !xd.LeftMi.IsNull)
            {
                // One edge available
                ref ModeInfo edgeMi = ref !xd.AboveMi.IsNull ? ref xd.AboveMi.Value : ref xd.LeftMi.Value;
                if (!edgeMi.IsInterBlock())
                {
                    // Intra
                    predContext = 2;
                }
                else
                {
                    // Inter
                    if (!edgeMi.HasSecondRef())
                    {
                        predContext = 4 * (edgeMi.RefFrame[0] == Constants.LastFrame ? 1 : 0);
                    }
                    else
                    {
                        predContext = 1 + (edgeMi.RefFrame[0] == Constants.LastFrame ||
                                           edgeMi.RefFrame[1] == Constants.LastFrame
                            ? 1
                            : 0);
                    }
                }
            }
            else
            {
                // No edges available
                predContext = 2;
            }

            Debug.Assert(predContext >= 0 && predContext < Constants.RefContexts);
            return predContext;
        }

        public static int GetPredContextSingleRefP2(ref MacroBlockD xd)
        {
            int predContext;

            // Note:
            // The mode info data structure has a one element border above and to the
            // left of the entries corresponding to real macroblocks.
            // The prediction flags in these dummy entries are initialized to 0.
            if (!xd.AboveMi.IsNull && !xd.LeftMi.IsNull)
            {
                // Both edges available
                bool aboveIntra = !xd.AboveMi.Value.IsInterBlock();
                bool leftIntra = !xd.LeftMi.Value.IsInterBlock();

                if (aboveIntra && leftIntra)
                {
                    // Intra/Intra
                    predContext = 2;
                }
                else if (aboveIntra || leftIntra)
                {
                    // Intra/Inter or Inter/Intra
                    ref ModeInfo edgeMi = ref aboveIntra ? ref xd.LeftMi.Value : ref xd.AboveMi.Value;
                    if (!edgeMi.HasSecondRef())
                    {
                        if (edgeMi.RefFrame[0] == Constants.LastFrame)
                        {
                            predContext = 3;
                        }
                        else
                        {
                            predContext = 4 * (edgeMi.RefFrame[0] == Constants.GoldenFrame ? 1 : 0);
                        }
                    }
                    else
                    {
                        predContext = 1 + (2 * (edgeMi.RefFrame[0] == Constants.GoldenFrame ||
                                                edgeMi.RefFrame[1] == Constants.GoldenFrame
                            ? 1
                            : 0));
                    }
                }
                else
                {
                    // Inter/Inter
                    bool aboveHasSecond = xd.AboveMi.Value.HasSecondRef();
                    bool leftHasSecond = xd.LeftMi.Value.HasSecondRef();
                    sbyte above0 = xd.AboveMi.Value.RefFrame[0];
                    sbyte above1 = xd.AboveMi.Value.RefFrame[1];
                    sbyte left0 = xd.LeftMi.Value.RefFrame[0];
                    sbyte left1 = xd.LeftMi.Value.RefFrame[1];

                    if (aboveHasSecond && leftHasSecond)
                    {
                        if (above0 == left0 && above1 == left1)
                        {
                            predContext = 3 * (above0 == Constants.GoldenFrame || above1 == Constants.GoldenFrame ||
                                               left0 == Constants.GoldenFrame || left1 == Constants.GoldenFrame
                                ? 1
                                : 0);
                        }
                        else
                        {
                            predContext = 2;
                        }
                    }
                    else if (aboveHasSecond || leftHasSecond)
                    {
                        sbyte rfs = !aboveHasSecond ? above0 : left0;
                        sbyte crf1 = aboveHasSecond ? above0 : left0;
                        sbyte crf2 = aboveHasSecond ? above1 : left1;

                        if (rfs == Constants.GoldenFrame)
                        {
                            predContext = 3 + (crf1 == Constants.GoldenFrame || crf2 == Constants.GoldenFrame ? 1 : 0);
                        }
                        else if (rfs == Constants.AltRefFrame)
                        {
                            predContext = crf1 == Constants.GoldenFrame || crf2 == Constants.GoldenFrame ? 1 : 0;
                        }
                        else
                        {
                            predContext =
                                1 + (2 * (crf1 == Constants.GoldenFrame || crf2 == Constants.GoldenFrame ? 1 : 0));
                        }
                    }
                    else
                    {
                        if (above0 == Constants.LastFrame && left0 == Constants.LastFrame)
                        {
                            predContext = 3;
                        }
                        else if (above0 == Constants.LastFrame || left0 == Constants.LastFrame)
                        {
                            sbyte edge0 = above0 == Constants.LastFrame ? left0 : above0;
                            predContext = 4 * (edge0 == Constants.GoldenFrame ? 1 : 0);
                        }
                        else
                        {
                            predContext = (2 * (above0 == Constants.GoldenFrame ? 1 : 0)) +
                                          (2 * (left0 == Constants.GoldenFrame ? 1 : 0));
                        }
                    }
                }
            }
            else if (!xd.AboveMi.IsNull || !xd.LeftMi.IsNull)
            {
                // One edge available
                ref ModeInfo edgeMi = ref !xd.AboveMi.IsNull ? ref xd.AboveMi.Value : ref xd.LeftMi.Value;

                if (!edgeMi.IsInterBlock() || (edgeMi.RefFrame[0] == Constants.LastFrame && !edgeMi.HasSecondRef()))
                {
                    predContext = 2;
                }
                else if (!edgeMi.HasSecondRef())
                {
                    predContext = 4 * (edgeMi.RefFrame[0] == Constants.GoldenFrame ? 1 : 0);
                }
                else
                {
                    predContext = 3 * (edgeMi.RefFrame[0] == Constants.GoldenFrame ||
                                       edgeMi.RefFrame[1] == Constants.GoldenFrame
                        ? 1
                        : 0);
                }
            }
            else
            {
                // No edges available (2)
                predContext = 2;
            }

            Debug.Assert(predContext >= 0 && predContext < Constants.RefContexts);
            return predContext;
        }
    }
}
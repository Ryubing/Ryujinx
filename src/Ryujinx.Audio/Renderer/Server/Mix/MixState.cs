using Ryujinx.Audio.Renderer.Common;
using Ryujinx.Audio.Renderer.Parameter;
using Ryujinx.Audio.Renderer.Server.Effect;
using Ryujinx.Audio.Renderer.Server.Splitter;
using Ryujinx.Common.Utilities;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ryujinx.Audio.Constants;

namespace Ryujinx.Audio.Renderer.Server.Mix
{
    /// <summary>
    /// Server state for a mix.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 0x940, Pack = Alignment)]
    public struct MixState
    {
        public const uint InvalidDistanceFromFinalMix = 0x80000000;

        public const int Alignment = 0x10;

        /// <summary>
        /// Base volume of the mix.
        /// </summary>
        public float Volume;

        /// <summary>
        /// Target sample rate of the mix.
        /// </summary>
        public uint SampleRate;

        /// <summary>
        /// Target buffer count.
        /// </summary>
        public uint BufferCount;

        /// <summary>
        /// Set to true if in use.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool IsUsed;

        /// <summary>
        /// The id of the mix.
        /// </summary>
        public int MixId;

        /// <summary>
        /// The mix node id.
        /// </summary>
        public int NodeId;

        /// <summary>
        /// the buffer offset to use for command generation.
        /// </summary>
        public uint BufferOffset;

        /// <summary>
        /// The distance of the mix from the final mix.
        /// </summary>
        public uint DistanceFromFinalMix;

        /// <summary>
        /// The effect processing order storage.
        /// </summary>
        private readonly nint _effectProcessingOrderArrayPointer;

        /// <summary>
        /// The max element count that can be found in the effect processing order storage.
        /// </summary>
        public uint EffectProcessingOrderArrayMaxCount;

        /// <summary>
        /// The mix to output the result of this mix.
        /// </summary>
        public int DestinationMixId;

        /// <summary>
        /// Mix buffer volumes storage.
        /// </summary>
        private MixVolumeArray _mixVolumeArray;

        /// <summary>
        /// The splitter to output the result of this mix.
        /// </summary>
        public uint DestinationSplitterId;

        /// <summary>
        /// If set to true, the long size pre-delay is supported on the reverb command.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool IsLongSizePreDelaySupported;

        [StructLayout(LayoutKind.Sequential, Size = Size, Pack = 1)]
        private struct MixVolumeArray
        {
            private const int Size = 4 * MixBufferCountMax * MixBufferCountMax;
        }

        /// <summary>
        /// Mix buffer volumes.
        /// </summary>
        /// <remarks>Used when no splitter id is specified.</remarks>
        public Span<float> MixBufferVolume => SpanHelpers.AsSpan<MixVolumeArray, float>(ref _mixVolumeArray);

        /// <summary>
        /// Get the volume for a given connection destination.
        /// </summary>
        /// <param name="sourceIndex">The source node index.</param>
        /// <param name="destinationIndex">The destination node index</param>
        /// <returns>The volume for the given connection destination.</returns>
        public float GetMixBufferVolume(int sourceIndex, int destinationIndex)
        {
            return MixBufferVolume[sourceIndex * MixBufferCountMax + destinationIndex];
        }

        /// <summary>
        /// The array used to order effects associated to this mix.
        /// </summary>
        public readonly Span<int> EffectProcessingOrderArray
        {
            get
            {
                if (_effectProcessingOrderArrayPointer == nint.Zero)
                {
                    return [];
                }

                unsafe
                {
                    return new Span<int>((void*)_effectProcessingOrderArrayPointer, (int)EffectProcessingOrderArrayMaxCount);
                }
            }
        }

        /// <summary>
        /// Create a new <see cref="MixState"/>
        /// </summary>
        /// <param name="effectProcessingOrderArray"></param>
        /// <param name="behaviourContext"></param>
        public MixState(Memory<int> effectProcessingOrderArray, ref BehaviourContext behaviourContext) : this()
        {
            MixId = UnusedMixId;

            DistanceFromFinalMix = InvalidDistanceFromFinalMix;

            DestinationMixId = UnusedMixId;

            DestinationSplitterId = UnusedSplitterId;

            unsafe
            {
                // SAFETY: safe as effectProcessingOrderArray comes from the work buffer memory that is pinned.
                _effectProcessingOrderArrayPointer = (nint)Unsafe.AsPointer(ref MemoryMarshal.GetReference(effectProcessingOrderArray.Span));
            }

            EffectProcessingOrderArrayMaxCount = (uint)effectProcessingOrderArray.Length;

            IsLongSizePreDelaySupported = behaviourContext.IsLongSizePreDelaySupported();

            ClearEffectProcessingOrder();
        }

        /// <summary>
        /// Clear the <see cref="DistanceFromFinalMix"/> value to its default state.
        /// </summary>
        public void ClearDistanceFromFinalMix()
        {
            DistanceFromFinalMix = InvalidDistanceFromFinalMix;
        }

        /// <summary>
        /// Clear the <see cref="EffectProcessingOrderArray"/> to its default state.
        /// </summary>
        public readonly void ClearEffectProcessingOrder()
        {
            EffectProcessingOrderArray.Fill(-1);
        }

        /// <summary>
        /// Return true if the mix has any destinations.
        /// </summary>
        /// <returns>True if the mix has any destinations.</returns>
        public readonly bool HasAnyDestination()
        {
            return DestinationMixId != UnusedMixId || DestinationSplitterId != UnusedSplitterId;
        }

        /// <summary>
        /// Update the mix connection on the adjacency matrix.
        /// </summary>
        /// <param name="edgeMatrix">The adjacency matrix.</param>
        /// <param name="parameter">The input parameter of the mix.</param>
        /// <param name="splitterContext">The splitter context.</param>
        /// <returns>Return true, new connections were done on the adjacency matrix.</returns>
        private bool UpdateConnection(EdgeMatrix edgeMatrix, in MixParameter parameter, ref SplitterContext splitterContext)
        {
            bool hasNewConnections;

            if (DestinationSplitterId == UnusedSplitterId)
            {
                hasNewConnections = false;
            }
            else
            {
                ref SplitterState splitter = ref splitterContext.GetState((int)DestinationSplitterId);

                hasNewConnections = splitter.HasNewConnection;
            }

            if (DestinationMixId == parameter.DestinationMixId && DestinationSplitterId == parameter.DestinationSplitterId && !hasNewConnections)
            {
                return false;
            }

            edgeMatrix.RemoveEdges(MixId);

            if (parameter.DestinationMixId == UnusedMixId)
            {
                if (parameter.DestinationSplitterId != UnusedSplitterId)
                {
                    ref SplitterState splitter = ref splitterContext.GetState((int)parameter.DestinationSplitterId);

                    for (int i = 0; i < splitter.DestinationCount; i++)
                    {
                        SplitterDestination destination = splitter.GetData(i);

                        if (!destination.IsNull)
                        {
                            int destinationMixId = destination.DestinationId;

                            if (destinationMixId != UnusedMixId)
                            {
                                edgeMatrix.Connect(MixId, destinationMixId);
                            }
                        }
                    }
                }
            }
            else
            {
                edgeMatrix.Connect(MixId, parameter.DestinationMixId);
            }

            DestinationMixId = parameter.DestinationMixId;
            DestinationSplitterId = parameter.DestinationSplitterId;

            return true;
        }

        /// <summary>
        /// Update the mix from user information.
        /// </summary>
        /// <param name="edgeMatrix">The adjacency matrix.</param>
        /// <param name="parameter">The input parameter of the mix.</param>
        /// <param name="effectContext">The effect context.</param>
        /// <param name="splitterContext">The splitter context.</param>
        /// <param name="behaviourContext">The behaviour context.</param>
        /// <returns>Return true if the mix was changed.</returns>
        public bool Update(EdgeMatrix edgeMatrix, in MixParameter parameter, EffectContext effectContext, SplitterContext splitterContext, BehaviourContext behaviourContext)
        {
            bool isDirty;

            Volume = parameter.Volume;
            SampleRate = parameter.SampleRate;
            BufferCount = parameter.BufferCount;
            IsUsed = parameter.IsUsed;
            MixId = parameter.MixId;
            NodeId = parameter.NodeId;
            parameter.MixBufferVolume.CopyTo(MixBufferVolume);

            if (behaviourContext.IsSplitterSupported())
            {
                isDirty = UpdateConnection(edgeMatrix, in parameter, ref splitterContext);
            }
            else
            {
                isDirty = DestinationMixId != parameter.DestinationMixId;

                if (DestinationMixId != parameter.DestinationMixId)
                {
                    DestinationMixId = parameter.DestinationMixId;
                }

                DestinationSplitterId = UnusedSplitterId;
            }

            ClearEffectProcessingOrder();

            for (int i = 0; i < effectContext.GetCount(); i++)
            {
                ref BaseEffect effect = ref effectContext.GetEffect(i);

                if (effect.MixId == MixId)
                {
                    Debug.Assert(effect.ProcessingOrder <= EffectProcessingOrderArrayMaxCount);

                    if (effect.ProcessingOrder > EffectProcessingOrderArrayMaxCount)
                    {
                        return isDirty;
                    }

                    EffectProcessingOrderArray[(int)effect.ProcessingOrder] = i;
                }
            }

            return isDirty;
        }
    }
}

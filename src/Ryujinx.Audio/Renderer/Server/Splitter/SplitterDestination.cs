using Ryujinx.Audio.Renderer.Parameter;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ryujinx.Audio.Renderer.Server.Splitter
{
    /// <summary>
    /// Server state for a splitter destination.
    /// </summary>
    public ref struct SplitterDestination
    {
        private ref SplitterDestinationVersion1 _v1;
        private ref SplitterDestinationVersion2 _v2;

        /// <summary>
        /// Checks if the splitter destination data reference is null.
        /// </summary>
        public bool IsNull => Unsafe.IsNullRef(ref _v1) && Unsafe.IsNullRef(ref _v2);

        /// <summary>
        /// The splitter unique id.
        /// </summary>
        public int Id
        {
            get
            {
                if (Unsafe.IsNullRef(ref _v2))
                {
                    if (Unsafe.IsNullRef(ref _v1))
                    {
                        return 0;
                    }
                    else
                    {
                        return _v1.Id;
                    }
                }
                else
                {
                    return _v2.Id;
                }
            }
        }

        /// <summary>
        /// The mix to output the result of the splitter.
        /// </summary>
        public int DestinationId
        {
            get
            {
                if (Unsafe.IsNullRef(ref _v2))
                {
                    if (Unsafe.IsNullRef(ref _v1))
                    {
                        return 0;
                    }
                    else
                    {
                        return _v1.DestinationId;
                    }
                }
                else
                {
                    return _v2.DestinationId;
                }
            }
        }

        /// <summary>
        /// Mix buffer volumes.
        /// </summary>
        /// <remarks>Used when a splitter id is specified in the mix.</remarks>
        public Span<float> MixBufferVolume
        {
            get
            {
                if (Unsafe.IsNullRef(ref _v2))
                {
                    if (Unsafe.IsNullRef(ref _v1))
                    {
                        return [];
                    }
                    else
                    {
                        return _v1.MixBufferVolume;
                    }
                }
                else
                {
                    return _v2.MixBufferVolume;
                }
            }
        }

        /// <summary>
        /// Previous mix buffer volumes.
        /// </summary>
        /// <remarks>Used when a splitter id is specified in the mix.</remarks>
        public Span<float> PreviousMixBufferVolume
        {
            get
            {
                if (Unsafe.IsNullRef(ref _v2))
                {
                    if (Unsafe.IsNullRef(ref _v1))
                    {
                        return [];
                    }
                    else
                    {
                        return _v1.PreviousMixBufferVolume;
                    }
                }
                else
                {
                    return _v2.PreviousMixBufferVolume;
                }
            }
        }

        /// <summary>
        /// Get the <see cref="SplitterDestination"/> of the next element or null if not present.
        /// </summary>
        public readonly SplitterDestination Next
        {
            get
            {
                unsafe
                {
                    if (Unsafe.IsNullRef(ref _v2))
                    {
                        if (Unsafe.IsNullRef(ref _v1))
                        {
                            return new SplitterDestination();
                        }
                        else
                        {
                            return new SplitterDestination(ref _v1.Next);
                        }
                    }
                    else
                    {
                        return new SplitterDestination(ref _v2.Next);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a new splitter destination wrapper for the version 1 splitter destination data.
        /// </summary>
        /// <param name="v1">Version 1 splitter destination data</param>
        public SplitterDestination(ref SplitterDestinationVersion1 v1)
        {
            _v1 = ref v1;
            _v2 = ref Unsafe.NullRef<SplitterDestinationVersion2>();
        }

        /// <summary>
        /// Creates a new splitter destination wrapper for the version 2 splitter destination data.
        /// </summary>
        /// <param name="v2">Version 2 splitter destination data</param>
        public SplitterDestination(ref SplitterDestinationVersion2 v2)
        {

            _v1 = ref Unsafe.NullRef<SplitterDestinationVersion1>();
            _v2 = ref v2;
        }

        /// <summary>
        /// Creates a new splitter destination wrapper for the splitter destination data.
        /// </summary>
        /// <param name="v1">Version 1 splitter destination data</param>
        /// <param name="v2">Version 2 splitter destination data</param>
        public unsafe SplitterDestination(SplitterDestinationVersion1* v1, SplitterDestinationVersion2* v2)
        {
            _v1 = ref Unsafe.AsRef<SplitterDestinationVersion1>(v1);
            _v2 = ref Unsafe.AsRef<SplitterDestinationVersion2>(v2);
        }

        /// <summary>
        /// Update the splitter destination data from user parameter.
        /// </summary>
        /// <param name="parameter">The user parameter.</param>
        /// <param name="isPrevVolumeResetSupported">Indicates that the audio renderer revision in use supports explicitly resetting the volume.</param>
        public void Update<T>(in T parameter, bool isPrevVolumeResetSupported) where T : ISplitterDestinationInParameter
        {
            if (Unsafe.IsNullRef(ref _v2))
            {
                _v1.Update(parameter, isPrevVolumeResetSupported);
            }
            else
            {
                _v2.Update(parameter, isPrevVolumeResetSupported);
            }
        }

        /// <summary>
        /// Update the internal state of the instance.
        /// </summary>
        public void UpdateInternalState()
        {
            if (Unsafe.IsNullRef(ref _v2))
            {
                _v1.UpdateInternalState();
            }
            else
            {
                _v2.UpdateInternalState();
            }
        }

        /// <summary>
        /// Set the update internal state marker.
        /// </summary>
        public void MarkAsNeedToUpdateInternalState()
        {
            if (Unsafe.IsNullRef(ref _v2))
            {
                _v1.MarkAsNeedToUpdateInternalState();
            }
            else
            {
                _v2.MarkAsNeedToUpdateInternalState();
            }
        }

        /// <summary>
        /// Return true if the splitter destination is used and has a destination.
        /// </summary>
        /// <returns>True if the splitter destination is used and has a destination.</returns>
        public readonly bool IsConfigured()
        {
            return Unsafe.IsNullRef(ref _v2) ? _v1.IsConfigured() : _v2.IsConfigured();
        }

        /// <summary>
        /// Get the volume for a given destination.
        /// </summary>
        /// <param name="destinationIndex">The destination index to use.</param>
        /// <returns>The volume for the given destination.</returns>
        public float GetMixVolume(int destinationIndex)
        {
            return Unsafe.IsNullRef(ref _v2) ? _v1.GetMixVolume(destinationIndex) : _v2.GetMixVolume(destinationIndex);
        }

        /// <summary>
        /// Get the previous volume for a given destination.
        /// </summary>
        /// <param name="destinationIndex">The destination index to use.</param>
        /// <returns>The volume for the given destination.</returns>
        public float GetMixVolumePrev(int destinationIndex)
        {
            return Unsafe.IsNullRef(ref _v2) ? _v1.GetMixVolumePrev(destinationIndex) : _v2.GetMixVolumePrev(destinationIndex);
        }

        /// <summary>
        /// Clear the volumes.
        /// </summary>
        public void ClearVolumes()
        {
            if (Unsafe.IsNullRef(ref _v2))
            {
                _v1.ClearVolumes();
            }
            else
            {
                _v2.ClearVolumes();
            }
        }

        /// <summary>
        /// Link the next element to the given splitter destination.
        /// </summary>
        /// <param name="next">The given splitter destination to link.</param>
        public void Link(SplitterDestination next)
        {
            if (Unsafe.IsNullRef(ref _v2))
            {
                Debug.Assert(!Unsafe.IsNullRef(ref next._v1));

                _v1.Link(ref next._v1);
            }
            else
            {
                Debug.Assert(!Unsafe.IsNullRef(ref next._v2));

                _v2.Link(ref next._v2);
            }
        }

        /// <summary>
        /// Remove the link to the next element.
        /// </summary>
        public void Unlink()
        {
            if (Unsafe.IsNullRef(ref _v2))
            {
                _v1.Unlink();
            }
            else
            {
                _v2.Unlink();
            }
        }

        /// <summary>
        /// Checks if any biquad filter is enabled.
        /// </summary>
        /// <returns>True if any biquad filter is enabled.</returns>
        public bool IsBiquadFilterEnabled()
        {
            return !Unsafe.IsNullRef(ref _v2) && _v2.IsBiquadFilterEnabled();
        }

        /// <summary>
        /// Checks if any biquad filter was previously enabled.
        /// </summary>
        /// <returns>True if any biquad filter was previously enabled.</returns>
        public bool IsBiquadFilterEnabledPrev()
        {
            return !Unsafe.IsNullRef(ref _v2) && _v2.IsBiquadFilterEnabledPrev();
        }

        /// <summary>
        /// Gets the biquad filter parameters.
        /// </summary>
        /// <param name="index">Biquad filter index (0 or 1).</param>
        /// <returns>Biquad filter parameters.</returns>
        public ref BiquadFilterParameter GetBiquadFilterParameter(int index)
        {
            Debug.Assert(!Unsafe.IsNullRef(ref _v2));

            return ref _v2.GetBiquadFilterParameter(index);
        }

        /// <summary>
        /// Checks if any biquad filter was previously enabled.
        /// </summary>
        /// <param name="index">Biquad filter index (0 or 1).</param>
        public void UpdateBiquadFilterEnabledPrev(int index)
        {
            if (!Unsafe.IsNullRef(ref _v2))
            {
                _v2.UpdateBiquadFilterEnabledPrev(index);
            }
        }

        /// <summary>
        /// Get the reference for the version 1 splitter destination data, or null if version 2 is being used or the destination is null.
        /// </summary>
        /// <returns>Reference for the version 1 splitter destination data.</returns>
        public ref SplitterDestinationVersion1 GetV1RefOrNull()
        {
            return ref _v1;
        }

        /// <summary>
        /// Get the reference for the version 2 splitter destination data, or null if version 1 is being used or the destination is null.
        /// </summary>
        /// <returns>Reference for the version 2 splitter destination data.</returns>
        public ref SplitterDestinationVersion2 GetV2RefOrNull()
        {
            return ref _v2;
        }
    }
}

using System.Diagnostics;

namespace Ryujinx.Audio.Renderer.Device
{
    /// <summary>
    /// Represents a virtual device used by IAudioDevice.
    /// </summary>
    public class VirtualDevice
    {
        /// <summary>
        /// All the defined virtual devices.
        /// </summary>
        public static readonly VirtualDevice[] Devices =
        [
            new("AudioStereoJackOutput", 2, true),
            new("AudioBuiltInSpeakerOutput", 2, false),
            new("AudioTvOutput", 6, false),
            new("AudioUsbDeviceOutput", 2, true),
            new("AudioExternalOutput", 6, true)
        ];

        /// <summary>
        /// The name of the <see cref="VirtualDevice"/>.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The count of channels supported by the <see cref="VirtualDevice"/>.
        /// </summary>
        public uint ChannelCount { get; }

        /// <summary>
        /// The system master volume of the <see cref="VirtualDevice"/>.
        /// </summary>
        public float MasterVolume { get; private set; }

        /// <summary>
        /// Define if the <see cref="VirtualDevice"/> is provided by an external interface.
        /// </summary>
        public bool IsExternalOutput { get; }

        /// <summary>
        /// Create a new <see cref="VirtualDevice"/> instance.
        /// </summary>
        /// <param name="name">The name of the <see cref="VirtualDevice"/>.</param>
        /// <param name="channelCount">The count of channels supported by the <see cref="VirtualDevice"/>.</param>
        /// <param name="isExternalOutput">Indicate if the <see cref="VirtualDevice"/> is provided by an external interface.</param>
        public VirtualDevice(string name, uint channelCount, bool isExternalOutput)
        {
            Name = name;
            ChannelCount = channelCount;
            IsExternalOutput = isExternalOutput;
        }

        /// <summary>
        /// Update the master volume of the <see cref="VirtualDevice"/>.
        /// </summary>
        /// <param name="volume">The new master volume.</param>
        public void UpdateMasterVolume(float volume)
        {
            Debug.Assert(volume >= 0.0f && volume <= 1.0f);

            MasterVolume = volume;
        }

        /// <summary>
        /// Check if the <see cref="VirtualDevice"/> is a usb device.
        /// </summary>
        /// <returns>Returns true if the <see cref="VirtualDevice"/> is a usb device.</returns>
        public bool IsUsbDevice()
        {
            return Name.Equals("AudioUsbDeviceOutput");
        }

        /// <summary>
        /// Get the output device name of the <see cref="VirtualDevice"/>.
        /// </summary>
        /// <returns>The output device name of the <see cref="VirtualDevice"/>.</returns>
        public string GetOutputDeviceName()
        {
            if (IsExternalOutput)
            {
                return "AudioExternalOutput";
            }

            return Name;
        }
    }
}

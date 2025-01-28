namespace Ryujinx.Audio
{
    /// <summary>
    /// Define constants used by the audio system.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// The default device output name.
        /// </summary>
        public const string DefaultDeviceOutputName = "DeviceOut";

        /// <summary>
        /// The default device input name.
        /// </summary>
        public const string DefaultDeviceInputName = "BuiltInHeadset";

        /// <summary>
        /// The maximum number of channels supported. (6 channels for 5.1 surround)
        /// </summary>
        public const int ChannelCountMax = 6;

        /// <summary>
        /// The maximum number of channels supported per voice.
        /// </summary>
        public const int VoiceChannelCountMax = ChannelCountMax;

        /// <summary>
        /// The maximum count of mix buffer supported per operations (volumes, mix effect, ...)
        /// </summary>
        public const int MixBufferCountMax = 24;

        /// <summary>
        /// The maximum count of wavebuffer per voice.
        /// </summary>
        public const int VoiceWaveBufferCount = 4;

        /// <summary>
        /// The maximum count of biquad filter per voice.
        /// </summary>
        public const int VoiceBiquadFilterCount = 2;

        /// <summary>
        /// The lowest priority that a voice can have.
        /// </summary>
        public const int VoiceLowestPriority = 0xFF;

        /// <summary>
        /// The highest priority that a voice can have.
        /// </summary>
        /// <remarks>Voices with the highest priority will not be dropped if a voice drop needs to occur.</remarks>
        public const int VoiceHighestPriority = 0;

        /// <summary>
        /// Maximum <see cref="Common.BehaviourParameter.ErrorInfo"/> that can be returned by <see cref="Parameter.BehaviourErrorInfoOutStatus"/>.
        /// </summary>
        public const int MaxErrorInfos = 10;

        /// <summary>
        /// Default alignment for buffers.
        /// </summary>
        public const int BufferAlignment = 0x40;

        /// <summary>
        /// Alignment required for the work buffer.
        /// </summary>
        public const int WorkBufferAlignment = 0x1000;

        /// <summary>
        /// Alignment required for every performance metrics frame.
        /// </summary>
        public const int PerformanceMetricsPerFramesSizeAlignment = 0x100;

        /// <summary>
        /// The id of the final mix.
        /// </summary>
        public const int FinalMixId = 0;

        /// <summary>
        /// The id defining an unused mix id.
        /// </summary>
        public const int UnusedMixId = int.MaxValue;

        /// <summary>
        /// The id defining an unused splitter id as a signed integer.
        /// </summary>
        public const int UnusedSplitterIdInt = -1;

        /// <summary>
        /// The id defining an unused splitter id.
        /// </summary>
        public const uint UnusedSplitterId = uint.MaxValue;

        /// <summary>
        /// The id of invalid/unused node id.
        /// </summary>
        public const int InvalidNodeId = -268435456;

        /// <summary>
        /// The indice considered invalid for processing order.
        /// </summary>
        public const int InvalidProcessingOrder = -1;

        /// <summary>
        /// The maximum number of audio renderer sessions allowed to be created system wide.
        /// </summary>
        public const int AudioRendererSessionCountMax = 2;

        /// <summary>
        /// The maximum number of audio output sessions allowed to be created system wide.
        /// </summary>
        public const int AudioOutSessionCountMax = 12;

        /// <summary>
        /// The maximum number of audio input sessions allowed to be created system wide.
        /// </summary>
        public const int AudioInSessionCountMax = 4;

        /// <summary>
        /// Maximum buffers supported by one audio device session.
        /// </summary>
        public const int AudioDeviceBufferCountMax = 32;

        /// <summary>
        /// The target sample rate of the audio renderer. (48kHz)
        /// </summary>
        public const uint TargetSampleRate = 48000;

        /// <summary>
        /// The target sample size of the audio renderer. (PCM16)
        /// </summary>
        public const int TargetSampleSize = sizeof(ushort);

        /// <summary>
        /// The target sample count per audio renderer update.
        /// </summary>
        public const int TargetSampleCount = 240;

        /// <summary>
        /// The size of an upsampler entry to process upsampling to <see cref="TargetSampleRate"/>.
        /// </summary>
        public const int UpSampleEntrySize = TargetSampleCount * VoiceChannelCountMax;

        /// <summary>
        /// The target audio latency computed from <see cref="TargetSampleRate"/> and <see cref="TargetSampleCount"/>.
        /// </summary>
        public const int AudioProcessorMaxUpdateTimeTarget = 1000000000 / ((int)TargetSampleRate / TargetSampleCount); // 5.00 ms

        /// <summary>
        /// The maximum update time of the DSP on original hardware.
        /// </summary>
        public const int AudioProcessorMaxUpdateTime = 5760000; // 5.76 ms

        /// <summary>
        /// The maximum update time per audio renderer session.
        /// </summary>
        public const int AudioProcessorMaxUpdateTimePerSessions = AudioProcessorMaxUpdateTime / AudioRendererSessionCountMax;

        /// <summary>
        /// Guest timer frequency used for system ticks.
        /// </summary>
        public const int TargetTimerFrequency = 19200000;

        /// <summary>
        /// The default coefficients used for standard 5.1 surround to stereo downmixing.
        /// </summary>
        public static readonly float[] DefaultSurroundToStereoCoefficients =
        [
            1.0f,
            0.707f,
            0.251f,
            0.707f
        ];
    }
}

namespace Ryujinx.Common.Configuration.Hid.Controller
{
    public class LedConfigController
    {
        /// <summary>
        ///     Packed RGB int of the color
        /// </summary>
        public uint LedColor { get; set; }

        /// <summary>
        /// Enable LED color changing by the emulator
        /// </summary>
        public bool EnableLed { get; set; }
    }
}

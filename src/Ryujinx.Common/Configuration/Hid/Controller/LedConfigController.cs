namespace Ryujinx.Common.Configuration.Hid.Controller
{
    public class LedConfigController
    {
        /// <summary>
        /// Enable LED color changing by the emulator
        /// </summary>
        public bool EnableLed { get; set; }
        
        /// <summary>
        /// Ignores the color and disables the LED entirely.
        /// </summary>
        public bool TurnOffLed { get; set; }
        
        /// <summary>
        /// Ignores the color and uses the rainbow color functionality for the LED.
        /// </summary>
        public bool UseRainbow { get; set; }
        
        /// <summary>
        ///     Packed RGB int of the color
        /// </summary>
        public uint LedColor { get; set; }
    }
}

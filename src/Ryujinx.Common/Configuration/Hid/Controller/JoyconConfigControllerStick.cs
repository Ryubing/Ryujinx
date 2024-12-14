namespace Ryujinx.Common.Configuration.Hid.Controller
{
    public class JoyconConfigControllerStick<TButton, TStick> 
        where TButton : unmanaged 
        where TStick : unmanaged
    {
        public TStick Joystick { get; set; }
        public bool InvertStickX { get; set; }
        public bool InvertStickY { get; set; }
        public bool Rotate90CW { get; set; }
        public TButton StickButton { get; set; }
    }
}

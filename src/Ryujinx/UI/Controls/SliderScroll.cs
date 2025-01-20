using Avalonia.Controls;
using Avalonia.Input;
using System;

namespace Ryujinx.Ava.UI.Controls
{
    public class SliderScroll : Slider
    {
        protected override Type StyleKeyOverride => typeof(Slider);

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            Value = Math.Clamp(Value + e.Delta.Y * TickFrequency, Minimum, Maximum);

            e.Handled = true;
        }
    }
}

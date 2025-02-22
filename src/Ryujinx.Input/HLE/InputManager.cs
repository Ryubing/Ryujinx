using System;

namespace Ryujinx.Input.HLE
{
    public class InputManager(IGamepadDriver keyboardDriver, IGamepadDriver gamepadDriver, IHandheld handheld)
        : IDisposable
    {
        public IGamepadDriver KeyboardDriver { get; } = keyboardDriver;
        public IGamepadDriver GamepadDriver { get; } = gamepadDriver;
        public IGamepadDriver MouseDriver { get; private set; }
        public IHandheld Handheld { get; } = handheld;

        public void SetMouseDriver(IGamepadDriver mouseDriver)
        {
            MouseDriver?.Dispose();

            MouseDriver = mouseDriver;
        }

        public NpadManager CreateNpadManager()
        {
            return new NpadManager(KeyboardDriver, GamepadDriver, MouseDriver, Handheld);
        }

        public TouchScreenManager CreateTouchScreenManager()
        {
            if (MouseDriver == null)
            {
                throw new InvalidOperationException("Mouse Driver has not been initialized.");
            }

            return new TouchScreenManager(MouseDriver.GetGamepad("0") as IMouse);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                KeyboardDriver?.Dispose();
                GamepadDriver?.Dispose();
                MouseDriver?.Dispose();
                Handheld?.Dispose();
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }
    }
}

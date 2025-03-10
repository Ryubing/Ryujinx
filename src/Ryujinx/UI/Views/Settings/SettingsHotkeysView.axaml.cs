using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Ryujinx.Ava.Input;
using Ryujinx.Ava.UI.Controls;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Input;
using Ryujinx.Input.Assigner;
using Button = Ryujinx.Input.Button;
using Key = Ryujinx.Common.Configuration.Hid.Key;

namespace Ryujinx.Ava.UI.Views.Settings
{
    public partial class SettingsHotkeysView : RyujinxControl<SettingsViewModel>
    {
        private ButtonKeyAssigner _currentAssigner;
        private readonly IGamepadDriver _avaloniaKeyboardDriver;

        public SettingsHotkeysView()
        {
            InitializeComponent();

            foreach (ILogical visual in SettingButtons.GetLogicalDescendants())
            {
                if (visual is ToggleButton button and not CheckBox)
                {
                    button.IsCheckedChanged += Button_IsCheckedChanged;
                }
            }

            _avaloniaKeyboardDriver = new AvaloniaKeyboardDriver(this);
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            if (!_currentAssigner?.ToggledButton?.IsPointerOver ?? false)
            {
                _currentAssigner.Cancel();
            }
        }

        private void MouseClick(object sender, PointerPressedEventArgs e)
        {
            bool shouldUnbind = e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed;

            _currentAssigner?.Cancel(shouldUnbind);

            PointerPressed -= MouseClick;
        }

        private void Button_IsCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton button)
            {
                if ((bool)button.IsChecked)
                {
                    if (_currentAssigner != null && button == _currentAssigner.ToggledButton)
                    {
                        return;
                    }

                    if (_currentAssigner == null)
                    {
                        _currentAssigner = new ButtonKeyAssigner(button);

                        this.Focus(NavigationMethod.Pointer);

                        PointerPressed += MouseClick;

                        IKeyboard keyboard = (IKeyboard)_avaloniaKeyboardDriver.GetGamepad("0");
                        IButtonAssigner assigner = new KeyboardKeyAssigner(keyboard);

                        _currentAssigner.ButtonAssigned += (sender, e) =>
                        {
                            if (e.ButtonValue.HasValue)
                            {
                                Button buttonValue = e.ButtonValue.Value;

                                Dispatcher.UIThread.Post(() =>
                                {
                                    switch (button.Name)
                                    {
                                        case "ToggleVSyncMode":
                                            ViewModel.KeyboardHotkey.ToggleVSyncMode = buttonValue.AsHidType<Key>();
                                            break;
                                        case "Screenshot":
                                            ViewModel.KeyboardHotkey.Screenshot = buttonValue.AsHidType<Key>();
                                            break;
                                        case "ShowUI":
                                            ViewModel.KeyboardHotkey.ShowUI = buttonValue.AsHidType<Key>();
                                            break;
                                        case "Pause":
                                            ViewModel.KeyboardHotkey.Pause = buttonValue.AsHidType<Key>();
                                            break;
                                        case "ToggleMute":
                                            ViewModel.KeyboardHotkey.ToggleMute = buttonValue.AsHidType<Key>();
                                            break;
                                        case "ResScaleUp":
                                            ViewModel.KeyboardHotkey.ResScaleUp = buttonValue.AsHidType<Key>();
                                            break;
                                        case "ResScaleDown":
                                            ViewModel.KeyboardHotkey.ResScaleDown = buttonValue.AsHidType<Key>();
                                            break;
                                        case "VolumeUp":
                                            ViewModel.KeyboardHotkey.VolumeUp = buttonValue.AsHidType<Key>();
                                            break;
                                        case "VolumeDown":
                                            ViewModel.KeyboardHotkey.VolumeDown = buttonValue.AsHidType<Key>();
                                            break;
                                        case "CustomVSyncIntervalIncrement":
                                            ViewModel.KeyboardHotkey.CustomVSyncIntervalIncrement =
                                                buttonValue.AsHidType<Key>();
                                            break;
                                        case "CustomVSyncIntervalDecrement":
                                            ViewModel.KeyboardHotkey.CustomVSyncIntervalDecrement =
                                                buttonValue.AsHidType<Key>();
                                            break;
                                        case "TurboMode":
                                            ViewModel.KeyboardHotkey.TurboMode = buttonValue.AsHidType<Key>();
                                            break;
                                    }
                                });
                            }
                        };

                        _currentAssigner.GetInputAndAssign(assigner, keyboard);
                    }
                    else
                    {
                        if (_currentAssigner != null)
                        {
                            _currentAssigner.Cancel();
                            _currentAssigner = null;
                            button.IsChecked = false;
                        }
                    }
                }
                else
                {
                    _currentAssigner?.Cancel();
                    _currentAssigner = null;
                }
            }
        }

        public void Dispose()
        {
            _currentAssigner?.Cancel();
            _currentAssigner = null;

            _avaloniaKeyboardDriver.Dispose();
        }
    }
}

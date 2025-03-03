using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Ryujinx.Ava.UI.Controls;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.ViewModels.Input;
using Ryujinx.Input;
using Ryujinx.Input.Assigner;
using Button = Ryujinx.Input.Button;
using Key = Ryujinx.Common.Configuration.Hid.Key;

namespace Ryujinx.Ava.UI.Views.Input
{
    public partial class KeyboardInputView : RyujinxControl<KeyboardInputViewModel>
    {
        private ButtonKeyAssigner _currentAssigner;

        public KeyboardInputView()
        {
            InitializeComponent();

            foreach (ILogical visual in SettingButtons.GetLogicalDescendants())
            {
                if (visual is ToggleButton button and not CheckBox)
                {
                    button.IsCheckedChanged += Button_IsCheckedChanged;
                }
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            if (_currentAssigner is { ToggledButton.IsPointerOver: false })
            {
                _currentAssigner.Cancel();
            }
        }

        private void Button_IsCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (sender is not ToggleButton button) 
                return;
            
            if (button.IsChecked is true)
            {
                if (_currentAssigner != null && button == _currentAssigner.ToggledButton)
                {
                    return;
                }

                if (_currentAssigner == null)
                {
                    _currentAssigner = new ButtonKeyAssigner(button);

                    Focus(NavigationMethod.Pointer);

                    PointerPressed += MouseClick;

                    IKeyboard keyboard =
                        (IKeyboard)ViewModel.ParentModel.AvaloniaKeyboardDriver.GetGamepad("0"); // Open Avalonia keyboard for cancel operations.
                    IButtonAssigner assigner = 
                        new KeyboardKeyAssigner((IKeyboard)ViewModel.ParentModel.SelectedGamepad);

                    _currentAssigner.ButtonAssigned += (_, be) =>
                    {
                        if (be.ButtonValue.HasValue)
                        {
                            Button buttonValue = be.ButtonValue.Value;
                            ViewModel.ParentModel.IsModified = true;

                            switch (button.Name)
                            {
                                case "ButtonZl":
                                    ViewModel.Config.ButtonZl = buttonValue.AsHidType<Key>();
                                    break;
                                case "ButtonL":
                                    ViewModel.Config.ButtonL = buttonValue.AsHidType<Key>();
                                    break;
                                case "ButtonMinus":
                                    ViewModel.Config.ButtonMinus = buttonValue.AsHidType<Key>();
                                    break;
                                case "LeftStickButton":
                                    ViewModel.Config.LeftStickButton = buttonValue.AsHidType<Key>();
                                    break;
                                case "LeftStickUp":
                                    ViewModel.Config.LeftStickUp = buttonValue.AsHidType<Key>();
                                    break;
                                case "LeftStickDown":
                                    ViewModel.Config.LeftStickDown = buttonValue.AsHidType<Key>();
                                    break;
                                case "LeftStickRight":
                                    ViewModel.Config.LeftStickRight = buttonValue.AsHidType<Key>();
                                    break;
                                case "LeftStickLeft":
                                    ViewModel.Config.LeftStickLeft = buttonValue.AsHidType<Key>();
                                    break;
                                case "DpadUp":
                                    ViewModel.Config.DpadUp = buttonValue.AsHidType<Key>();
                                    break;
                                case "DpadDown":
                                    ViewModel.Config.DpadDown = buttonValue.AsHidType<Key>();
                                    break;
                                case "DpadLeft":
                                    ViewModel.Config.DpadLeft = buttonValue.AsHidType<Key>();
                                    break;
                                case "DpadRight":
                                    ViewModel.Config.DpadRight = buttonValue.AsHidType<Key>();
                                    break;
                                case "LeftButtonSr":
                                    ViewModel.Config.LeftButtonSr = buttonValue.AsHidType<Key>();
                                    break;
                                case "LeftButtonSl":
                                    ViewModel.Config.LeftButtonSl = buttonValue.AsHidType<Key>();
                                    break;
                                case "RightButtonSr":
                                    ViewModel.Config.RightButtonSr = buttonValue.AsHidType<Key>();
                                    break;
                                case "RightButtonSl":
                                    ViewModel.Config.RightButtonSl = buttonValue.AsHidType<Key>();
                                    break;
                                case "ButtonZr":
                                    ViewModel.Config.ButtonZr = buttonValue.AsHidType<Key>();
                                    break;
                                case "ButtonR":
                                    ViewModel.Config.ButtonR = buttonValue.AsHidType<Key>();
                                    break;
                                case "ButtonPlus":
                                    ViewModel.Config.ButtonPlus = buttonValue.AsHidType<Key>();
                                    break;
                                case "ButtonA":
                                    ViewModel.Config.ButtonA = buttonValue.AsHidType<Key>();
                                    break;
                                case "ButtonB":
                                    ViewModel.Config.ButtonB = buttonValue.AsHidType<Key>();
                                    break;
                                case "ButtonX":
                                    ViewModel.Config.ButtonX = buttonValue.AsHidType<Key>();
                                    break;
                                case "ButtonY":
                                    ViewModel.Config.ButtonY = buttonValue.AsHidType<Key>();
                                    break;
                                case "RightStickButton":
                                    ViewModel.Config.RightStickButton = buttonValue.AsHidType<Key>();
                                    break;
                                case "RightStickUp":
                                    ViewModel.Config.RightStickUp = buttonValue.AsHidType<Key>();
                                    break;
                                case "RightStickDown":
                                    ViewModel.Config.RightStickDown = buttonValue.AsHidType<Key>();
                                    break;
                                case "RightStickRight":
                                    ViewModel.Config.RightStickRight = buttonValue.AsHidType<Key>();
                                    break;
                                case "RightStickLeft":
                                    ViewModel.Config.RightStickLeft = buttonValue.AsHidType<Key>();
                                    break;
                            }
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

        private void MouseClick(object sender, PointerPressedEventArgs e)
        {
            bool shouldUnbind = e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed;

            _currentAssigner?.Cancel(shouldUnbind);

            PointerPressed -= MouseClick;
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _currentAssigner?.Cancel();
            _currentAssigner = null;
        }
    }
}

using Ryujinx.Ava.UI.Models.Input;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Ava.Utilities.Configuration;
using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Common.Configuration.Hid.Controller;
using Ryujinx.Common.Configuration.Hid.Controller.Motion;
using ConfigGamepadInputId = Ryujinx.Common.Configuration.Hid.Controller.GamepadInputId;
using ConfigStickInputId = Ryujinx.Common.Configuration.Hid.Controller.StickInputId;
using Ryujinx.Common.Logging;
using Ryujinx.Input;
using Ryujinx.Input.HLE;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Ava.Input
{
    public class AutoAssignController
    {
        private readonly InputManager _inputManager;
        private readonly MainWindowViewModel _viewModel;
        private readonly ConfigurationState _configurationState;
        private readonly ControllerConfigurator _controllerConfigurator;

        public event Action ConfigurationUpdated;

        public AutoAssignController(InputManager inputManager, MainWindowViewModel mainWindowViewModel)
        {
            _inputManager = inputManager;
            _viewModel = mainWindowViewModel;
            _configurationState = ConfigurationState.Instance;
            _controllerConfigurator = new ControllerConfigurator();

            _inputManager.GamepadDriver.OnGamepadConnected += HandleOnGamepadConnected;
            _inputManager.GamepadDriver.OnGamepadDisconnected += HandleOnGamepadDisconnected;

            RefreshControllers();
        }

        private void HandleOnGamepadConnected(string id)
        {
            Logger.Warning?.Print(LogClass.Application, $"Gamepad connected: {id}");
            RefreshControllers();
        }

        private void HandleOnGamepadDisconnected(string id)
        {
            Logger.Warning?.Print(LogClass.Application, $"Gamepad disconnected: {id}");
            RefreshControllers();
        }

        public void RefreshControllers()
        {
            if (!_configurationState.Hid.EnableAutoAssign) return;

            List<IGamepad> controllers = _inputManager.GamepadDriver.GetGamepads().ToList();
            List<InputConfig> oldConfig = _configurationState.Hid.InputConfig.Value.Where(x => x != null).ToList();

            List<InputConfig> newConfig = _controllerConfigurator.GetConfiguredControllers(
                controllers, oldConfig, new HashSet<int>(), out bool hasNewControllersConnected);

            _viewModel.AppHost?.NpadManager.ReloadConfiguration(newConfig, _configurationState.Hid.EnableKeyboard, _configurationState.Hid.EnableMouse);

            if (!hasNewControllersConnected)
            {
                // there is no *new* controller, we must switch the order of the controllers in
                // oldConfig to match the new order since probably a controller was disconnected
                // or an old controller was reconnected
                newConfig = _controllerConfigurator.ReorderControllers(newConfig, oldConfig);
            }

            _configurationState.Hid.InputConfig.Value = newConfig;
            
            // we want to save the configuration only if a *new* controller was connected
            if(hasNewControllersConnected)
            {
                ConfigurationState.Instance.ToFileFormat().SaveConfig(Program.ConfigurationPath);
            }
            ConfigurationUpdated?.Invoke();
        }
    }
}

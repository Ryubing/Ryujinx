using Avalonia.Controls;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using Gommon;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Controls;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Ava.UI.Windows;
using Ryujinx.Ava.Utilities.Configuration;
using Ryujinx.Common;
using Ryujinx.HLE;
using Ryujinx.HLE.HOS.Applets;
using Ryujinx.HLE.HOS.Applets.SoftwareKeyboard;
using Ryujinx.HLE.HOS.Services.Account.Acc;
using Ryujinx.HLE.HOS.Services.Am.AppletOE.ApplicationProxyService.ApplicationProxy.Types;
using Ryujinx.HLE.UI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace Ryujinx.Ava.UI.Applet
{
    internal class AvaHostUIHandler : IHostUIHandler
    {
        private readonly MainWindow _parent;

        public IHostUITheme HostUITheme { get; }

        public AvaHostUIHandler(MainWindow parent)
        {
            _parent = parent;

            HostUITheme = new AvaloniaHostUITheme(parent);
        }

        public bool DisplayMessageDialog(ControllerAppletUIArgs args)
        {
            ManualResetEvent dialogCloseEvent = new(false);

            bool okPressed = false;

            if (ConfigurationState.Instance.System.IgnoreControllerApplet)
                return false;

            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                UserResult response = await ControllerAppletDialog.ShowControllerAppletDialog(_parent, args);
                if (response == UserResult.Ok)
                {
                    okPressed = true;
                }

                dialogCloseEvent.Set();
            });

            dialogCloseEvent.WaitOne();

            return okPressed;
        }

        public bool DisplayMessageDialog(string title, string message)
        {
            ManualResetEvent dialogCloseEvent = new(false);

            bool okPressed = false;

            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                try
                {
                    ManualResetEvent deferEvent = new(false);

                    bool opened = false;

                    UserResult response = await ContentDialogHelper.ShowDeferredContentDialog(_parent,
                        title,
                        message,
                        string.Empty,
                        LocaleManager.Instance[LocaleKeys.DialogOpenSettingsWindowLabel],
                        string.Empty,
                        LocaleManager.Instance[LocaleKeys.SettingsButtonClose],
                        (int)Symbol.Important,
                        deferEvent,
                        async window =>
                        {
                            if (opened)
                            {
                                return;
                            }

                            opened = true;

                            _parent.SettingsWindow =
                                new SettingsWindow(_parent.VirtualFileSystem, _parent.ContentManager);

                            await StyleableAppWindow.ShowAsync(_parent.SettingsWindow, window);

                            _parent.SettingsWindow = null;

                            opened = false;
                        });

                    if (response == UserResult.Ok)
                    {
                        okPressed = true;
                    }

                    dialogCloseEvent.Set();
                }
                catch (Exception ex)
                {
                    await ContentDialogHelper.CreateErrorDialog(
                        LocaleManager.Instance.UpdateAndGetDynamicValue(
                            LocaleKeys.DialogMessageDialogErrorExceptionMessage, ex));

                    dialogCloseEvent.Set();
                }
            });

            dialogCloseEvent.WaitOne();

            return okPressed;
        }

        public bool DisplayInputDialog(SoftwareKeyboardUIArgs args, out string userText)
        {
            ManualResetEvent dialogCloseEvent = new(false);

            bool okPressed = false;
            bool error = false;
            string inputText = args.InitialText ?? string.Empty;

            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                try
                {
                    _parent.ViewModel.AppHost.NpadManager.BlockInputUpdates();
                    (UserResult result, string userInput) =
                        await SwkbdAppletDialog.ShowInputDialog(LocaleManager.Instance[LocaleKeys.SoftwareKeyboard],
                            args);

                    if (result == UserResult.Ok)
                    {
                        inputText = userInput;
                        okPressed = true;
                    }
                }
                catch (Exception ex)
                {
                    error = true;

                    await ContentDialogHelper.CreateErrorDialog(
                        LocaleManager.Instance.UpdateAndGetDynamicValue(
                            LocaleKeys.DialogSoftwareKeyboardErrorExceptionMessage, ex));
                }
                finally
                {
                    dialogCloseEvent.Set();
                }
            });

            dialogCloseEvent.WaitOne();
            _parent.ViewModel.AppHost.NpadManager.UnblockInputUpdates();

            userText = error ? null : inputText;

            return error || okPressed;
        }

        public bool DisplayCabinetDialog(out string userText)
        {
            ManualResetEvent dialogCloseEvent = new(false);
            bool okPressed = false;
            string inputText = "My Amiibo";
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                try
                {
                    _parent.ViewModel.AppHost.NpadManager.BlockInputUpdates();
                    SoftwareKeyboardUIArgs args = new();
                    args.KeyboardMode = KeyboardMode.Default;
                    args.InitialText = "Ryujinx";
                    args.StringLengthMin = 1;
                    args.StringLengthMax = 25;
                    (UserResult result, string userInput) =
                        await SwkbdAppletDialog.ShowInputDialog(LocaleManager.Instance[LocaleKeys.CabinetDialog], args);
                    if (result == UserResult.Ok)
                    {
                        inputText = userInput;
                        okPressed = true;
                    }
                }
                finally
                {
                    dialogCloseEvent.Set();
                }
            });
            dialogCloseEvent.WaitOne();
            _parent.ViewModel.AppHost.NpadManager.UnblockInputUpdates();
            userText = inputText;
            return okPressed;
        }

        public void DisplayCabinetMessageDialog()
        {
            ManualResetEvent dialogCloseEvent = new(false);
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                dialogCloseEvent.Set();
                await ContentDialogHelper.CreateInfoDialog(
                    LocaleManager.Instance[LocaleKeys.CabinetScanDialog],
                    string.Empty,
                    LocaleManager.Instance[LocaleKeys.InputDialogOk],
                    string.Empty,
                    LocaleManager.Instance[LocaleKeys.CabinetTitle]
                );
            });
            dialogCloseEvent.WaitOne();
        }


        public void ExecuteProgram(Switch device, ProgramSpecifyKind kind, ulong value)
        {
            device.Configuration.UserChannelPersistence.ExecuteProgram(kind, value);
            _parent.ViewModel.AppHost?.Stop();
        }

        public bool DisplayErrorAppletDialog(string title, string message, string[] buttons,
            (uint Module, uint Description)? errorCode = null)
        {
            ManualResetEvent dialogCloseEvent = new(false);

            bool showDetails = false;

            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                try
                {
                    ErrorAppletWindow msgDialog = new(_parent, buttons, message)
                    {
                        Title = title, WindowStartupLocation = WindowStartupLocation.CenterScreen, Width = 400
                    };

                    object response = await msgDialog.Run();

                    if (response != null && buttons is { Length: > 1 } && (int)response != buttons.Length - 1)
                    {
                        showDetails = true;
                    }

                    dialogCloseEvent.Set();

                    msgDialog.Close();
                }
                catch (Exception ex)
                {
                    dialogCloseEvent.Set();

                    await ContentDialogHelper.CreateErrorDialog(
                        LocaleManager.Instance.UpdateAndGetDynamicValue(
                            LocaleKeys.DialogErrorAppletErrorExceptionMessage, ex));
                }
            });

            dialogCloseEvent.WaitOne();

            return showDetails;
        }

        public IDynamicTextInputHandler CreateDynamicTextInputHandler() => new AvaloniaDynamicTextInputHandler(_parent);

        public UserProfile ShowPlayerSelectDialog()
        {
            UserId selected = UserId.Null;
            byte[] defaultGuestImage = EmbeddedResources.Read("Ryujinx.HLE/HOS/Services/Account/Acc/GuestUserImage.jpg");
            UserProfile guest = new(new UserId("00000000000000000000000000000080"), "Guest", defaultGuestImage);

            ManualResetEvent dialogCloseEvent = new(false);

            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                ObservableCollection<BaseModel> profiles = [];
                NavigationDialogHost nav = new();

                _parent.AccountManager.GetAllUsers()
                    .OrderBy(x => x.Name)
                    .ForEach(profile => profiles.Add(new Models.UserProfile(profile, nav)));

                profiles.Add(new Models.UserProfile(guest, nav));
                ProfileSelectorDialogViewModel viewModel = new()
                {
                    Profiles = profiles, SelectedUserId = _parent.AccountManager.LastOpenedUser.UserId
                };
                (selected, _) = await ProfileSelectorDialog.ShowInputDialog(viewModel);

                dialogCloseEvent.Set();
            });

            dialogCloseEvent.WaitOne();

            UserProfile profile = _parent.AccountManager.LastOpenedUser;
            if (selected == guest.UserId)
            {
                profile = guest;
            }
            else if (selected == UserId.Null)
            {
                profile = null;
            }
            else
            {
                foreach (UserProfile p in _parent.AccountManager.GetAllUsers())
                {
                    if (p.UserId == selected)
                    {
                        profile = p;
                        break;
                    }
                }
            }

            return profile;
        }
    }
}

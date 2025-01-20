using Ryujinx.HLE.HOS.Applets;
using Ryujinx.HLE.HOS.Services.Account.Acc;
using Ryujinx.HLE.HOS.Services.Am.AppletOE.ApplicationProxyService.ApplicationProxy.Types;

namespace Ryujinx.HLE.UI
{
    public interface IHostUIHandler
    {
        /// <summary>
        /// Displays an Input Dialog box to the user and blocks until text is entered.
        /// </summary>
        /// <param name="userText">Text that the user entered. Set to `null` on internal errors</param>
        /// <returns>True when OK is pressed, False otherwise. Also returns True on internal errors</returns>
        bool DisplayInputDialog(SoftwareKeyboardUIArgs args, out string userText);

        /// <summary>
        /// Displays a Message Dialog box to the user and blocks until it is closed.
        /// </summary>
        /// <returns>True when OK is pressed, False otherwise.</returns>
        bool DisplayMessageDialog(string title, string message);

        /// <summary>
        /// Displays a Message Dialog box specific to Controller Applet and blocks until it is closed.
        /// </summary>
        /// <returns>True when OK is pressed, False otherwise.</returns>
        bool DisplayMessageDialog(ControllerAppletUIArgs args);

        /// <summary>
        /// Displays an Input Dialog box to the user so they can enter the Amiibo's new name
        /// </summary>
        /// <param name="userText">Text that the user entered. Set to `null` on internal errors</param>
        /// <returns>True when OK is pressed, False otherwise. Also returns True on internal errors</returns>
        bool DisplayCabinetDialog(out string userText);

        /// <summary>
        /// Displays a Message Dialog box to the user to notify them to scan the Amiibo.
        /// </summary>
        void DisplayCabinetMessageDialog();

        /// <summary>
        /// Tell the UI that we need to transition to another program.
        /// </summary>
        /// <param name="device">The device instance.</param>
        /// <param name="kind">The program kind.</param>
        /// <param name="value">The value associated to the <paramref name="kind"/>.</param>
        void ExecuteProgram(Switch device, ProgramSpecifyKind kind, ulong value);

        /// Displays a Message Dialog box specific to Error Applet and blocks until it is closed.
        /// </summary>
        /// <returns>False when OK is pressed, True when another button (Details) is pressed.</returns>
        bool DisplayErrorAppletDialog(string title, string message, string[] buttonsText);

        /// <summary>
        /// Creates a handler to process keyboard inputs into text strings.
        /// </summary>
        /// <returns>An instance of the text handler.</returns>
        IDynamicTextInputHandler CreateDynamicTextInputHandler();

        /// <summary>
        /// Gets fonts and colors used by the host.
        /// </summary>
        IHostUITheme HostUITheme { get; }
        
        
        /// <summary>
        /// Displays the player select dialog and returns the selected profile.
        /// </summary>
        UserProfile ShowPlayerSelectDialog();
    }
}

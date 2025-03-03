using Avalonia.Controls;
using System.Threading;

namespace Ryujinx.Ava.UI.Windows
{
    public partial class UpdateWaitWindow : StyleableWindow
    {
        public UpdateWaitWindow(string primaryText, string secondaryText, CancellationTokenSource cancellationToken) : this(primaryText, secondaryText)
        {
            SystemDecorations = SystemDecorations.Full;
            ShowInTaskbar = true;

            Closing += (_, _) => cancellationToken.Cancel();
        }

        public UpdateWaitWindow(string primaryText, string secondaryText) : this()
        {
            PrimaryText.Text = primaryText;
            SecondaryText.Text = secondaryText;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            SystemDecorations = SystemDecorations.BorderOnly;
            ShowInTaskbar = false;
        }

        public UpdateWaitWindow()
        {
            InitializeComponent();
        }
    }
}

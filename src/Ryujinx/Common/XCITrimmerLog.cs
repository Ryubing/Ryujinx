using Avalonia.Threading;
using Ryujinx.Ava.UI.ViewModels;

namespace Ryujinx.Ava.Common
{
    public static class XCITrimmerLog
    {
        internal class MainWindow : Ryujinx.Common.Logging.XCIFileTrimmerLog
        {
            private readonly MainWindowViewModel _viewModel;

            public MainWindow(MainWindowViewModel viewModel)
            {
                _viewModel = viewModel;
            }

            public override void Progress(long current, long total, string text, bool complete)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    _viewModel.StatusBarProgressMaximum = (int)(total);
                    _viewModel.StatusBarProgressValue = (int)(current);
                });
            }
        }
        
        internal class TrimmerWindow : Ryujinx.Common.Logging.XCIFileTrimmerLog
        {
            private readonly XciTrimmerViewModel _viewModel;

            public TrimmerWindow(XciTrimmerViewModel viewModel)
            {
                _viewModel = viewModel;
            }

            public override void Progress(long current, long total, string text, bool complete)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    _viewModel.SetProgress((int)(current), (int)(total));
                });
            }
        }
    }
}

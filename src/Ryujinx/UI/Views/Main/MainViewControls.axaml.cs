using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Ryujinx.Ava.Common;
using Ryujinx.Ava.UI.Controls;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Ava.UI.Windows;
using System;

namespace Ryujinx.Ava.UI.Views.Main
{
    public partial class MainViewControls : RyujinxControl<MainWindowViewModel>
    {
        public MainViewControls()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (VisualRoot is MainWindow window)
            {
                ViewModel = window.ViewModel;
            }
        }

        public void Sort_Checked(object sender, RoutedEventArgs args)
        {
            if (sender is RadioButton { Tag: string sortStrategy })
                ViewModel.Sort(Enum.Parse<ApplicationSort>(sortStrategy));
        }

        public void Order_Checked(object sender, RoutedEventArgs args)
        {
            if (sender is RadioButton { Tag: string sortOrder })
                ViewModel.Sort(sortOrder is not "Descending");
        }

        private void SearchBox_OnKeyUp(object sender, KeyEventArgs e)
        {
            ViewModel.SearchText = SearchBox.Text;
        }
    }
}

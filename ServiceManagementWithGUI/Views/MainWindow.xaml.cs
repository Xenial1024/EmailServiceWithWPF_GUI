using System.ComponentModel;
using System.Windows;
using MahApps.Metro.Controls;

namespace ServiceManagementWithGUI.Views
{
 public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            MainViewModel viewModel = new();
            this.DataContext = viewModel;
            this.Closing += MainWindow_Closing;
            viewModel.RequestClose += (s, e) => this.Close();
        }
        private async void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true; // Tymczasowe wstrzymanie zamykania

            var viewModel = (MainViewModel)DataContext;
            await viewModel.HandleClosingAsync();

            Application.Current.Shutdown();
        }
    }
}
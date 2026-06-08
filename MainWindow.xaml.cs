using System.Windows;
using vcv_etagere_remaster.Front.ViewModel;

namespace vcv_etagere_remaster
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            this.DataContext = _viewModel;
            this.Closed += MainWindow_Closed;
        }

        private void MainWindow_Closed(object? sender, System.EventArgs e)
        {
            _viewModel.Shutdown();
        }
    }
}
using Microsoft.Maui.Controls;
using WindowManager.ViewModels;

namespace WindowManager.Views
{
    public partial class MainWindow : ContentPage
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}

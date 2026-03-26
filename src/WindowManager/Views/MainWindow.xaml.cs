using System.Windows;
using WindowManager.ViewModels;

namespace WindowManager.Views
{
    /// <summary>
    /// Code-behind for the application's main window.
    /// Receives its view model via constructor injection.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Initialises a new instance of <see cref="MainWindow"/>.
        /// </summary>
        /// <param name="viewModel">The main view model provided by the DI container.</param>
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}

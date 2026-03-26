using Microsoft.Maui;
using Microsoft.Maui.Controls;
using WindowManager.Views;

namespace WindowManager
{
    public partial class App : Application
    {
        private readonly MainWindow _mainWindow;

        public App(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
        }

        protected override Window CreateWindow(IActivationState? activationState)
            => new Window(_mainWindow);
    }
}

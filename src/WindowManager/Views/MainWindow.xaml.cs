using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using WindowManager.ViewModels;

namespace WindowManager.Views
{
    public partial class MainWindow : ContentPage
    {
        private static readonly System.TimeSpan AutoRefreshInterval = System.TimeSpan.FromSeconds(2);

        private readonly MainViewModel _viewModel;
        private IDispatcherTimer? _pollingTimer;

        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            RefreshWindows();
            StartAutoPolling();
        }

        protected override void OnDisappearing()
        {
            StopAutoPolling();
            base.OnDisappearing();
        }

        private void OnRefreshWindowsClicked(object sender, System.EventArgs e)
        {
            RefreshWindows();
        }

        private void StartAutoPolling()
        {
            if (_pollingTimer != null)
            {
                return;
            }

            _pollingTimer = Dispatcher.CreateTimer();
            _pollingTimer.Interval = AutoRefreshInterval;
            _pollingTimer.Tick += OnPollingTimerTick;
            _pollingTimer.Start();
        }

        private void StopAutoPolling()
        {
            if (_pollingTimer == null)
            {
                return;
            }

            _pollingTimer.Stop();
            _pollingTimer.Tick -= OnPollingTimerTick;
            _pollingTimer = null;
        }

        private void OnPollingTimerTick(object? sender, System.EventArgs e)
        {
            RefreshWindows();
        }

        private void RefreshWindows()
        {
            _viewModel.RefreshTopLevelWindows();
        }
    }
}

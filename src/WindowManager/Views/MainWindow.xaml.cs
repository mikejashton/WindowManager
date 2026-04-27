using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using WindowManager.ViewModels;

namespace WindowManager.Views
{
    public partial class MainWindow : ContentPage
    {
        private static readonly System.TimeSpan AutoRefreshInterval = System.TimeSpan.FromSeconds(2);

        /// <summary>
        /// Width of the left sidebar in device-independent points.
        /// Must match the <c>ColumnDefinition Width="200"</c> in MainWindow.xaml.
        /// </summary>
        private const double SidebarWidth = 200.0;

        private readonly MainViewModel _viewModel;
        private IDispatcherTimer? _pollingTimer;

        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        // ── Page lifecycle ──────────────────────────────────────────────────

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Surface OS permission dialogs immediately on launch rather than
            // waiting for the first operation to fail silently.
            _viewModel.CheckPermissions();
            PushContentArea();
            RefreshWindows();
            StartAutoPolling();
        }

        protected override void OnDisappearing()
        {
            StopAutoPolling();
            base.OnDisappearing();
        }

        /// <summary>
        /// Re-pushes the content area whenever the page is resized so that
        /// any active managed window is repositioned to the new rect.
        /// </summary>
        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            PushContentArea();
        }

        // ── Content area ────────────────────────────────────────────────────

        /// <summary>
        /// Computes the screen-space rectangle of the right-hand content pane and
        /// forwards it to the view model.
        ///
        /// On Mac Catalyst, MAUI's <c>Window.X/Y/Width/Height</c> can return stale or
        /// zero values during the initial layout pass. We prefer reading the native
        /// <c>UIWindow.Frame</c> directly, falling back to MAUI's values if unavailable.
        ///
        /// Mac Catalyst coordinate note: <c>UIWindow.Frame</c> is reported in UIKit points.
        /// Apps targeting iPad idiom (UIDeviceFamily 2) run with the documented Mac Catalyst
        /// scale factor of 0.77, meaning 1 UIKit point = 0.77 macOS screen points. The macOS
        /// Accessibility API (AXPosition / AXSize) uses macOS screen points, so all UIKit
        /// values must be multiplied by this factor before being passed to the window service.
        /// <c>SidebarWidth</c> is in MAUI DIPs (= UIKit points) and must be scaled too.
        ///
        /// Title-bar note: the title-bar height is derived by subtracting the MAUI
        /// <c>ContentPage.Height</c> (the layout-resolved usable area) from the
        /// <c>UIWindow.Frame.Height</c>. This automatically produces zero when the title bar is
        /// removed, so no code change is needed if you later hide the window chrome.
        /// </summary>
        private void PushContentArea()
        {
            double screenX = 0, screenY = 0, windowWidth = 0, windowHeight = 0;
            double sidebarPts = SidebarWidth; // default: MAUI DIPs already match platform pts
            bool gotBounds = false;

#if MACCATALYST
            // Mac Catalyst iPad idiom scale: UIKit pt × 0.77 = macOS screen pt.
            const double CatalystScale = 0.77;
            if (Window?.Handler?.PlatformView is UIKit.UIWindow uiWin
                && uiWin.Frame.Width > 0)
            {
                var insets = uiWin.SafeAreaInsets;

                // Derive the title-bar height from the difference between the UIWindow
                // frame height and the MAUI ContentPage height (in UIKit pts = MAUI DIPs).
                // ContentPage.Height is set by the layout engine to the usable area below
                // the title bar, so:  titleBarH = frameH - pageH - bottomInset
                // When the title bar is absent, pageH ≈ frameH − bottomInset, giving titleBarH ≈ 0.
                // Fall back to SafeAreaInsets.Top only before the first layout pass (Height == 0).
                var pageH     = Height; // ContentPage.Height in MAUI DIPs = UIKit pts
                var titleBarH = pageH > 0
                    ? (double)uiWin.Frame.Height - pageH - (double)insets.Bottom
                    : (double)insets.Top;

                // Convert UIKit points → macOS screen points before handing off to the
                // Accessibility API via the window service.
                screenX      = (double)uiWin.Frame.X * CatalystScale;
                screenY      = ((double)uiWin.Frame.Y + titleBarH) * CatalystScale;
                windowWidth  = (double)uiWin.Frame.Width * CatalystScale;
                // Subtract the title bar and the bottom safe area (Dock) so the managed
                // window doesn't extend into either chrome region.
                windowHeight = ((double)uiWin.Frame.Height
                               - titleBarH
                               - (double)insets.Bottom) * CatalystScale;
                // SidebarWidth is in MAUI DIPs (= UIKit pts); convert to macOS pts.
                sidebarPts   = SidebarWidth * CatalystScale;
                gotBounds    = windowWidth > 0 && windowHeight > 0;
            }
#endif
            if (!gotBounds)
            {
                var win = Window;
                if (win == null) return;

                screenX      = win.X;
                screenY      = win.Y;
                // ContentPage.Width/Height are set during layout and are more
                // reliable than Window.Width/Height on first allocation.
                windowWidth  = win.Width  > 0 ? win.Width  : Width;
                windowHeight = win.Height > 0 ? win.Height : Height;
                // sidebarPts stays as SidebarWidth (MAUI DIPs match platform pts here)
            }

            var contentWidth  = windowWidth - sidebarPts;
            var contentHeight = windowHeight;

            if (contentWidth <= 0 || contentHeight <= 0) return;

            System.Diagnostics.Debug.WriteLine(
                $"[MainWindow] Content area → " +
                $"x={screenX + sidebarPts:F0} y={screenY:F0} " +
                $"w={contentWidth:F0} h={contentHeight:F0}");

            _viewModel.UpdateContentArea(screenX + sidebarPts, screenY, contentWidth, contentHeight);
        }

        // ── Refresh ─────────────────────────────────────────────────────────

        private void OnRefreshWindowsClicked(object sender, System.EventArgs e) => RefreshWindows();

        private void StartAutoPolling()
        {
            if (_pollingTimer != null) return;
            _pollingTimer = Dispatcher.CreateTimer();
            _pollingTimer.Interval = AutoRefreshInterval;
            _pollingTimer.Tick += OnPollingTimerTick;
            _pollingTimer.Start();
        }

        private void StopAutoPolling()
        {
            if (_pollingTimer == null) return;
            _pollingTimer.Stop();
            _pollingTimer.Tick -= OnPollingTimerTick;
            _pollingTimer = null;
        }

        private void OnPollingTimerTick(object? sender, System.EventArgs e) => RefreshWindows();

        private void RefreshWindows() => _viewModel.RefreshTopLevelWindows();
    }
}

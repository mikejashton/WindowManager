using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using WindowManager.Abstractions.Models;
using WindowManager.Abstractions.Services;
using WindowManager.Windows.Helpers;

namespace WindowManager.Windows.Services
{
    /// <summary>
    /// Windows implementation of <see cref="IWindowService"/> that uses Win32 P/Invoke.
    /// </summary>
    public class WindowsWindowService : IWindowService
    {
        private readonly int _currentPid = Process.GetCurrentProcess().Id;

        /// <inheritdoc/>
        public List<ManagedWindow> EnumerateWindows()
        {
            var windows = new List<ManagedWindow>();

            NativeMethods.EnumWindows((hWnd, _) =>
            {
                if (!NativeMethods.IsWindowVisible(hWnd)) return true;

                var len = NativeMethods.GetWindowTextLength(hWnd);
                if (len == 0) return true;

                var sb = new StringBuilder(len + 1);
                NativeMethods.GetWindowText(hWnd, sb, sb.Capacity);
                var title = sb.ToString();

                NativeMethods.GetWindowThreadProcessId(hWnd, out var pidUint);
                var pid = (int)pidUint;
                if (pid == _currentPid) return true;

                var processName = "Unknown";
                try { processName = Process.GetProcessById(pid).ProcessName; }
                catch { /* process may have exited */ }

                windows.Add(new ManagedWindow
                {
                    Handle      = hWnd,
                    Title       = title,
                    ProcessName = processName,
                    ProcessId   = pid
                });

                return true; // continue enumeration
            }, IntPtr.Zero);

            return windows
                .OrderBy(w => w.ProcessName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(w => w.Title,        StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <inheritdoc/>
        public void ShowWindow(ManagedWindow window)
        {
            NativeMethods.ShowWindow(window.Handle, NativeMethods.SW_RESTORE);
            NativeMethods.SetForegroundWindow(window.Handle);
        }

        /// <inheritdoc/>
        public void HideWindow(ManagedWindow window)
        {
            NativeMethods.ShowWindow(window.Handle, NativeMethods.SW_HIDE);
        }

        /// <inheritdoc/>
        public void PositionWindow(ManagedWindow window, double x, double y, double width, double height)
        {
            // Convert from device-independent logical pixels to physical pixels using the
            // target window's DPI so the window lands in exactly the right screen position.
            var dpi   = NativeMethods.GetDpiForWindow(window.Handle);
            var scale = dpi > 0 ? dpi / 96.0 : 1.0;

            var px = (int)Math.Round(x      * scale);
            var py = (int)Math.Round(y      * scale);
            var pw = (int)Math.Round(width  * scale);
            var ph = (int)Math.Round(height * scale);

            NativeMethods.ShowWindow(window.Handle, NativeMethods.SW_RESTORE);
            NativeMethods.SetWindowPos(
                window.Handle, IntPtr.Zero,
                px, py, pw, ph,
                NativeMethods.SWP_NOZORDER | NativeMethods.SWP_SHOWWINDOW);
            NativeMethods.SetForegroundWindow(window.Handle);
        }

        /// <inheritdoc/>
        /// <remarks>Windows does not require an explicit permission request for window management.</remarks>
        public void CheckPermissions() { /* no-op on Windows */ }

        /// <inheritdoc/>
        public bool IsWindowValid(ManagedWindow window) => NativeMethods.IsWindow(window.Handle);
    }
}

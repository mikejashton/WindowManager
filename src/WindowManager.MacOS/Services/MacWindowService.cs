using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Foundation;
using ObjCRuntime;
using WindowManager.Abstractions.Models;
using WindowManager.Abstractions.Services;

namespace WindowManager.MacOS.Services
{
    /// <summary>
    /// macOS (Mac Catalyst) implementation of <see cref="IWindowService"/>.
    /// Uses the macOS Accessibility API and CGWindowList to enumerate and manage windows.
    /// </summary>
    public class MacWindowService : IWindowService
    {
        private static readonly NSString WindowNumberKey = new("kCGWindowNumber");
        private static readonly NSString WindowLayerKey = new("kCGWindowLayer");
        private static readonly NSString WindowNameKey = new("kCGWindowName");
        private static readonly NSString WindowOwnerNameKey = new("kCGWindowOwnerName");

        private readonly HashSet<IntPtr> _knownHandles = new();
        private readonly string _currentProcessName;

        public MacWindowService()
        {
            _currentProcessName = Process.GetCurrentProcess().ProcessName;
        }

        /// <inheritdoc/>
        public List<ManagedWindow> EnumerateWindows()
        {
            var windows = new List<ManagedWindow>();
            // Rebuild the handle cache every pass so IsWindowValid reflects the latest snapshot.
            _knownHandles.Clear();

            // Take a single CoreGraphics snapshot of windows currently on screen and skip desktop elements.
            var listHandle = NativeMethods.CGWindowListCopyWindowInfo(
                NativeMethods.CGWindowListOption.OnScreenOnly | NativeMethods.CGWindowListOption.ExcludeDesktopElements,
                IntPtr.Zero);

            if (listHandle == IntPtr.Zero)
            {
                // If native snapshot fails, return an empty result rather than throwing in the UI path.
                return windows;
            }

            try
            {
                var windowDictionaries = NSArray.ArrayFromHandle<NSDictionary>(listHandle);

                foreach (var dictionary in windowDictionaries)
                {
                    var layerValue = (dictionary[WindowLayerKey] as NSNumber)?.Int32Value ?? -1;
                    if (layerValue != 0)
                    {
                        // Layer 0 corresponds to normal top-level app windows; skip overlays/system surfaces.
                        continue;
                    }

                    var title = (dictionary[WindowNameKey] as NSString)?.ToString() ?? string.Empty;

                    var windowNumber = (dictionary[WindowNumberKey] as NSNumber)?.Int32Value ?? 0;
                    if (windowNumber <= 0)
                    {
                        // Invalid/missing ids cannot be used as stable handles for later operations.
                        continue;
                    }

                    var ownerName = (dictionary[WindowOwnerNameKey] as NSString)?.ToString() ?? "Unknown";
                    // Skip windows owned by the window manager application itself.
                    if (string.Equals(ownerName, _currentProcessName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var handle = new IntPtr(windowNumber);
                    var displayTitle = string.IsNullOrWhiteSpace(title)
                        ? $"{ownerName} (Window {windowNumber})"
                        : title;

                    windows.Add(new ManagedWindow
                    {
                        Handle = handle,
                        Title = displayTitle,
                        ProcessName = ownerName
                    });

                    // Keep a fast lookup set for IsWindowValid checks between refreshes.
                    _knownHandles.Add(handle);
                }

                // Use deterministic ordering to minimize visual churn during polling refreshes.
                return windows
                    .OrderBy(w => w.ProcessName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(w => w.Title, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            finally
            {
                // Always release native objects returned by CoreGraphics snapshots.
                Runtime.GetNSObject(listHandle)?.Dispose();
            }
        }

        /// <inheritdoc/>
        public void ShowWindow(ManagedWindow window)
        {
            // Cross-application show/hide requires Accessibility APIs and user-granted permission.
            // For now, this method is intentionally non-throwing until that control path is added.
            _ = window;
        }

        /// <inheritdoc/>
        public void HideWindow(ManagedWindow window)
        {
            // Cross-application show/hide requires Accessibility APIs and user-granted permission.
            // For now, this method is intentionally non-throwing until that control path is added.
            _ = window;
        }

        /// <inheritdoc/>
        public void PositionWindowFullScreen(ManagedWindow window)
        {
            // Window geometry control requires Accessibility APIs and explicit permission.
            // Keep this method non-throwing so the rest of the app can enumerate windows.
            _ = window;
        }

        /// <inheritdoc/>
        public bool IsWindowValid(ManagedWindow window)
        {
            if (window.Handle == IntPtr.Zero)
            {
                return false;
            }

            if (_knownHandles.Count == 0)
            {
                _ = EnumerateWindows();
            }

            return _knownHandles.Contains(window.Handle);
        }

        private static class NativeMethods
        {
            [Flags]
            internal enum CGWindowListOption : uint
            {
                OnScreenOnly = 1u,
                ExcludeDesktopElements = 16u
            }

            [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
            internal static extern IntPtr CGWindowListCopyWindowInfo(CGWindowListOption option, IntPtr relativeToWindow);
        }
    }
}

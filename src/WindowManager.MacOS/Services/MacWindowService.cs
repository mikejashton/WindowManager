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
        // ── CGWindowList dictionary keys ─────────────────────────────────────
        private static readonly NSString WindowNumberKey    = new("kCGWindowNumber");
        private static readonly NSString WindowLayerKey     = new("kCGWindowLayer");
        private static readonly NSString WindowNameKey      = new("kCGWindowName");
        private static readonly NSString WindowOwnerNameKey = new("kCGWindowOwnerName");
        private static readonly NSString WindowOwnerPidKey  = new("kCGWindowOwnerPID");

        // ── CF Boolean singletons (NSNumber is NOT toll-free with CFBoolean) ─
        private static readonly Lazy<IntPtr> CfBooleanTrue =
            new(static () => ReadCFGlobal("kCFBooleanTrue"));
        private static readonly Lazy<IntPtr> CfBooleanFalse =
            new(static () => ReadCFGlobal("kCFBooleanFalse"));

        private static IntPtr ReadCFGlobal(string symbol)
        {
            try
            {
                var lib = NativeLibrary.Load(
                    "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation");
                return Marshal.ReadIntPtr(NativeLibrary.GetExport(lib, symbol));
            }
            catch { return IntPtr.Zero; }
        }

        // ── Main-thread dispatcher ───────────────────────────────────────────
        // AXUIElement is not thread-safe; every AX call must run on the macOS main thread.
        private static readonly NSObject MainThreadBridge = new();

        private static void RunOnMainThread(Action action)
        {
            // NSThread.IsMain guards against the deadlock that InvokeOnMainThread
            // would cause if called from the main thread itself.
            if (NSThread.IsMain)
                action();
            else
                MainThreadBridge.InvokeOnMainThread(action);
        }

        // ── Accessibility permission ─────────────────────────────────────────
        private bool _permissionCheckDone;

        /// <inheritdoc/>
        /// <remarks>
        /// Calls <c>AXIsProcessTrustedWithOptions</c> with <c>kAXTrustedCheckOptionPrompt=YES</c>
        /// the first time per session that the process is not trusted, opening the System Settings
        /// Accessibility dialog so the user can grant access before trying any operation.
        ///
        /// Also requests the Screen Recording permission required by <c>CGWindowListCreateImage</c>
        /// (available since macOS 10.15). If not yet granted the system consent dialog is shown.
        ///
        /// <b>Development note</b>: macOS ties the Accessibility permission entry to the binary's
        /// code-signing hash. Every debug rebuild produces a new hash, making the previously
        /// granted entry stale. After each rebuild you must open System Settings →
        /// Privacy &amp; Security → Accessibility, remove the old entry, and enable the new one.
        /// </remarks>
        public void CheckPermissions()
        {
            CheckAccessibilityPermission();
            CheckScreenCapturePermission();
        }

        private void CheckAccessibilityPermission()
        {
            if (NativeMethods.AXIsProcessTrusted())
            {
                Debug.WriteLine("[MacWindowService] Accessibility permission: GRANTED ✓");
                return;
            }

            Debug.WriteLine(
                "[MacWindowService] Accessibility permission: NOT GRANTED. " +
                "Showing system dialog — enable the app in System Settings → " +
                "Privacy & Security → Accessibility, then restart.");

            // Show the system permission dialog exactly once per session.
            if (_permissionCheckDone) return;
            _permissionCheckDone = true;

            using var key   = new NSString("AXTrustedCheckOptionPrompt");
            using var value = NSNumber.FromBoolean(true);
            using var opts  = NSDictionary.FromObjectAndKey(value, key);
            NativeMethods.AXIsProcessTrustedWithOptions(opts.Handle);
        }

        private void CheckScreenCapturePermission()
        {
            if (NativeMethods.CGPreflightScreenCaptureAccess())
            {
                Debug.WriteLine("[MacWindowService] Screen Recording permission: GRANTED ✓");
                return;
            }

            Debug.WriteLine(
                "[MacWindowService] Screen Recording permission: NOT GRANTED. " +
                "Requesting access — enable the app in System Settings → " +
                "Privacy & Security → Screen Recording.");
            NativeMethods.CGRequestScreenCaptureAccess();
        }

        /// <summary>Returns <c>true</c> if the process currently has Accessibility permission.</summary>
        private static bool EnsureAccessibilityTrusted()
        {
            if (NativeMethods.AXIsProcessTrusted()) return true;
            Debug.WriteLine(
                "[MacWindowService] AX operation skipped — Accessibility permission not granted. " +
                "Call CheckPermissions() at startup and grant access in System Settings.");
            return false;
        }

        // ── Fields ───────────────────────────────────────────────────────────
        private readonly HashSet<IntPtr> _knownHandles = new();
        private readonly string _currentProcessName;

        public MacWindowService()
        {
            _currentProcessName = Process.GetCurrentProcess().ProcessName;
        }

        // ── IWindowService ───────────────────────────────────────────────────

        /// <inheritdoc/>
        public List<ManagedWindow> EnumerateWindows()
        {
            var windows = new List<ManagedWindow>();
            _knownHandles.Clear();

            var listHandle = NativeMethods.CGWindowListCopyWindowInfo(
                NativeMethods.CGWindowListOption.OnScreenOnly |
                NativeMethods.CGWindowListOption.ExcludeDesktopElements,
                IntPtr.Zero);

            if (listHandle == IntPtr.Zero) return windows;

            try
            {
                foreach (var dict in NSArray.ArrayFromHandle<NSDictionary>(listHandle))
                {
                    var layer = (dict[WindowLayerKey] as NSNumber)?.Int32Value ?? -1;
                    if (layer != 0) continue;

                    var windowNumber = (dict[WindowNumberKey] as NSNumber)?.Int32Value ?? 0;
                    if (windowNumber <= 0) continue;

                    var ownerName = (dict[WindowOwnerNameKey] as NSString)?.ToString() ?? "Unknown";
                    if (string.Equals(ownerName, _currentProcessName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var pid    = (dict[WindowOwnerPidKey] as NSNumber)?.Int32Value ?? 0;
                    var title  = (dict[WindowNameKey] as NSString)?.ToString() ?? string.Empty;
                    var handle = new IntPtr(windowNumber);

                    windows.Add(new ManagedWindow
                    {
                        Handle      = handle,
                        Title       = string.IsNullOrWhiteSpace(title)
                                          ? $"{ownerName} (Window {windowNumber})"
                                          : title,
                        ProcessName = ownerName,
                        ProcessId   = pid
                    });

                    _knownHandles.Add(handle);
                }

                return windows
                    .OrderBy(w => w.ProcessName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(w => w.Title,        StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            finally
            {
                Runtime.GetNSObject(listHandle)?.Dispose();
            }
        }

        /// <inheritdoc/>
        public void ShowWindow(ManagedWindow window)
        {
            if (!EnsureAccessibilityTrusted() || window.ProcessId <= 0) return;

            RunOnMainThread(() =>
            {
                var appEl = NativeMethods.AXUIElementCreateApplication(window.ProcessId);
                if (appEl == IntPtr.Zero) return;

                try
                {
                    WithAxWindow(appEl, window.Title, winEl =>
                    {
                        using var minAttr   = new NSString("AXMinimized");
                        using var frontAttr = new NSString("AXFrontmost");
                        NativeMethods.AXUIElementSetAttributeValue(
                            winEl, minAttr.Handle,   CfBooleanFalse.Value);
                        NativeMethods.AXUIElementSetAttributeValue(
                            appEl, frontAttr.Handle, CfBooleanTrue.Value);
                    });
                }
                finally { NativeMethods.CFRelease(appEl); }
            });
        }

        /// <inheritdoc/>
        public void HideWindow(ManagedWindow window)
        {
            if (!EnsureAccessibilityTrusted() || window.ProcessId <= 0) return;

            RunOnMainThread(() =>
            {
                var appEl = NativeMethods.AXUIElementCreateApplication(window.ProcessId);
                if (appEl == IntPtr.Zero) return;

                try
                {
                    WithAxWindow(appEl, window.Title, winEl =>
                    {
                        using var minAttr = new NSString("AXMinimized");
                        NativeMethods.AXUIElementSetAttributeValue(
                            winEl, minAttr.Handle, CfBooleanTrue.Value);
                    });
                }
                finally { NativeMethods.CFRelease(appEl); }
            });
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Both CGWindowList and the AX API use top-left screen coordinates (logical points),
        /// matching MAUI's <c>Window.X</c> / <c>Window.Y</c> on Mac Catalyst — no conversion needed.
        /// </remarks>
        public void PositionWindow(ManagedWindow window, double x, double y, double width, double height)
        {
            if (!EnsureAccessibilityTrusted() || window.ProcessId <= 0) return;

            Debug.WriteLine(
                $"[MacWindowService] PositionWindow '{window.Title}' (pid={window.ProcessId}) " +
                $"→ ({x:F0}, {y:F0}) {width:F0}×{height:F0}");

            RunOnMainThread(() =>
            {
                var appEl = NativeMethods.AXUIElementCreateApplication(window.ProcessId);
                if (appEl == IntPtr.Zero) return;

                try
                {
                    // Disable AXEnhancedUserInterface while repositioning.
                    // Electron, Firefox and LibreOffice set this attribute, which hands window layout
                    // to their internal engine and causes AXPosition / AXSize writes to be silently
                    // ignored. We must disable it, apply the frame, then restore the original value.
                    bool hadEnhancedUI = DisableEnhancedUIIfNeeded(appEl);

                    try
                    {
                        WithAxWindow(appEl, window.Title, winEl =>
                            ApplyWindowFrame(winEl, x, y, width, height));
                    }
                    finally
                    {
                        if (hadEnhancedUI)
                        {
                            using var attr = new NSString("AXEnhancedUserInterface");
                            NativeMethods.AXUIElementSetAttributeValue(
                                appEl, attr.Handle, CfBooleanTrue.Value);
                        }
                    }
                }
                finally { NativeMethods.CFRelease(appEl); }
            });
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Uses a broader CGWindowList query that includes minimised (off-screen) windows so that
        /// a workspace window that was hidden via <see cref="HideWindow"/> is not falsely reported
        /// as invalid. The on-screen-only <see cref="_knownHandles"/> cache cannot be used here
        /// because it excludes minimised windows, which would cause the workspace to lose its
        /// window assignment the moment the user switches away.
        /// </remarks>
        public bool IsWindowValid(ManagedWindow window)
        {
            if (window.Handle == IntPtr.Zero) return false;

            // Query ALL windows (including minimised/off-screen) — no OnScreenOnly flag.
            var listHandle = NativeMethods.CGWindowListCopyWindowInfo(
                NativeMethods.CGWindowListOption.ExcludeDesktopElements,
                IntPtr.Zero);

            if (listHandle == IntPtr.Zero) return false;

            try
            {
                foreach (var dict in NSArray.ArrayFromHandle<NSDictionary>(listHandle))
                {
                    var windowNumber = (dict[WindowNumberKey] as NSNumber)?.Int32Value ?? 0;
                    if (windowNumber > 0 && new IntPtr(windowNumber) == window.Handle)
                        return true;
                }

                return false;
            }
            finally
            {
                Runtime.GetNSObject(listHandle)?.Dispose();
            }
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Uses <c>CGWindowListCreateImage</c> with <c>kCGWindowListOptionIncludingWindow</c>
        /// to capture a composited snapshot of the specified window. The result is encoded as
        /// JPEG (quality 0.7) using the ImageIO framework and returned as a byte array.
        ///
        /// <b>Permission note</b>: macOS 10.15+ requires the Screen Recording permission.
        /// <see cref="CheckPermissions"/> requests this at startup; if not yet granted
        /// <c>CGWindowListCreateImage</c> will return a transparent/black image and this method
        /// returns <c>null</c>.
        /// </remarks>
        public byte[]? CaptureScreenshot(ManagedWindow window)
        {
            if (window.Handle == IntPtr.Zero) return null;

            // CGWindowID is a uint; the handle was stored as IntPtr(windowNumber).
            var windowID = (uint)window.Handle.ToInt32();

            var cgImage = NativeMethods.CGWindowListCreateImage(
                NativeMethods.CGRect.Infinite,
                NativeMethods.CGWindowListOption.IncludingWindow,
                windowID,
                NativeMethods.kCGWindowImageBoundsIgnoreFraming);

            if (cgImage == IntPtr.Zero)
            {
                Debug.WriteLine($"[MacWindowService] CaptureScreenshot: CGWindowListCreateImage returned null for window {windowID}");
                return null;
            }

            try
            {
                return EncodeImageAsJpeg(cgImage);
            }
            finally
            {
                NativeMethods.CFRelease(cgImage);
            }
        }

        // ── Private helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Finds the best-matching AX window for <paramref name="appElement"/> and invokes
        /// <paramref name="action"/> with its element pointer.
        /// <para>
        /// Both <c>AXWindows</c> (visible) and <c>AXMinimizedWindows</c> (minimised in the Dock)
        /// are searched so that the action works correctly regardless of whether the window was
        /// hidden by minimising it. Both CF arrays are kept retained for the entire duration of
        /// <paramref name="action"/> so that the unowned element pointers remain valid.
        /// </para>
        /// </summary>
        private static void WithAxWindow(IntPtr appElement, string title, Action<IntPtr> action)
        {
            // Fetch both lists up-front and keep them alive until after action() returns.
            using var visibleKey   = new NSString("AXWindows");
            using var minimisedKey = new NSString("AXMinimizedWindows");

            NativeMethods.AXUIElementCopyAttributeValue(
                appElement, visibleKey.Handle,   out var visibleArr);
            NativeMethods.AXUIElementCopyAttributeValue(
                appElement, minimisedKey.Handle, out var minimisedArr);

            try
            {
                // Exact title match wins; prefer visible windows over minimised ones.
                var exact = FindExactInArray(visibleArr,   title)
                         ?? FindExactInArray(minimisedArr, title);

                // Fall back to the first window in either list when no exact match exists.
                var fallback = FirstInArray(visibleArr) ?? FirstInArray(minimisedArr);

                var chosen = exact ?? fallback;
                if (chosen is not null)
                {
                    Debug.WriteLine(
                        $"[MacWindowService] WithAxWindow: found window " +
                        $"(exact={exact is not null}) for '{title}'");
                    action(chosen.Value);
                }
                else
                {
                    Debug.WriteLine(
                        $"[MacWindowService] WithAxWindow: no AX window found for '{title}'");
                }
            }
            finally
            {
                if (visibleArr   != IntPtr.Zero) NativeMethods.CFRelease(visibleArr);
                if (minimisedArr != IntPtr.Zero) NativeMethods.CFRelease(minimisedArr);
            }
        }

        /// <summary>
        /// Returns the first element in <paramref name="arrRef"/>, or <c>null</c> if the array
        /// is empty or <see cref="IntPtr.Zero"/>.
        /// </summary>
        private static IntPtr? FirstInArray(IntPtr arrRef)
        {
            if (arrRef == IntPtr.Zero) return null;
            var count = NativeMethods.CFArrayGetCount(arrRef);
            if (count <= 0) return null;
            var first = NativeMethods.CFArrayGetValueAtIndex(arrRef, 0);
            return first != IntPtr.Zero ? first : null;
        }

        /// <summary>
        /// Searches <paramref name="arrRef"/> for an AX window whose <c>AXTitle</c> equals
        /// <paramref name="title"/> (ordinal) and returns its element pointer, or <c>null</c>.
        /// </summary>
        private static IntPtr? FindExactInArray(IntPtr arrRef, string title)
        {
            if (arrRef == IntPtr.Zero) return null;
            var count = NativeMethods.CFArrayGetCount(arrRef);

            for (nint i = 0; i < count; i++)
            {
                var candidate = NativeMethods.CFArrayGetValueAtIndex(arrRef, i);
                if (candidate == IntPtr.Zero) continue;

                using var titleAttr = new NSString("AXTitle");
                if (NativeMethods.AXUIElementCopyAttributeValue(
                        candidate, titleAttr.Handle, out var titleRef) != 0
                    || titleRef == IntPtr.Zero)
                    continue;

                var axTitle = Runtime.GetNSObject(titleRef)?.ToString();
                NativeMethods.CFRelease(titleRef);

                if (string.Equals(axTitle, title, StringComparison.Ordinal))
                    return candidate;
            }

            return null;
        }

        /// <summary>
        /// Applies position and size in the order <b>Size → Position → Size</b>.
        /// The repeated size call corrects the drift introduced by Electron and Gecko-based
        /// apps that apply the first size relative to the old position.
        /// </summary>
        private static void ApplyWindowFrame(IntPtr winEl, double x, double y, double w, double h)
        {
            if (winEl == IntPtr.Zero) return;

            using var posAttr  = new NSString("AXPosition");
            using var sizeAttr = new NSString("AXSize");

            SetAXSize(winEl,     sizeAttr.Handle, w, h);  // 1. Size first
            SetAXPosition(winEl, posAttr.Handle,  x, y);  // 2. Position
            SetAXSize(winEl,     sizeAttr.Handle, w, h);  // 3. Size again — fixes Electron/Gecko drift
        }

        private static void SetAXPosition(IntPtr element, IntPtr attr, double x, double y)
        {
            var point = new NativeMethods.CGPoint { X = x, Y = y };
            var val   = NativeMethods.AXValueCreate(NativeMethods.kAXValueCGPointType, ref point);
            if (val == IntPtr.Zero) return;
            NativeMethods.AXUIElementSetAttributeValue(element, attr, val);
            NativeMethods.CFRelease(val);
        }

        private static void SetAXSize(IntPtr element, IntPtr attr, double width, double height)
        {
            var size = new NativeMethods.CGSize { Width = width, Height = height };
            var val  = NativeMethods.AXValueCreateSize(NativeMethods.kAXValueCGSizeType, ref size);
            if (val == IntPtr.Zero) return;
            NativeMethods.AXUIElementSetAttributeValue(element, attr, val);
            NativeMethods.CFRelease(val);
        }

        /// <summary>
        /// Reads <c>AXEnhancedUserInterface</c> from the app element.
        /// If it is <c>true</c>, sets it to <c>false</c> and returns <c>true</c>
        /// so the caller can restore it afterwards.
        /// </summary>
        private static bool DisableEnhancedUIIfNeeded(IntPtr appElement)
        {
            using var attr = new NSString("AXEnhancedUserInterface");

            if (NativeMethods.AXUIElementCopyAttributeValue(
                    appElement, attr.Handle, out var cur) != 0 || cur == IntPtr.Zero)
                return false;

            bool wasEnabled = cur == CfBooleanTrue.Value;
            NativeMethods.CFRelease(cur);

            if (!wasEnabled) return false;

            NativeMethods.AXUIElementSetAttributeValue(appElement, attr.Handle, CfBooleanFalse.Value);
            return true;
        }

        /// <summary>
        /// Encodes a <c>CGImageRef</c> as a JPEG byte array (quality 0.7) using the ImageIO
        /// framework. Returns <c>null</c> if the encoding fails.
        /// </summary>
        private static byte[]? EncodeImageAsJpeg(IntPtr cgImage)
        {
            var cfData = NativeMethods.CFDataCreateMutable(IntPtr.Zero, 0);
            if (cfData == IntPtr.Zero) return null;

            try
            {
                using var uti  = new NSString("public.jpeg");
                var dest = NativeMethods.CGImageDestinationCreateWithData(
                    cfData, uti.Handle, 1, IntPtr.Zero);

                if (dest == IntPtr.Zero) return null;

                try
                {
                    NativeMethods.CGImageDestinationAddImage(dest, cgImage, IntPtr.Zero);
                    if (!NativeMethods.CGImageDestinationFinalize(dest)) return null;

                    int    length = (int)NativeMethods.CFDataGetLength(cfData);
                    IntPtr ptr    = NativeMethods.CFDataGetBytePtr(cfData);
                    if (length <= 0 || ptr == IntPtr.Zero) return null;

                    var result = new byte[length];
                    Marshal.Copy(ptr, result, 0, length);
                    return result;
                }
                finally
                {
                    NativeMethods.CFRelease(dest);
                }
            }
            finally
            {
                NativeMethods.CFRelease(cfData);
            }
        }

        // ── P/Invoke declarations ────────────────────────────────────────────

        private static class NativeMethods
        {
            // CGWindowList ────────────────────────────────────────────────────

            [Flags]
            internal enum CGWindowListOption : uint
            {
                OnScreenOnly           = 1u,
                IncludingWindow        = 8u,
                ExcludeDesktopElements = 16u
            }

            [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
            internal static extern IntPtr CGWindowListCopyWindowInfo(
                CGWindowListOption option, IntPtr relativeToWindow);

            /// <summary>
            /// Captures a composited image of the specified windows.
            /// Pass <see cref="CGRect.Infinite"/> to let the system use each window's own bounds.
            /// Pass <see cref="CGWindowListOption.IncludingWindow"/> with a specific window ID to
            /// capture that single window.
            /// </summary>
            [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
            internal static extern IntPtr CGWindowListCreateImage(
                CGRect screenBounds,
                CGWindowListOption listOption,
                uint windowID,
                int imageOption); // CGWindowImageOption: 0 = default, 1 = bounds ignore framing

            // Accessibility ───────────────────────────────────────────────────

            [DllImport("/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices")]
            internal static extern IntPtr AXUIElementCreateApplication(int pid);

            [DllImport("/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices")]
            internal static extern int AXUIElementCopyAttributeValue(
                IntPtr element, IntPtr attribute, out IntPtr value);

            [DllImport("/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices")]
            internal static extern int AXUIElementSetAttributeValue(
                IntPtr element, IntPtr attribute, IntPtr value);

            [DllImport("/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices")]
            internal static extern IntPtr AXValueCreate(int type, ref CGPoint value);

            // EntryPoint alias so we can pass CGSize to the same underlying symbol.
            [DllImport("/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices",
                       EntryPoint = "AXValueCreate")]
            internal static extern IntPtr AXValueCreateSize(int type, ref CGSize value);

            /// <summary>Returns whether this process has been granted Accessibility permission.</summary>
            [DllImport("/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool AXIsProcessTrusted();

            /// <summary>
            /// Returns whether this process is trusted; when <paramref name="options"/> contains
            /// <c>kAXTrustedCheckOptionPrompt = YES</c>, shows the system permission dialog.
            /// </summary>
            [DllImport("/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool AXIsProcessTrustedWithOptions(IntPtr options);

            // Screen-capture permission (macOS 10.15+) ────────────────────────

            /// <summary>
            /// Returns whether the current process already has Screen Recording permission.
            /// Does NOT show a system dialog.
            /// </summary>
            [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CGPreflightScreenCaptureAccess();

            /// <summary>
            /// Requests Screen Recording permission. On first call this triggers the system
            /// consent dialog; subsequent calls while the request is pending are no-ops.
            /// Returns <c>true</c> if access is already granted.
            /// </summary>
            [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CGRequestScreenCaptureAccess();

            // ImageIO ─────────────────────────────────────────────────────────

            /// <summary>Creates an image destination that writes to a mutable CF data buffer.</summary>
            [DllImport("/System/Library/Frameworks/ImageIO.framework/ImageIO")]
            internal static extern IntPtr CGImageDestinationCreateWithData(
                IntPtr data,        // CFMutableDataRef
                IntPtr type,        // CFStringRef UTI  (e.g. "public.jpeg")
                nint   count,       // number of images (1)
                IntPtr options);    // CFDictionaryRef, pass IntPtr.Zero

            /// <summary>Appends a CGImage to an image destination.</summary>
            [DllImport("/System/Library/Frameworks/ImageIO.framework/ImageIO")]
            internal static extern void CGImageDestinationAddImage(
                IntPtr dest,        // CGImageDestinationRef
                IntPtr image,       // CGImageRef
                IntPtr properties); // CFDictionaryRef, pass IntPtr.Zero

            /// <summary>Finalises the image destination and writes the encoded bytes to the data buffer.</summary>
            [DllImport("/System/Library/Frameworks/ImageIO.framework/ImageIO")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CGImageDestinationFinalize(IntPtr dest);

            // CoreFoundation ──────────────────────────────────────────────────

            [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
            internal static extern void CFRelease(IntPtr handle);

            [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
            internal static extern nint CFArrayGetCount(IntPtr theArray);

            [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
            internal static extern IntPtr CFArrayGetValueAtIndex(IntPtr theArray, nint idx);

            /// <summary>Creates a new mutable CF data object with the given initial capacity.</summary>
            [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
            internal static extern IntPtr CFDataCreateMutable(IntPtr allocator, nint capacity);

            /// <summary>Returns the number of bytes in a CF data object.</summary>
            [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
            internal static extern nint CFDataGetLength(IntPtr theData);

            /// <summary>Returns a read-only pointer to the bytes of a CF data object.</summary>
            [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
            internal static extern IntPtr CFDataGetBytePtr(IntPtr theData);

            // AX value type constants ─────────────────────────────────────────
            internal const int kAXValueCGPointType = 1;
            internal const int kAXValueCGSizeType  = 2;

            // CGWindowImageOption constants
            internal const int kCGWindowImageDefault            = 0;
            internal const int kCGWindowImageBoundsIgnoreFraming = 1;

            // Structs ─────────────────────────────────────────────────────────

            [StructLayout(LayoutKind.Sequential)]
            internal struct CGPoint { public double X; public double Y; }

            [StructLayout(LayoutKind.Sequential)]
            internal struct CGSize { public double Width; public double Height; }

            [StructLayout(LayoutKind.Sequential)]
            internal struct CGRect
            {
                public double X;
                public double Y;
                public double Width;
                public double Height;

                /// <summary>
                /// Equivalent to CoreGraphics <c>CGRectInfinite</c>: a rect large enough to
                /// encompass every window when passed to <c>CGWindowListCreateImage</c>.
                /// </summary>
                internal static CGRect Infinite => new CGRect
                {
                    X      = -double.MaxValue / 2,
                    Y      = -double.MaxValue / 2,
                    Width  = double.MaxValue,
                    Height = double.MaxValue
                };
            }
        }
    }
}

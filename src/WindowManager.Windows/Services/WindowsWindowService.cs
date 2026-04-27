using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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

        /// <inheritdoc/>
        /// <remarks>
        /// Uses <c>PrintWindow</c> with <c>PW_RENDERFULLCONTENT</c> so that the content of
        /// DWM-composited, DirectX, and UWP windows is captured correctly even when the window
        /// is partially off-screen or occluded. The result is scaled down to a 300-px-wide
        /// thumbnail and encoded as a 24-bit BMP byte array.
        /// </remarks>
        public byte[]? CaptureScreenshot(ManagedWindow window)
        {
            var hWnd = window.Handle;
            if (!NativeMethods.IsWindow(hWnd)) return null;
            if (!NativeMethods.GetWindowRect(hWnd, out var rect)) return null;

            int srcW = rect.Right  - rect.Left;
            int srcH = rect.Bottom - rect.Top;
            if (srcW <= 0 || srcH <= 0) return null;

            // Scale to a thumbnail of at most 300 px wide, preserving aspect ratio.
            const int MaxThumbWidth = 300;
            double scale = srcW > MaxThumbWidth ? (double)MaxThumbWidth / srcW : 1.0;
            int thumbW = Math.Max(1, (int)(srcW * scale));
            int thumbH = Math.Max(1, (int)(srcH * scale));

            // Obtain the screen DC used as a reference for creating compatible objects.
            var hdcScreen = NativeMethods.GetDC(IntPtr.Zero);
            if (hdcScreen == IntPtr.Zero) return null;

            // Source DC: capture the full window via PrintWindow.
            var hdcSrc  = NativeMethods.CreateCompatibleDC(hdcScreen);
            var hbmSrc  = NativeMethods.CreateCompatibleBitmap(hdcScreen, srcW, srcH);
            var oldSrc  = NativeMethods.SelectObject(hdcSrc, hbmSrc);

            // Pre-fill with white so transparent regions appear on a white background.
            var whiteBrush = NativeMethods.GetStockObject(NativeMethods.WHITE_BRUSH);
            var fillRect   = new NativeMethods.RECT { Left = 0, Top = 0, Right = srcW, Bottom = srcH };
            NativeMethods.FillRect(hdcSrc, ref fillRect, whiteBrush);

            bool captured = NativeMethods.PrintWindow(hWnd, hdcSrc, NativeMethods.PW_RENDERFULLCONTENT);

            byte[]? result = null;

            if (captured)
            {
                // Thumbnail DC: scale the full capture down.
                var hdcThumb = NativeMethods.CreateCompatibleDC(hdcScreen);
                var hbmThumb = NativeMethods.CreateCompatibleBitmap(hdcScreen, thumbW, thumbH);
                var oldThumb = NativeMethods.SelectObject(hdcThumb, hbmThumb);

                NativeMethods.SetStretchBltMode(hdcThumb, NativeMethods.HALFTONE);
                NativeMethods.SetBrushOrgEx(hdcThumb, 0, 0, out _);
                NativeMethods.StretchBlt(
                    hdcThumb, 0, 0, thumbW, thumbH,
                    hdcSrc,  0, 0, srcW,   srcH,
                    NativeMethods.SRCCOPY);

                // Read pixel data as top-down 24-bit BGR DIB.
                var bmi = new NativeMethods.BITMAPINFOHEADER
                {
                    biSize        = Marshal.SizeOf<NativeMethods.BITMAPINFOHEADER>(),
                    biWidth       = thumbW,
                    biHeight      = -thumbH, // negative → top-down row order
                    biPlanes      = 1,
                    biBitCount    = 24,
                    biCompression = 0,       // BI_RGB
                };

                // Row stride for 24-bit DIBs must be a multiple of 4 bytes.
                int rowStride  = (thumbW * 3 + 3) & ~3;
                int pixelBytes = rowStride * thumbH;
                var pixels     = new byte[pixelBytes];
                NativeMethods.GetDIBits(hdcThumb, hbmThumb, 0, (uint)thumbH, pixels, ref bmi, 0);

                result = EncodeBmp24(thumbW, thumbH, rowStride, pixels);

                NativeMethods.SelectObject(hdcThumb, oldThumb);
                NativeMethods.DeleteObject(hbmThumb);
                NativeMethods.DeleteDC(hdcThumb);
            }

            // Clean up source DC resources.
            NativeMethods.SelectObject(hdcSrc, oldSrc);
            NativeMethods.DeleteObject(hbmSrc);
            NativeMethods.DeleteDC(hdcSrc);
            NativeMethods.ReleaseDC(IntPtr.Zero, hdcScreen);

            return result;
        }

        // ── Private helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Encodes a top-down 24-bit BGR DIB pixel buffer as a BMP file byte array.
        /// The BITMAPFILEHEADER and BITMAPINFOHEADER are written with <c>BI_RGB</c> compression.
        /// </summary>
        private static byte[] EncodeBmp24(int width, int height, int rowStride, byte[] pixels)
        {
            const int FileHeaderSize = 14;
            const int DibHeaderSize  = 40;
            int pixelDataSize = rowStride * height;
            int totalSize     = FileHeaderSize + DibHeaderSize + pixelDataSize;

            var result = new byte[totalSize];
            int i = 0;

            // BITMAPFILEHEADER
            result[i++] = 0x42; // 'B'
            result[i++] = 0x4D; // 'M'
            WriteInt32(result, ref i, totalSize);
            WriteInt16(result, ref i, 0);                          // bfReserved1
            WriteInt16(result, ref i, 0);                          // bfReserved2
            WriteInt32(result, ref i, FileHeaderSize + DibHeaderSize); // bfOffBits

            // BITMAPINFOHEADER
            WriteInt32(result, ref i, DibHeaderSize);
            WriteInt32(result, ref i, width);
            WriteInt32(result, ref i, -height); // negative = top-down
            WriteInt16(result, ref i, 1);       // biPlanes
            WriteInt16(result, ref i, 24);      // biBitCount
            WriteInt32(result, ref i, 0);       // biCompression = BI_RGB
            WriteInt32(result, ref i, pixelDataSize);
            WriteInt32(result, ref i, 2835);    // biXPelsPerMeter ≈ 72 dpi
            WriteInt32(result, ref i, 2835);    // biYPelsPerMeter ≈ 72 dpi
            WriteInt32(result, ref i, 0);       // biClrUsed
            WriteInt32(result, ref i, 0);       // biClrImportant

            // Pixel data
            Array.Copy(pixels, 0, result, i, pixelDataSize);

            return result;
        }

        private static void WriteInt32(byte[] buf, ref int offset, int value)
        {
            buf[offset++] = (byte)(value);
            buf[offset++] = (byte)(value >> 8);
            buf[offset++] = (byte)(value >> 16);
            buf[offset++] = (byte)(value >> 24);
        }

        private static void WriteInt16(byte[] buf, ref int offset, short value)
        {
            buf[offset++] = (byte)(value);
            buf[offset++] = (byte)(value >> 8);
        }
    }
}

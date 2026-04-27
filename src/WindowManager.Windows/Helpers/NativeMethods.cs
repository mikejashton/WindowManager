using System;
using System.Runtime.InteropServices;

namespace WindowManager.Windows.Helpers
{
    /// <summary>
    /// P/Invoke declarations for the Windows API (user32.dll, gdi32.dll, kernel32.dll).
    /// All Win32 interop is centralised here; no other class should contain DllImport attributes.
    /// </summary>
    public static class NativeMethods
    {
        /// <summary>Delegate used as the callback for <see cref="EnumWindows"/>.</summary>
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        /// <summary>
        /// Enumerates all top-level windows on the screen by passing the handle of each window,
        /// in turn, to an application-defined callback function.
        /// </summary>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        /// <summary>Copies the text of the specified window's title bar into a buffer.</summary>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        /// <summary>Retrieves the length of the text of the specified window's title bar.</summary>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        /// <summary>Determines whether the specified window is visible.</summary>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        /// <summary>Determines whether the specified window handle identifies an existing window.</summary>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindow(IntPtr hWnd);

        /// <summary>Sets the specified window's show state.</summary>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        /// <summary>Changes the size, position, and Z order of a child, pop-up, or top-level window.</summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        /// <summary>Puts the thread that created the specified window into the foreground and activates the window.</summary>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        /// <summary>
        /// Retrieves the identifier of the thread that created the specified window and the
        /// identifier of the process that created the window.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        /// <summary>Retrieves the DPI for the specified window.</summary>
        [DllImport("user32.dll")]
        public static extern uint GetDpiForWindow(IntPtr hWnd);

        /// <summary>Retrieves the dimensions of the bounding rectangle of the specified window.</summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        /// <summary>
        /// Copies the visual content of a window into the specified device context.
        /// Use <see cref="PW_RENDERFULLCONTENT"/> to capture DirectX / UWP windows.
        /// </summary>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);

        /// <summary>Fills a rectangle using the specified brush, used to pre-fill the capture DC with a white background.</summary>
        [DllImport("user32.dll")]
        public static extern int FillRect(IntPtr hDC, ref RECT lprc, IntPtr hbr);

        // ── GDI ──────────────────────────────────────────────────────────────

        /// <summary>Retrieves a handle to a device context (DC) for the specified window or, when NULL, the entire screen.</summary>
        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        /// <summary>Creates a memory device context (DC) compatible with the specified device.</summary>
        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);

        /// <summary>Releases a DC obtained via <see cref="GetDC"/>.</summary>
        [DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        /// <summary>Creates a bitmap compatible with the device associated with the specified DC.</summary>
        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        /// <summary>Selects an object into a DC, returning the previously selected object.</summary>
        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

        /// <summary>Deletes a logical GDI object, freeing all resources associated with it.</summary>
        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject(IntPtr ho);

        /// <summary>Deletes a memory DC created with <see cref="CreateCompatibleDC"/>.</summary>
        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteDC(IntPtr hdc);

        /// <summary>
        /// Copies a bitmap from a source DC into a destination DC, scaling and stretching it.
        /// </summary>
        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool StretchBlt(
            IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest,
            IntPtr hdcSrc,  int xSrc,  int ySrc,  int wSrc,  int hSrc,
            uint   dwRop);

        /// <summary>Sets the bitmap stretching mode in the specified device context.</summary>
        [DllImport("gdi32.dll")]
        public static extern int SetStretchBltMode(IntPtr hdc, int mode);

        /// <summary>Sets the brush origin for the specified DC (required before HALFTONE stretching).</summary>
        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetBrushOrgEx(IntPtr hdc, int x, int y, out POINT lppt);

        /// <summary>Retrieves the bits of the specified compatible bitmap and copies them into a buffer as a DIB.</summary>
        [DllImport("gdi32.dll")]
        public static extern int GetDIBits(
            IntPtr hdc, IntPtr hbm,
            uint start, uint cLines,
            byte[]? lpvBits, ref BITMAPINFOHEADER lpbmi,
            uint usage);

        /// <summary>Retrieves a handle to one of the stock objects (pens, brushes, fonts, or palettes).</summary>
        [DllImport("gdi32.dll")]
        public static extern IntPtr GetStockObject(int fnObject);

        // ShowWindow nCmdShow constants
        public const int SW_HIDE    = 0;
        public const int SW_RESTORE = 9;
        public const int SW_SHOW    = 5;

        // SetWindowPos flags
        public const uint SWP_NOZORDER    = 0x0004;
        public const uint SWP_NOACTIVATE  = 0x0010;
        public const uint SWP_SHOWWINDOW  = 0x0040;

        // PrintWindow flags
        public const uint PW_RENDERFULLCONTENT = 0x00000002;

        // StretchBlt raster operations
        public const uint SRCCOPY = 0x00CC0020;

        // SetStretchBltMode modes
        public const int HALFTONE = 4;

        // GetStockObject constants
        public const int WHITE_BRUSH = 0;

        // ── Structs ───────────────────────────────────────────────────────────

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        /// <summary>
        /// Describes the dimensions and colour format of a device-independent bitmap (DIB).
        /// Matches the Win32 BITMAPINFOHEADER structure.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAPINFOHEADER
        {
            public int    biSize;
            public int    biWidth;
            public int    biHeight;
            public short  biPlanes;
            public short  biBitCount;
            public int    biCompression;
            public int    biSizeImage;
            public int    biXPelsPerMeter;
            public int    biYPelsPerMeter;
            public int    biClrUsed;
            public int    biClrImportant;
        }
    }
}

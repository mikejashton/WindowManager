using System;
using System.Runtime.InteropServices;

namespace WindowManager.Helpers
{
    /// <summary>
    /// P/Invoke declarations for the Windows API (user32.dll, kernel32.dll).
    /// All Win32 interop is centralised here; no other class should contain DllImport attributes.
    /// </summary>
    public static class NativeMethods
    {
        // TODO: Add additional Win32 API declarations here as needed.

        /// <summary>Delegate used as the callback for <see cref="EnumWindows"/>.</summary>
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        /// <summary>
        /// Enumerates all top-level windows on the screen by passing the handle of each window,
        /// in turn, to an application-defined callback function.
        /// </summary>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        /// <summary>
        /// Copies the text of the specified window's title bar into a buffer.
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        /// <summary>
        /// Retrieves the length of the text of the specified window's title bar.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        /// <summary>
        /// Determines whether the specified window is visible.
        /// </summary>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        /// <summary>
        /// Determines whether the specified window handle identifies an existing window.
        /// </summary>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindow(IntPtr hWnd);

        /// <summary>
        /// Sets the specified window's show state.
        /// </summary>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        /// <summary>
        /// Changes the size, position, and Z order of a child, pop-up, or top-level window.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        /// <summary>
        /// Puts the thread that created the specified window into the foreground and activates the window.
        /// </summary>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        /// <summary>
        /// Retrieves the identifier of the thread that created the specified window and the
        /// identifier of the process that created the window.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        // ShowWindow nCmdShow constants
        public const int SW_HIDE = 0;
        public const int SW_SHOW = 5;
        public const int SW_RESTORE = 9;

        // SetWindowPos flags
        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_SHOWWINDOW = 0x0040;
    }
}

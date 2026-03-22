using System;
using System.Collections.Generic;
using WindowManager.Models;

namespace WindowManager.Services
{
    /// <summary>
    /// Service for interacting with the Windows API to enumerate and manipulate application windows.
    /// </summary>
    public class WindowService
    {
        /// <summary>
        /// Returns a list of all currently visible top-level windows, excluding system and invisible windows.
        /// </summary>
        // TODO: Implement window enumeration using NativeMethods.EnumWindows
        public List<ManagedWindow> EnumerateWindows()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Makes the specified window visible and restores it if it was hidden.
        /// </summary>
        /// <param name="handle">The Win32 window handle (HWND) to show.</param>
        // TODO: Implement using NativeMethods.ShowWindow(handle, SW_SHOW)
        public void ShowWindow(IntPtr handle)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Hides the specified window without closing or suspending the owning process.
        /// </summary>
        /// <param name="handle">The Win32 window handle (HWND) to hide.</param>
        // TODO: Implement using NativeMethods.ShowWindow(handle, SW_HIDE)
        public void HideWindow(IntPtr handle)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Repositions and resizes the specified window to fill the working area of the primary screen,
        /// offset by the sidebar width so it does not overlap the application's own UI.
        /// </summary>
        /// <param name="handle">The Win32 window handle (HWND) to position.</param>
        // TODO: Implement using NativeMethods.SetWindowPos with the calculated content-area rectangle
        public void PositionWindowFullScreen(IntPtr handle)
        {
            throw new NotImplementedException();
        }
    }
}

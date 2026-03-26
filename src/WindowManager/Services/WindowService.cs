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
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Makes the specified window visible and restores it if it was hidden.
        /// </summary>
        /// <param name="window">The managed window to show.</param>
        // TODO: Implement using NativeMethods.ShowWindow(window.Handle, SW_SHOW)
        public void ShowWindow(ManagedWindow window)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Hides the specified window without closing or suspending the owning process.
        /// </summary>
        /// <param name="window">The managed window to hide.</param>
        // TODO: Implement using NativeMethods.ShowWindow(window.Handle, SW_HIDE)
        public void HideWindow(ManagedWindow window)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Repositions and resizes the specified window to fill the working area of the primary screen,
        /// offset by the sidebar width so it does not overlap the application's own UI.
        /// </summary>
        /// <param name="window">The managed window to position.</param>
        // TODO: Implement using NativeMethods.SetWindowPos with the calculated content-area rectangle
        public void PositionWindowFullScreen(ManagedWindow window)
        {
            throw new System.NotImplementedException();
        }
    }
}

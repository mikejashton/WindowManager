using System.Collections.Generic;
using WindowManager.Abstractions.Models;

namespace WindowManager.Abstractions.Services
{
    /// <summary>
    /// Abstraction over OS-level APIs for enumerating and manipulating application windows.
    /// </summary>
    public interface IWindowService
    {
        /// <summary>Returns all currently visible top-level windows, excluding system and invisible windows.</summary>
        List<ManagedWindow> EnumerateWindows();

        /// <summary>Makes the specified window visible and restores it if it was hidden.</summary>
        void ShowWindow(ManagedWindow window);

        /// <summary>Hides the specified window without closing or suspending its process.</summary>
        void HideWindow(ManagedWindow window);

        /// <summary>
        /// Repositions and resizes the specified window to fill the given screen-space rectangle.
        /// Coordinates use a top-left origin in logical (device-independent) screen points.
        /// </summary>
        /// <param name="window">The window to move and resize.</param>
        /// <param name="x">Left edge of the target area in screen coordinates.</param>
        /// <param name="y">Top edge of the target area in screen coordinates.</param>
        /// <param name="width">Width of the target area.</param>
        /// <param name="height">Height of the target area.</param>
        void PositionWindow(ManagedWindow window, double x, double y, double width, double height);

        /// <summary>Returns true if the window handle still refers to a live window.</summary>
        bool IsWindowValid(ManagedWindow window);

        /// <summary>
        /// Checks whether the required OS permissions are in place and, on the first call
        /// when they are not, triggers the platform permission request flow.
        /// Should be called once at application startup.
        /// </summary>
        void CheckPermissions();
    }
}

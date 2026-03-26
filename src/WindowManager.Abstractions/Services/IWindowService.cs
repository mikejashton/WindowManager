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
        /// Repositions and resizes the specified window to fill the primary screen's working area,
        /// offset so it does not overlap the application's own sidebar.
        /// </summary>
        void PositionWindowFullScreen(ManagedWindow window);

        /// <summary>Returns true if the window handle still refers to a live window.</summary>
        bool IsWindowValid(ManagedWindow window);
    }
}

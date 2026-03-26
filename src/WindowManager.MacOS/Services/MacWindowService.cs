using System.Collections.Generic;
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
        /// <inheritdoc/>
        // TODO: Implement using CGWindowListCopyWindowInfo / NSWorkspace
        public List<ManagedWindow> EnumerateWindows()
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        // TODO: Implement using the Accessibility API (AXUIElement) to show the window
        public void ShowWindow(ManagedWindow window)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        // TODO: Implement using the Accessibility API (AXUIElement) to hide the window
        public void HideWindow(ManagedWindow window)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        // TODO: Implement using AXUIElement to resize and reposition the window
        public void PositionWindowFullScreen(ManagedWindow window)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        // TODO: Validate by checking if the CGWindowID is still active in CGWindowListCopyWindowInfo
        public bool IsWindowValid(ManagedWindow window)
        {
            throw new System.NotImplementedException();
        }
    }
}

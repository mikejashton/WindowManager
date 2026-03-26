using System.Collections.Generic;
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
        /// <inheritdoc/>
        // TODO: Implement using NativeMethods.EnumWindows
        public List<ManagedWindow> EnumerateWindows()
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        // TODO: Implement using NativeMethods.ShowWindow(window.Handle, SW_SHOW)
        public void ShowWindow(ManagedWindow window)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        // TODO: Implement using NativeMethods.ShowWindow(window.Handle, SW_HIDE)
        public void HideWindow(ManagedWindow window)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        // TODO: Implement using NativeMethods.SetWindowPos with the calculated content-area rectangle
        public void PositionWindowFullScreen(ManagedWindow window)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        public bool IsWindowValid(ManagedWindow window) => NativeMethods.IsWindow(window.Handle);
    }
}

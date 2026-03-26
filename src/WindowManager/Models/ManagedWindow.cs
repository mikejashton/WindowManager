using System;

namespace WindowManager.Models
{
    /// <summary>
    /// Represents a window being managed by the application.
    /// </summary>
    public class ManagedWindow
    {
        /// <summary>Gets or sets the Win32 window handle (HWND).</summary>
        public IntPtr Handle { get; set; }

        /// <summary>Gets or sets the window title at the time it was captured.</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Gets or sets the name of the process that owns the window.</summary>
        public string ProcessName { get; set; } = string.Empty;

        /// <summary>
        /// Determines whether the window handle is still valid (i.e. the window has not been closed).
        /// </summary>
        /// <returns><c>true</c> if the handle refers to a live window; otherwise <c>false</c>.</returns>
        // TODO: Implement validation logic
        public bool IsValid()
        {
            // Check if window handle is still valid
            throw new NotImplementedException();
        }
    }
}

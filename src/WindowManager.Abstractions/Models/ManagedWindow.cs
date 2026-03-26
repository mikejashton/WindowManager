using System;

namespace WindowManager.Abstractions.Models
{
    /// <summary>
    /// Represents a window being managed by the application.
    /// </summary>
    public class ManagedWindow
    {
        /// <summary>Gets or sets the platform window handle.</summary>
        public IntPtr Handle { get; set; }

        /// <summary>Gets or sets the window title at the time it was captured.</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Gets or sets the name of the process that owns the window.</summary>
        public string ProcessName { get; set; } = string.Empty;
    }
}

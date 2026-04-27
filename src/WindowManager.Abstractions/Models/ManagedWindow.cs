using System;
using System.ComponentModel;

namespace WindowManager.Abstractions.Models
{
    /// <summary>
    /// Represents a window being managed by the application.
    /// </summary>
    public class ManagedWindow : INotifyPropertyChanged
    {
        private byte[]? _screenshot;

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Gets or sets the platform window handle.</summary>
        public IntPtr Handle { get; set; }

        /// <summary>Gets or sets the window title at the time it was captured.</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Gets or sets the name of the process that owns the window.</summary>
        public string ProcessName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the OS process identifier of the window owner.
        /// Used on macOS to address the Accessibility API for positioning.
        /// </summary>
        public int ProcessId { get; set; }

        /// <summary>
        /// Gets or sets the most recently captured screenshot of this window as raw image bytes
        /// (BMP on Windows, JPEG on macOS). <c>null</c> when no screenshot has been captured yet.
        /// </summary>
        public byte[]? Screenshot
        {
            get => _screenshot;
            set
            {
                if (ReferenceEquals(_screenshot, value)) return;
                _screenshot = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Screenshot)));
            }
        }
    }
}

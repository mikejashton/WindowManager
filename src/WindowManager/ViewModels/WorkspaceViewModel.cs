using System.ComponentModel;
using WindowManager.Models;

namespace WindowManager.ViewModels
{
    /// <summary>
    /// View model for a single workspace tab.
    /// Wraps a <see cref="Workspace"/> model and exposes it for data binding.
    /// </summary>
    public class WorkspaceViewModel : INotifyPropertyChanged
    {
        private Workspace _workspace;

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Gets or sets the underlying workspace model.</summary>
        public Workspace Workspace
        {
            get => _workspace;
            set
            {
                _workspace = value;
                OnPropertyChanged(nameof(Workspace));
            }
        }

        /// <summary>
        /// Initialises a new instance of <see cref="WorkspaceViewModel"/>.
        /// </summary>
        /// <param name="workspace">The workspace model to wrap.</param>
        public WorkspaceViewModel(Workspace workspace)
        {
            _workspace = workspace;
        }

        /// <summary>Raises the <see cref="PropertyChanged"/> event.</summary>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

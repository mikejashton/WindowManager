using System.Collections.ObjectModel;
using System.ComponentModel;
using WindowManager.Models;
using WindowManager.Services;

namespace WindowManager.ViewModels
{
    /// <summary>
    /// Main view model for the application.
    /// Owns the workspace collection and tracks the currently active workspace.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly WorkspaceManager _workspaceManager;
        private Workspace? _activeWorkspace;

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Gets the observable collection of workspaces bound to the sidebar list.</summary>
        public ObservableCollection<Workspace> Workspaces => _workspaceManager.Workspaces;

        /// <summary>Gets or sets the currently active workspace.</summary>
        public Workspace? ActiveWorkspace
        {
            get => _activeWorkspace;
            set
            {
                if (_activeWorkspace == value) return;
                _activeWorkspace = value;
                OnPropertyChanged(nameof(ActiveWorkspace));
            }
        }

        /// <summary>
        /// Initialises a new instance of <see cref="MainViewModel"/>.
        /// </summary>
        /// <param name="workspaceManager">The workspace manager service.</param>
        public MainViewModel(WorkspaceManager workspaceManager)
        {
            _workspaceManager = workspaceManager;
        }

        // TODO: Implement commands for CreateWorkspace, SelectWindow, SwitchWorkspace

        /// <summary>Raises the <see cref="PropertyChanged"/> event.</summary>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

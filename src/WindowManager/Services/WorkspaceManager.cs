using System;
using System.Collections.ObjectModel;
using WindowManager.Abstractions.Models;
using WindowManager.Abstractions.Services;

namespace WindowManager.Services
{
    /// <summary>
    /// Manages the lifecycle of workspace tabs and coordinates show/hide transitions
    /// when the active workspace changes.
    /// </summary>
    public class WorkspaceManager
    {
        private readonly IWindowService _windowService;
        private Workspace? _activeWorkspace;

        /// <summary>Gets the collection of workspaces; changes are reflected in the UI via data binding.</summary>
        public ObservableCollection<Workspace> Workspaces { get; }

        /// <summary>
        /// Initialises a new instance of <see cref="WorkspaceManager"/>.
        /// </summary>
        /// <param name="windowService">The platform window service used to show/hide managed windows.</param>
        public WorkspaceManager(IWindowService windowService)
        {
            _windowService = windowService;
            Workspaces = new ObservableCollection<Workspace>();
        }

        /// <summary>
        /// Creates a new workspace with the given name and adds it to the collection.
        /// </summary>
        public Workspace CreateWorkspace(string name)
        {
            var trimmedName = (name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmedName))
            {
                trimmedName = $"Workspace {Workspaces.Count + 1}";
            }

            var workspace = new Workspace
            {
                Name = trimmedName
            };

            Workspaces.Add(workspace);
            return workspace;
        }

        /// <summary>
        /// Switches the active workspace: hides the window of the current workspace (if any)
        /// and shows the window of the target workspace (if any).
        /// </summary>
        public void SwitchToWorkspace(Workspace workspace)
        {
            if (_activeWorkspace == workspace)
            {
                return;
            }

            if (_activeWorkspace?.Window is { } previousWindow)
            {
                if (_windowService.IsWindowValid(previousWindow))
                {
                    _windowService.HideWindow(previousWindow);
                }
                else
                {
                    _activeWorkspace.Window = null;
                }
            }

            if (workspace.Window is { } nextWindow)
            {
                if (_windowService.IsWindowValid(nextWindow))
                {
                    _windowService.ShowWindow(nextWindow);
                    _windowService.PositionWindowFullScreen(nextWindow);
                }
                else
                {
                    workspace.Window = null;
                }
            }

            _activeWorkspace = workspace;
        }
    }
}

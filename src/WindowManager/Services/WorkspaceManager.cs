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
        // TODO: Implement workspace creation (create Workspace, add to Workspaces)
        public void CreateWorkspace(string name)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Switches the active workspace: hides the window of the current workspace (if any)
        /// and shows the window of the target workspace (if any).
        /// </summary>
        // TODO: Implement show/hide transitions via _windowService
        public void SwitchToWorkspace(Workspace workspace)
        {
            throw new System.NotImplementedException();
        }
    }
}

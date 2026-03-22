using System.Collections.ObjectModel;
using WindowManager.Models;

namespace WindowManager.Services
{
    /// <summary>
    /// Manages the lifecycle of workspace tabs and coordinates show/hide transitions
    /// when the active workspace changes.
    /// </summary>
    public class WorkspaceManager
    {
        private readonly WindowService _windowService;

        /// <summary>Gets the collection of workspaces; changes are reflected in the UI via data binding.</summary>
        public ObservableCollection<Workspace> Workspaces { get; }

        /// <summary>
        /// Initialises a new instance of <see cref="WorkspaceManager"/>.
        /// </summary>
        /// <param name="windowService">The window service used to show/hide managed windows.</param>
        public WorkspaceManager(WindowService windowService)
        {
            _windowService = windowService;
            Workspaces = new ObservableCollection<Workspace>();
        }

        /// <summary>
        /// Creates a new workspace with the given name and adds it to the collection.
        /// </summary>
        /// <param name="name">The user-defined name for the workspace.</param>
        // TODO: Implement workspace creation (create Workspace, add to Workspaces)
        public void CreateWorkspace(string name)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Switches the active workspace: hides the window of the current workspace (if any)
        /// and shows the window of the target workspace (if any).
        /// </summary>
        /// <param name="workspace">The workspace to make active.</param>
        // TODO: Implement show/hide transitions via _windowService
        public void SwitchToWorkspace(Workspace workspace)
        {
            throw new System.NotImplementedException();
        }
    }
}

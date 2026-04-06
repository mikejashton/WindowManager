using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using WindowManager.Abstractions.Models;
using WindowManager.Abstractions.Services;
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
        private readonly IWindowService _windowService;
        private Workspace? _activeWorkspace;
        private ManagedWindow? _selectedTopLevelWindow;

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Gets the observable collection of workspaces bound to the sidebar list.</summary>
        public ObservableCollection<Workspace> Workspaces => _workspaceManager.Workspaces;

        /// <summary>Gets the currently enumerated top-level windows displayed in the main pane.</summary>
        public ObservableCollection<ManagedWindow> TopLevelWindows { get; } = new();

        /// <summary>Gets the command that creates and selects a new workspace.</summary>
        public ICommand CreateWorkspaceCommand { get; }

        /// <summary>Gets or sets the currently active workspace.</summary>
        public Workspace? ActiveWorkspace
        {
            get => _activeWorkspace;
            set
            {
                if (_activeWorkspace == value) return;

                if (value != null)
                {
                    _workspaceManager.SwitchToWorkspace(value);
                }

                _activeWorkspace = value;
                OnPropertyChanged(nameof(ActiveWorkspace));
                OnPropertyChanged(nameof(ShouldShowWindowList));
                OnPropertyChanged(nameof(ShowWindowListPlaceholder));
                OnPropertyChanged(nameof(WindowListPlaceholderText));

                RefreshTopLevelWindows();
            }
        }

        /// <summary>
        /// Gets or sets the selected top-level window from the window list.
        /// Selecting one assigns it to the active workspace.
        /// </summary>
        public ManagedWindow? SelectedTopLevelWindow
        {
            get => _selectedTopLevelWindow;
            set
            {
                if (_selectedTopLevelWindow == value)
                {
                    return;
                }

                _selectedTopLevelWindow = value;
                OnPropertyChanged(nameof(SelectedTopLevelWindow));

                if (value != null)
                {
                    SelectWindowForActiveWorkspace(value);
                }
            }
        }

        /// <summary>True when the window picker list should be visible.</summary>
        public bool ShouldShowWindowList => ActiveWorkspace != null && ActiveWorkspace.Window == null;

        /// <summary>True when the right pane should show instructional/placeholder text.</summary>
        public bool ShowWindowListPlaceholder => !ShouldShowWindowList;

        /// <summary>Gets the current placeholder message shown in the right pane.</summary>
        public string WindowListPlaceholderText
        {
            get
            {
                if (ActiveWorkspace == null)
                {
                    return "Create and select a workspace to choose an application window.";
                }

                return $"Workspace '{ActiveWorkspace.Name}' already has a selected window.";
            }
        }

        /// <summary>
        /// Initialises a new instance of <see cref="MainViewModel"/>.
        /// </summary>
        /// <param name="workspaceManager">The workspace manager service.</param>
        /// <param name="windowService">The platform window service used to enumerate visible windows.</param>
        public MainViewModel(WorkspaceManager workspaceManager, IWindowService windowService)
        {
            _workspaceManager = workspaceManager;
            _windowService = windowService;
            CreateWorkspaceCommand = new Command(CreateWorkspace);
        }

        /// <summary>
        /// Creates a workspace and makes it active.
        /// </summary>
        public void CreateWorkspace()
        {
            var workspace = _workspaceManager.CreateWorkspace(string.Empty);
            ActiveWorkspace = workspace;
        }

        /// <summary>
        /// Assigns a selected window to the active workspace and immediately positions it.
        /// </summary>
        public void SelectWindowForActiveWorkspace(ManagedWindow window)
        {
            if (ActiveWorkspace == null)
            {
                return;
            }

            ActiveWorkspace.Window = window;
            _windowService.ShowWindow(window);
            _windowService.PositionWindowFullScreen(window);

            TopLevelWindows.Clear();
            _selectedTopLevelWindow = null;
            OnPropertyChanged(nameof(SelectedTopLevelWindow));
            OnPropertyChanged(nameof(ShouldShowWindowList));
            OnPropertyChanged(nameof(ShowWindowListPlaceholder));
            OnPropertyChanged(nameof(WindowListPlaceholderText));
        }

        /// <summary>
        /// Refreshes the list of currently visible top-level windows.
        /// </summary>
        public void RefreshTopLevelWindows()
        {
            if (!ShouldShowWindowList)
            {
                TopLevelWindows.Clear();
                return;
            }

            var latestWindows = _windowService.EnumerateWindows();
            var latestByHandle = new Dictionary<nint, ManagedWindow>();

            foreach (var window in latestWindows)
            {
                if (window.Handle == nint.Zero)
                {
                    continue;
                }

                // Keep first occurrence if the native snapshot returns duplicate handles.
                if (!latestByHandle.ContainsKey(window.Handle))
                {
                    latestByHandle[window.Handle] = window;
                }
            }

            // Remove windows that no longer exist.
            for (var i = TopLevelWindows.Count - 1; i >= 0; i--)
            {
                if (!latestByHandle.ContainsKey(TopLevelWindows[i].Handle))
                {
                    TopLevelWindows.RemoveAt(i);
                }
            }

            // Keep positions for unchanged items; only replace changed data in-place.
            for (var i = 0; i < TopLevelWindows.Count; i++)
            {
                var current = TopLevelWindows[i];
                if (!latestByHandle.TryGetValue(current.Handle, out var latest))
                {
                    continue;
                }

                if (!string.Equals(current.Title, latest.Title, StringComparison.Ordinal) ||
                    !string.Equals(current.ProcessName, latest.ProcessName, StringComparison.Ordinal))
                {
                    TopLevelWindows[i] = latest;
                }

                latestByHandle.Remove(current.Handle);
            }

            // Append only truly new windows; existing items keep their previous indices.
            foreach (var window in latestWindows)
            {
                if (latestByHandle.Remove(window.Handle))
                {
                    TopLevelWindows.Add(window);
                }
            }
        }

        /// <summary>Raises the <see cref="PropertyChanged"/> event.</summary>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

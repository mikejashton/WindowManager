# Features

## MVP Features (v1.0)

### Feature 1 — Single Window per Tab, Full-Screen

**Description**  
Each workspace tab manages exactly one application window. When a workspace is active the managed window is resized and repositioned to fill the entire content area (screen working area minus the sidebar).

**Acceptance Criteria**
- [ ] Managed window covers the full content area when its tab is active.
- [ ] Window is resized correctly when the application window is resized.
- [ ] Managed window is not behind the taskbar or the sidebar.

---

### Feature 2 — Left Sidebar with User-Created Tabs

**Description**  
A 200 px fixed-width sidebar on the left shows all workspaces as clickable tab buttons. Users can create as many tabs as they need.

**Acceptance Criteria**
- [ ] "**+ New Workspace**" button creates a tab after prompting for a name.
- [ ] Workspace name is displayed on the tab button.
- [ ] Active tab is visually distinct from inactive tabs.
- [ ] Tabs are scrollable when there are more than fit in the viewport.

---

### Feature 3 — Select Existing Windows to Manage

**Description**  
A window-selection dialog enumerates all currently open, visible top-level windows and lets the user pick one to attach to the active workspace.

**Acceptance Criteria**
- [ ] Dialog lists all visible top-level windows with title and process name.
- [ ] System/invisible windows are filtered out.
- [ ] Double-clicking or selecting + clicking OK assigns the window to the workspace.
- [ ] The assigned window is immediately repositioned to the content area.

---

### Feature 4 — Tab Switching Hides/Shows Managed Windows

**Description**  
Clicking a tab hides the window belonging to the previously active workspace and shows the window belonging to the newly selected workspace. Both windows continue running in the background.

**Acceptance Criteria**
- [ ] Previously active window disappears from the screen when switching tabs.
- [ ] Newly active window appears and is positioned in the content area.
- [ ] Windows remain running (their process is not suspended or killed).
- [ ] Switching to a workspace with no assigned window shows the placeholder.

---

### Feature 5 — Window Enumeration and Selection Dialog

**Description**  
The window enumeration uses `EnumWindows` with a callback to collect all visible top-level windows. Results are presented in a sortable list dialog.

**Acceptance Criteria**
- [ ] Enumeration completes in under one second for typical desktop loads.
- [ ] List shows window title and process name.
- [ ] WindowManager's own window is excluded from the list.
- [ ] A search/filter box allows the user to narrow the list by typing.

---

## Future Feature Roadmap

### Phase 2 — Multiple Windows per Row (Horizontal Tiling)

**Description**  
Each workspace tab can hold more than one window arranged in a horizontal row. Windows are given equal width by default.

**Technical Challenges**
- Calculating equal-width rectangles for N windows in the content area.
- Rebalancing when a window is added or removed from a row.
- Handling windows with minimum-size constraints.

**Approach**
- Extend `Workspace.Windows` from a single reference to `List<ManagedWindow>`.
- `WindowService.PositionWindows(IList<IntPtr> handles, Rect contentArea)` calculates and applies rectangles.

---

### Phase 3 — Grid Layouts (Rows and Columns)

**Description**  
Workspaces support a 2-D grid, allowing windows to be arranged in both rows and columns.

**Technical Challenges**
- Defining and storing a flexible grid structure.
- Mapping grid cells to managed windows.
- Providing a UI to add/remove rows and columns.

**Approach**
- Introduce a `GridLayout` model with `Rows` and `Columns` collections.
- Render grid handles in the content area as resize grippers.

---

### Phase 4 — Split-Screen with Adjustable Ratios

**Description**  
Adjacent windows in a row (or column) can be resized by dragging a splitter handle, with ratios stored per workspace.

**Technical Challenges**
- Translating drag-delta pixel values into proportional ratios.
- Clamping minimum sizes so windows don't become unusably small.
- Persisting ratios without a storage layer (in-memory for MVP of this phase).

**Approach**
- Use a `GridSplitter` (WPF built-in) or a custom drag-gripper overlay.
- Store ratios as a `double[]` on the workspace layout model.

---

### Phase 5 — Keyboard Shortcuts

**Description**  
Global or application-scoped hotkeys for switching between workspace tabs and moving focus between windows within a tab.

**Technical Challenges**
- Global hotkeys require `RegisterHotKey` Win32 API and message pump handling.
- Conflicts with hotkeys used by managed applications.
- Configurable key bindings need a settings UI.

**Approach**
- Use `RegisterHotKey` / `UnregisterHotKey` via P/Invoke for global shortcuts.
- Provide a settings dialog for configuring bindings.
- Document default bindings in README.

---

### Phase 6 — Launch Applications from the Manager

**Description**  
Users can associate an executable with a workspace slot. The manager can launch the application and immediately manage it.

**Technical Challenges**
- Finding the newly launched window handle (the process may create the window after a short delay).
- Handling applications that spawn multiple windows.

**Approach**
- After `Process.Start`, poll `EnumWindows` filtered by the new `ProcessId` until a visible window appears (with timeout).
- Store the executable path in the `Workspace` model.

---

### Phase 7 — Workspace Persistence (Save/Load)

**Description**  
Workspace names and associated executable paths are saved to disk and restored on next launch.

**Technical Challenges**
- Window handles change between sessions; only executable paths can be persisted.
- The application must re-launch or re-attach windows on load.

**Approach**
- Serialise workspace configuration to JSON in the user's `AppData` folder.
- On startup, load saved workspaces and offer to re-launch associated applications.

---

### Phase 8 — Multi-Monitor Support

**Description**  
Workspaces can be pinned to specific monitors. The content area spans the correct monitor's working area.

**Technical Challenges**
- Enumerating monitors and their working areas (`EnumDisplayMonitors`, `GetMonitorInfo`).
- DPI awareness when monitors have different scaling factors.
- Handling monitor connect/disconnect events at runtime.

**Approach**
- Use `Screen` from `System.Windows.Forms` or `MonitorFromWindow` / `GetMonitorInfo` via P/Invoke.
- Mark the application as per-monitor DPI aware in the manifest.
- Subscribe to `SystemEvents.DisplaySettingsChanged` to react to monitor changes.

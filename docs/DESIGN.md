# Design

## UI/UX Design Philosophy

WindowManager aims for a minimal, distraction-free interface. The chrome of the application itself should never compete with the managed windows it controls. Dark tones keep the sidebar visually recessive while still being clearly readable.

Design principles:
- **One primary action visible at all times** — the active workspace tab is always obvious.
- **Minimal clicks** — creating a workspace, selecting a window, and switching between them should each take no more than two interactions.
- **Non-destructive** — closing a workspace or removing a window from management never closes the actual application.

---

## Layout

```
┌──────────────────────────────────────────────────────┐
│  WindowManager                                       │
├──────────────┬───────────────────────────────────────┤
│  Workspaces  │                                       │
│  ──────────  │                                       │
│  [Tab 1]  ◄  │   Managed window fills this area      │
│  [Tab 2]     │   (or placeholder text when empty)    │
│  [Tab 3]     │                                       │
│              │                                       │
│  + New       │                                       │
└──────────────┴───────────────────────────────────────┘
   200 px            Remaining width
```

### Left Sidebar (200 px fixed width)
- Dark background (`#2D2D30`) to contrast with the content area.
- "Workspaces" heading at the top.
- Scrollable list of workspace tab buttons below the heading; the active tab is highlighted.
- "**+ New Workspace**" button pinned at the bottom of the sidebar (or just below the list for MVP).
- Each tab button shows the workspace name. Future: right-click context menu for rename/delete.

### Main Content Area (remaining width)
- Very dark background (`#1E1E1E`) acts as a frame for the managed window.
- When a workspace has no assigned window, a centred placeholder message ("Select a window to manage") is shown.
- When a workspace has an assigned window, that window is resized/repositioned to cover this area exactly (full-screen within the work area, excluding the taskbar and the sidebar).

---

## User Interaction Flows

### Creating a New Workspace Tab
1. User clicks **"+ New Workspace"**.
2. An inline text input appears (or a small prompt dialog) asking for the workspace name.
3. User types a name and presses **Enter** (or clicks **OK**).
4. A new tab button appears in the sidebar and becomes the active workspace.

### Selecting a Window to Manage
1. User is on a workspace that has no assigned window (or clicks a "Select Window" button that appears in the empty content area).
2. A **Window Selector dialog** opens, listing all currently visible top-level windows with their titles and process names.
3. User selects a window from the list and clicks **OK** (or double-clicks the row).
4. The dialog closes; the selected window is hidden from its original position and then repositioned/shown to fill the content area.

### Switching Between Tabs
1. User clicks a tab in the sidebar.
2. The currently active workspace's managed window (if any) is hidden via `ShowWindow(SW_HIDE)`.
3. The newly selected workspace's managed window (if any) is shown and repositioned via `ShowWindow(SW_SHOW)` + `SetWindowPos`.
4. The active tab highlight updates in the sidebar.

### Removing a Window from Management
1. User clicks a **"Remove"** or **"×"** button associated with the managed window (future: context menu on the tab).
2. The window is shown again in its original state (restored, not repositioned).
3. The workspace content area reverts to the empty placeholder.

---

## Window Management Behaviour

### Full-Screen Positioning
The managed window is resized to cover the entire working area to the right of the sidebar. The exact rectangle is calculated at runtime:

```
x      = sidebar width (200 px) converted to physical pixels
y      = 0 (top of working area)
width  = screen working width − sidebar width
height = screen working height (excluding taskbar)
```

`SetWindowPos` is called with `SWP_NOZORDER` so the window does not change its Z-order, and with `HWND_TOP` when it needs to be brought to the front.

### Show/Hide Logic When Switching Tabs
- **Hide**: `ShowWindow(hWnd, SW_HIDE)` — the window remains in memory and continues running; it simply disappears from the screen and the taskbar.
- **Show**: `ShowWindow(hWnd, SW_SHOW)` followed by `SetWindowPos` to reposition it to the content area rectangle, then `SetForegroundWindow` to give it keyboard focus.

### Handling Closed / Invalid Windows
- When the application detects (via `IsWindow`) that a stored handle is no longer valid, it automatically removes the `ManagedWindow` from its workspace and shows the empty placeholder.
- Detection happens: (a) when the user switches to a workspace, and (b) on a periodic background check (future enhancement).

---

## Future Design Considerations

| Phase | Design Change |
|-------|--------------|
| Multiple windows per row | Content area splits horizontally into equal-width tiles; each tile hosts one managed window. |
| Grid layouts | Content area supports a 2-D grid; sidebar tab shows a mini-map of the layout. |
| Split-screen with adjustable ratios | Drag handles between tiles let the user resize windows within a row/column. |
| Keyboard shortcuts | A small overlay or settings panel exposes configurable key bindings for tab switching and window focus. |
| Window previews | Tab buttons show a live thumbnail of the managed window (using `DwmRegisterThumbnail`). |
| Drag-and-drop reordering | Workspace tabs in the sidebar can be reordered by dragging. |

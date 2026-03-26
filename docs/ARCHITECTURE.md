# Architecture

## Overview

WindowManager is a WPF desktop application built on the MVVM (Model-View-ViewModel) pattern. It uses the Windows API (Win32) via P/Invoke to enumerate and manipulate other application windows, while presenting a clean WPF user interface that lets the user create workspaces and assign windows to them.

---

## Component Breakdown

### Models (`src/WindowManager/Models/`)

Data-only classes that represent the core domain concepts.

| Class | Responsibility |
|-------|---------------|
| `ManagedWindow` | Holds a Win32 window handle (`HWND`), the window title, and the owning process name. Provides an `IsValid()` method that checks whether the handle is still live. |
| `Workspace` | Represents a named workspace tab. For MVP it holds a single `ManagedWindow`; future phases will extend this to support multiple windows. |

### Services (`src/WindowManager/Services/`)

Business-logic classes that interact with the Windows API and coordinate workspace lifecycle. Services are injected into ViewModels and do **not** depend on WPF types.

| Class | Responsibility |
|-------|---------------|
| `WindowService` | Wraps Win32 calls. Enumerates visible top-level windows, shows/hides windows by handle, and repositions a window to fill the working area. |
| `WorkspaceManager` | Maintains the ordered list of workspaces, handles creation/removal, and orchestrates show/hide transitions when the active workspace changes. |

### ViewModels (`src/WindowManager/ViewModels/`)

Implement `INotifyPropertyChanged` and expose observable state consumed by the Views via data binding.

| Class | Responsibility |
|-------|---------------|
| `MainViewModel` | Root view model. Owns the workspace collection and the currently active workspace. Exposes commands for creating workspaces and triggering window selection. |
| `WorkspaceViewModel` | Wraps a single `Workspace` model and exposes it for data binding and display in the UI. |

### Views (`src/WindowManager/Views/`)

Pure XAML + code-behind. Code-behind contains only UI-specific logic (event wiring, animations). All meaningful logic lives in the ViewModel.

| File | Responsibility |
|------|---------------|
| `MainWindow.xaml` | Application shell. Two-column layout: left sidebar + right content area. |
| `Controls/SidebarControl.xaml` | Lists workspace tabs, provides a button to add new workspaces, and communicates selection back through data binding. |

### Helpers (`src/WindowManager/Helpers/`)

| Class | Responsibility |
|-------|---------------|
| `NativeMethods` | `static` class containing all `[DllImport]` declarations and associated constants for Win32 API functions. Centralises P/Invoke in one place. |

---

## Key Technologies & Frameworks

| Technology | Version | Purpose |
|-----------|---------|---------|
| .NET | 10.0 (net10.0-windows) | Runtime and SDK |
| WPF | Built-in | UI framework |
| C# | 13 (via .NET 10 SDK) | Language |
| Win32 API (user32.dll) | — | Window enumeration and manipulation |

---

## Windows API Integration Strategy

All Win32 interop is funnelled through `NativeMethods.cs`. This keeps `unsafe`/`extern` declarations in one auditable location and makes mocking easier in tests.

`WindowService` calls into `NativeMethods` and converts raw `HWND` values into `ManagedWindow` model objects. The rest of the application never calls `NativeMethods` directly.

Key Win32 APIs used:

| API | Purpose |
|-----|---------|
| `EnumWindows` | Iterate all top-level windows |
| `GetWindowText` | Read the window title string |
| `GetWindowTextLength` | Determine buffer size for title |
| `IsWindowVisible` | Filter out invisible/system windows |
| `GetWindowThreadProcessId` | Map window to owning process |
| `IsWindow` | Validate a stored handle is still live |
| `ShowWindow` | Show (`SW_SHOW`) or hide (`SW_HIDE`) a window |
| `SetWindowPos` | Reposition and resize a window |
| `SetForegroundWindow` | Bring a window to the front |
| `GetSystemMetrics` | Read screen resolution |
| `SystemParametersInfo` | Read working area (screen minus taskbar) |

---

## Data Flow

```
User action (click tab)
        │
        ▼
MainViewModel updates active workspace (e.g., via command/property)
        │
        ├─► WorkspaceManager.SwitchToWorkspace(workspace)
        │           │
        │           ├─► WindowService.HideWindow(previousHandle)
        │           └─► WindowService.ShowWindow(newHandle)
        │                       │
        │                       └─► NativeMethods.ShowWindow / SetWindowPos
        │
        └─► Raises PropertyChanged → UI updates active tab highlight
```

---

## MVVM Pattern

```
View  ──(DataContext)──►  ViewModel  ──(calls)──►  Service  ──(P/Invoke)──►  Win32
  ▲                            │
  └────(PropertyChanged)───────┘
```

- **Views** bind to ViewModel properties and commands; they contain no business logic.
- **ViewModels** implement `INotifyPropertyChanged`; they call Services and update observable state.
- **Services** are plain C# classes; they contain all business logic and Win32 interactions.
- **Models** are pure data containers with no dependencies.

---

## Threading Considerations

- Window enumeration (`EnumerateWindows`) should be run on a background thread to keep the UI responsive, then results marshalled back to the UI thread via `Dispatcher.Invoke`.
- Show/hide and reposition operations are fast Win32 calls and can generally run on the UI thread without noticeable delay. If performance issues arise, these can be offloaded with `Task.Run`.
- `ObservableCollection<T>` mutations must always occur on the UI thread.

---

## Error Handling Strategy

- **Invalid handles**: Before every Win32 call that uses a stored handle, call `NativeMethods.IsWindow(handle)` and remove the `ManagedWindow` from its workspace if the check fails.
- **Access denied**: Some elevated/system windows reject manipulation. Catch `Win32Exception` around `SetWindowPos`/`ShowWindow` calls and surface a user-friendly message.
- **Empty workspaces**: The UI should gracefully handle a workspace with no assigned window (show a placeholder message).

# WindowManager

A Windows window management application built in C# WPF that allows you to organise open application windows into named tabbed workspaces with full-screen tiling.

## Overview

WindowManager lets you group your open Windows applications into distinct workspaces (tabs). Each workspace can hold a managed window that is displayed full-screen in the content area when its tab is selected. Switching between tabs automatically hides the previously active window and shows the newly selected one, letting you juggle multiple streams of work without clutter.

## Purpose

Modern work involves running many applications at once — browsers, terminals, editors, communication tools, and more. WindowManager helps you organise these into logical groups so you can stay focused on one context at a time while keeping everything else running in the background.

## Key Features (MVP v1.0)

- **Tabbed Workspaces** — create and name your own workspace tabs in a left sidebar
- **Window Selection** — pick any existing open Windows window to attach to a workspace
- **Full-Screen Tiling** — the managed window is automatically resized to fill the content area
- **Tab Switching** — switching tabs hides the previous window and shows the new one (windows keep running)
- **Window Enumeration** — a built-in dialog lists all currently open, visible windows for selection

## Technology Stack

| Component | Technology |
|-----------|-----------|
| UI Framework | C# WPF (.NET 6.0+) |
| Pattern | MVVM (Model-View-ViewModel) |
| Windows Integration | Win32 API via P/Invoke |
| Language | C# 10+ |

## Getting Started

### Build Requirements

- Windows 10 or Windows 11
- [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) or later
- Visual Studio 2022 (recommended) **or** the `dotnet` CLI

### Clone & Build

```bash
git clone https://github.com/mikejashton/WindowManager.git
cd WindowManager
dotnet build WindowManager.sln
```

### Run the Application

```bash
dotnet run --project src/WindowManager/WindowManager.csproj
```

Or open `WindowManager.sln` in Visual Studio 2022 and press **F5**.

## Project Structure

```
WindowManager/
├── .gitignore
├── LICENSE
├── README.md
├── WindowManager.sln
├── docs/
│   ├── ARCHITECTURE.md
│   ├── DESIGN.md
│   └── FEATURES.md
└── src/
    └── WindowManager/
        ├── WindowManager.csproj
        ├── App.xaml / App.xaml.cs
        ├── Models/
        │   ├── ManagedWindow.cs
        │   └── Workspace.cs
        ├── Services/
        │   ├── WindowService.cs
        │   └── WorkspaceManager.cs
        ├── ViewModels/
        │   ├── MainViewModel.cs
        │   └── WorkspaceViewModel.cs
        ├── Views/
        │   ├── MainWindow.xaml / MainWindow.xaml.cs
        │   └── Controls/
        │       ├── SidebarControl.xaml
        │       └── SidebarControl.xaml.cs
        └── Helpers/
            └── NativeMethods.cs
```

## Roadmap

| Phase | Feature |
|-------|---------|
| v1.0 | Single window per tab, full-screen tiling, tab switching |
| v2.0 | Multiple windows per row (horizontal tiling) |
| v3.0 | Grid layouts (rows and columns) |
| v4.0 | Split-screen with adjustable ratios |
| v5.0 | Keyboard shortcuts |
| v6.0 | Launch applications from the manager |
| v7.0 | Workspace persistence (save/load) |
| v8.0 | Multi-monitor support |

See [docs/FEATURES.md](docs/FEATURES.md) for full details.

## Contributing

Contributions are welcome! Please open an issue to discuss a feature or bug before submitting a pull request. Make sure your code follows the existing style and that all existing functionality continues to work.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-feature`)
3. Commit your changes (`git commit -m 'Add my feature'`)
4. Push to the branch (`git push origin feature/my-feature`)
5. Open a pull request

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.
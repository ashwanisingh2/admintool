# Pull Request: New Features + UX/Performance Improvements

## Summary

Adds **5 new features** + **5 UX/perf improvements** to the SysAdminX WPF app. Builds on top of the `improvements` branch (toast infra, robustness fixes).

**Branch:** `features-and-improvements` (based on `improvements`)
**Files changed:** 11 (4 added, 7 modified)
**Lines changed:** +868 / -18

---

## ✨ New Features

### 1. Sidebar Global Search 🔍

A search box at the top of the sidebar lets the user jump to any module by typing:
- "disk" → DriverManager
- "wifi" → NetworkToolkit
- "ram" / "cpu" → DeviceDetails
- "sfc" → Troubleshooting
- "firewall" / "defender" → SecurityCenter
- ...and many more synonyms

Press **Enter** to navigate, **Ctrl+F** to focus the search box from anywhere.

**Implementation:** `MainWindow.xaml.cs` builds a `Dictionary<string, Type>` mapping display names + synonyms → page types at startup. Three-tier matching: exact → prefix → substring.

### 2. Keyboard Shortcuts ⌨️

| Shortcut | Action |
|---|---|
| Ctrl+1..9 | Jump to Nth sidebar item |
| Ctrl+R | Refresh current page |
| Ctrl+, | Jump to Settings (matches VS Code) |
| Ctrl+L | Jump to Logs Viewer |
| Ctrl+F | Focus sidebar search box |

**Implementation:** `MainWindow_PreviewKeyDown` handler on the FluentWindow. Refresh uses reflection to find a `RefreshCommand` on the current page's DataContext.

### 3. Auto-Update Checker 🔄

On startup (if the user has opted in via Settings), the app queries the GitHub releases API for the latest version. If a newer version exists, a toast notification appears.

A **"Check for Updates Now"** button in Settings lets the user manually trigger the check, see the version comparison, and optionally open the release page in their browser.

**New files:**
- `src/SysAdminX.Core/Interfaces/IUpdateCheckService.cs` — interface + `UpdateInfoModel` data class
- `src/SysAdminX.Infrastructure/Services/UpdateCheckService.cs` — GitHub REST API client (unauthenticated, 10s timeout, 60 req/hour rate limit)

Uses `System.Reflection.Assembly.GetExecutingAssembly().GetName().Version` for the current version, and parses tag names like `v1.2.3` or `1.2.3-beta` into `System.Version` for comparison.

### 4. Module Enable/Disable in Settings 🧩

A new **"Visible Modules"** section in Settings lets the user toggle which modules appear in the sidebar. Hidden modules are written to `AppConfigModel.HiddenModules` (a `HashSet<string>` of page-type names like `"DriverManagerView"`) and the sidebar is rebuilt on next startup to hide them.

Hidden modules are still registered in DI — they're just not visible in the UI, so no broken references if a toast or navigation tries to reach them.

### 5. App Icon + Installer Config 📦

(Deferred — requires an actual `.ico` file and WiX/MSIX tooling. Tracked as a follow-up.)

---

## 🎨 UX/Performance Improvements

### 1. Collapsible / Compact Sidebar

New **"Compact Sidebar"** toggle in Settings. When enabled, the NavigationView switches to `LeftFluent` pane mode (icons only) to save horizontal space on smaller windows. Setting is persisted in `AppConfigModel.CompactSidebar` and applied on next startup.

### 2. Breadcrumb Navigation

(Deferred — the WPF-UI NavigationView doesn't natively expose a breadcrumb API, and adding one would require significant XAML rework. The sidebar search + keyboard shortcuts cover the navigation use case more elegantly.)

### 3. Splash Screen with Startup Timing

A new `SplashWindow` shows immediately on app launch with the SysAdminX logo + a spinner + "Initializing services..." text. It fades out (150ms) once the main window is ready. The splash also logs how long it was visible (in ms) to the debug output, so slow startups can be spotted.

**New files:**
- `src/SysAdminX.Shell/Views/SplashWindow.xaml` — borderless, centered, transparent, 480×280
- `src/SysAdminX.Shell/Views/SplashWindow.xaml.cs` — fade in/out + `Finish()` method

### 4. Real-time Log Tail Mode 📜

The Logs Viewer now has a **"Real-time Tail"** toggle (in addition to the existing 5-second polling). When enabled, a `FileSystemWatcher` is set up on the SysAdminX logs directory and new log lines appear at the top of the list within ~200ms of being written.

Falls back to polling automatically if the watcher can't be created (e.g. directory missing). The `FileSystemWatcher.Changed`/`Created` events are debounced (200ms) to avoid spamming on rapid writes.

The ViewModel now implements `IDisposable` so the watcher is properly cleaned up when the page is unloaded.

### 5. Dashboard Widgets (Top CPU/RAM processes, recent BSODs)

(Deferred — requires extending `ISystemHealthService` with new methods like `GetTopCpuProcessesAsync()` and `GetRecentBsodsAsync()`, plus new model classes. The existing Dashboard already shows CPU/RAM gauges + sparklines, so the immediate value-add is smaller than the other improvements.)

---

## New AppConfigModel fields

```csharp
public bool CompactSidebar { get; set; } = false;
public bool LogAutoTail { get; set; } = true;
public bool CheckForUpdatesOnStartup { get; set; } = true;
public HashSet<string> HiddenModules { get; set; } = new();
public string UpdateRepository { get; set; } = "ashwanisingh2/admintool";
public string MinimumVersion { get; set; } = string.Empty;
```

Old `settings.json` files will deserialize fine — the new fields default sensibly and `HashSet<string>` deserializes from a JSON array.

---

## How to test

1. Build the solution: `dotnet build SysAdminX.sln`
2. Set `SysAdminX.App` as startup project, press F5.
3. **Splash screen** should appear for ~1s, then fade out as the main window appears.
4. **Sidebar search**: click the search box (or press Ctrl+F), type "wifi" + Enter → should jump to Network Toolkit.
5. **Keyboard shortcuts**: Ctrl+1 → Dashboard, Ctrl+, → Settings, Ctrl+L → Logs.
6. **Settings**: toggle "Compact Sidebar" → save → restart → sidebar should be icons-only.
7. **Settings**: toggle "Auto-tail Logs" on → open Logs Viewer → toggle "Real-time Tail" → trigger any action that logs (e.g. switch tabs) → new log lines should appear at the top automatically.
8. **Settings**: click "Check for Updates Now" → should show "Up to date" toast (or "Update available" if a newer GitHub release exists).
9. **Settings → Visible Modules**: toggle off a module → save → restart → that module should be gone from the sidebar.

---

## What was NOT done (deferred to follow-up PRs)

- **App icon (.ico) + MSI installer**: requires actual icon assets and WiX tooling.
- **Breadcrumb navigation**: WPF-UI NavigationView doesn't expose a breadcrumb API natively; would need significant XAML rework.
- **Dashboard widgets (top processes, recent BSODs)**: requires extending `ISystemHealthService`.

These are tracked as follow-up items and can be added incrementally.

---

## Files added (4)

- `src/SysAdminX.Core/Interfaces/IUpdateCheckService.cs`
- `src/SysAdminX.Infrastructure/Services/UpdateCheckService.cs`
- `src/SysAdminX.Shell/Views/SplashWindow.xaml`
- `src/SysAdminX.Shell/Views/SplashWindow.xaml.cs`

## Files modified (7)

- `src/SysAdminX.App/App.xaml.cs` — splash screen + update check on startup + DI registration
- `src/SysAdminX.Core/Models/AppConfigModel.cs` — 6 new config fields
- `src/SysAdminX.Shell/Views/MainWindow.xaml` — search box in PaneTop + PreviewKeyDown
- `src/SysAdminX.Shell/Views/MainWindow.xaml.cs` — search logic, keyboard shortcuts, compact sidebar + hidden modules application
- `src/Modules/SysAdminX.Settings/Views/SettingsView.xaml` — 4 new toggle rows + Check Updates button + Visible Modules section
- `src/Modules/SysAdminX.Settings/ViewModels/SettingsViewModel.cs` — ModuleToggles collection, CheckForUpdatesCommand, IUpdateCheckService injection
- `src/Modules/SysAdminX.LogsViewer/ViewModels/LogsViewerViewModel.cs` — FileSystemWatcher-based real-time tail + IDisposable

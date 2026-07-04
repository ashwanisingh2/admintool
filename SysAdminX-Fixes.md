# SysAdminX — Complete Fix Document

**Goal:** Make your WPF admin tool actually run, with 3 dead modules (Microsoft 365, Azure, Active Directory) removed and all critical bugs fixed.

**Stack:** C# .NET 8 + WPF + WPF-UI 3.0.5 + CommunityToolkit.Mvvm

**Repo:** https://github.com/ashwanisingh2/admintool.git

---

## How to use this document

1. Open the repo in Visual Studio 2022 (or JetBrains Rider / VS Code with C# Dev Kit).
2. For each fix below, open the file, find the exact line, and paste the replacement code.
3. After all fixes, run `dotnet build SysAdminX.sln` — should succeed with zero errors.
4. Set `SysAdminX.App` as startup project, press F5.

**Conventions used:**
- 🔴 = Critical (app won't run / module dead)
- 🟠 = High (broken UX feature)
- 🟡 = Medium (works but fragile)
- File paths are relative to repo root.

---

# PART 1 — Remove 3 Dead Modules

These 3 modules are orphaned — in `.sln` and `Shell.csproj` but never registered in DI, never reachable from UI. Either remove them or wire them up. Below we **remove** them.

## 1.1 Delete the 3 module folders

In Solution Explorer (or file explorer), delete these 3 folders:
- `src/Modules/SysAdminX.Microsoft365/`
- `src/Modules/SysAdminX.Azure/`
- `src/Modules/SysAdminX.ActiveDirectory/`

## 1.2 Edit `SysAdminX.sln`

Open `SysAdminX.sln` in a text editor. Find and DELETE these 3 project blocks:

```text
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "SysAdminX.ActiveDirectory", "src\Modules\SysAdminX.ActiveDirectory\SysAdminX.ActiveDirectory.csproj", "{8C8D2A92-3B7C-4D8B-A3F9-91A0BD8B4B4B}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "SysAdminX.Azure", "src\Modules\SysAdminX.Azure\SysAdminX.Azure.csproj", "{6EA5B7D4-5C72-4D9D-A99B-7F7C5E8A2A2A}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "SysAdminX.Microsoft365", "src\Modules\SysAdminX.Microsoft365\SysAdminX.Microsoft365.csproj", "{4D9F7E2C-1D85-4E67-9F47-7A2489DBB0CC}"
EndProject
```

Also in the `GlobalSection(NestedProjects)` section, delete any lines containing the GUIDs `{8C8D2A92-...}`, `{6EA5B7D4-...}`, `{4D9F7E2C-...}` on the left side.

## 1.3 Edit `src/SysAdminX.Shell/SysAdminX.Shell.csproj`

Delete these 3 lines from `<ItemGroup>`:

```xml
<ProjectReference Include="..\Modules\SysAdminX.ActiveDirectory\SysAdminX.ActiveDirectory.csproj" />
<ProjectReference Include="..\Modules\SysAdminX.Microsoft365\SysAdminX.Microsoft365.csproj" />
<ProjectReference Include="..\Modules\SysAdminX.Azure\SysAdminX.Azure.csproj" />
```

## 1.4 Edit `src/SysAdminX.Shell/Views/MainWindow.xaml`

Open the file. Find the `xmlns:adViews`, `xmlns:m365Views`, `xmlns:azViews` declarations near the top (around lines 19–21) and DELETE those 3 lines.

Also delete any `<ui:NavigationViewItem>` entries that reference `adViews:`, `m365Views:`, or `azViews:` (if any exist — the review found none, but verify).

## 1.5 Verify build

Run `dotnet build SysAdminX.sln`. Should succeed with 17 modules remaining.

---

# PART 2 — Fix Critical Bugs (App Currently Broken)

## 🔴 Fix 2.1 — SecurityCenter module is 100% dead (ExecuteScriptAsync misuse)

**File:** `src/Modules/SysAdminX.SecurityCenter/Services/SecurityService.cs`

**Problem:** 7 calls use `_psService.ExecuteScriptAsync(script)` where `script` is a PowerShell **command string**, but `ExecuteScriptAsync` expects a **file path**. All 7 calls return `Failure("Script file not found: Get-MpComputerStatus | ...")`.

**Fix:** Replace every `ExecuteScriptAsync(` with `ExecuteCommandAsync(` in this file.

In Visual Studio: `Ctrl+H` → Find: `ExecuteScriptAsync(` → Replace: `ExecuteCommandAsync(` → click "Replace All" (scope: current file). Should replace 7 occurrences (around lines 41, 79, 158, 205, 267, 319, 356).

**After this fix:** Defender status, BitLocker, Firewall, UAC, Windows Update, and Secure Boot panels will actually populate.

---

## 🔴 Fix 2.2 — ProcessExecutorService freezes UI for elevated processes

**File:** `src/SysAdminX.Infrastructure/ProcessExecutorService.cs`

**Problem:** When `requireElevation: true`, the code calls `process.WaitForExit();` **synchronously on the UI thread**. SFC scans take 5–15 min, DISM takes 10–30 min — UI shows "Not Responding" the entire time. Plus, the method returns a hardcoded `"Elevated command executed."` instead of actual output.

**Fix:** Find this block (around lines 87–93):

```csharp
else
{
    process.WaitForExit();
    return Result<string>.Success("Elevated command executed.");
}
```

Replace with:

```csharp
else
{
    // Run WaitForExit on a thread-pool thread so we don't freeze the UI.
    // Note: with UseShellExecute=true we cannot redirect stdout/stderr,
    // so we cannot capture output. Callers that need output should write
    // to a temp file inside the elevated command and read it afterwards.
    await Task.Run(() => process.WaitForExit(), ct);
    return Result<string>.Success($"Elevated command exited with code {process.ExitCode}.");
}
```

**Also fix the non-elevated branch race condition** (around lines 74–84):

Find:
```csharp
process.Exited += (s, e) => tcs.TrySetResult(outputBuilder.ToString());
```

After this line, ensure the method awaits `tcs.Task` and THEN calls `process.WaitForExit()` once more to flush async output buffers. Final non-elevated branch should look like:

```csharp
else
{
    process.EnableRaisingEvents = true;
    process.OutputDataReceived += (s, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
    process.ErrorDataReceived += (s, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };

    process.Start();
    process.BeginOutputReadLine();
    process.BeginErrorReadLine();

    var tcs = new TaskCompletionSource<string>();
    process.Exited += (s, e) => tcs.TrySetResult(outputBuilder.ToString());

    await tcs.Task;
    process.WaitForExit(); // flush async buffers

    if (process.ExitCode != 0 && !string.IsNullOrEmpty(errorBuilder.ToString()))
        return Result<string>.Failure($"Exit {process.ExitCode}: {errorBuilder}");
    return Result<string>.Success(outputBuilder.ToString());
}
```

---

## 🔴 Fix 2.3 — Troubleshooting actions block on `& pause`

**File:** `src/Modules/SysAdminX.Troubleshooting/Services/TroubleshootingService.cs`

**Problem:** All 13 actions pass commands like `"/c sfc /scannow & pause"`. The `& pause` makes the elevated console stay open until user presses a key — `WaitForExit()` never returns, UI hangs forever.

**Fix:** Find every `"/c ... & pause"` string in this file and remove the `& pause` suffix. Use `Ctrl+H` → Find: `& pause` → Replace: `` (empty) → Replace All (current file). Should affect ~13 occurrences.

**Example — before:**
```csharp
return await _processService.ExecuteAsync("cmd.exe", "/c sfc /scannow & pause", true, ct);
```

**After:**
```csharp
return await _processService.ExecuteAsync("cmd.exe", "/c sfc /scannow", requireElevation: true, ct: ct);
```

**Optional polish:** If you want users to see the console output after completion, change strategy — write the command output to a temp file and read it back, instead of keeping the console open.

---

## 🔴 Fix 2.4 — PatchManager install freezes + ReadKey block

**File:** `src/Modules/SysAdminX.PatchManager/Services/PatchManagerService.cs`

**Problem 1:** `InstallMissingUpdatesAsync` (around line 257) calls `ExecuteAsync(..., requireElevation: true, ct)` — UI freezes due to Fix 2.2 (already fixed above, but verify).

**Problem 2:** The PowerShell script embedded around lines 251–252 ends with `$Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')` — the elevated PowerShell waits for a keypress, blocking `WaitForExit`.

**Fix:** Find the embedded PowerShell script (look for `ReadKey` around line 251–252) and DELETE that line.

```powershell
# DELETE this line from the embedded script:
$Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
```

Also find line 218 and check the result file path interpolation. If the temp path contains a single quote, the PS string breaks. Replace the path interpolation:

```csharp
// BEFORE
$@"InstallUpdateResult_{Guid.NewGuid()}.json"

// AFTER (escape single quotes in path)
$@"InstallUpdateResult_{Guid.NewGuid().ToString().Replace("'", "''")}.json"
```

---

## 🔴 Fix 2.5 — BatteryManager detailed report always reports success + unnecessary UAC

**File:** `src/Modules/SysAdminX.BatteryManager/ViewModels/BatteryManagerViewModel.cs`

**Problem:** Line 116 calls `ExecuteAsync("powercfg", $"/batteryreport /output \"{tempFile}\"", requireElevation: true, ct)`. But `powercfg /batteryreport` does **not** require elevation. Passing `requireElevation: true` triggers an unnecessary UAC prompt AND, because the elevated branch in `ProcessExecutorService` returned a hardcoded success string (Fix 2.2), the app proceeds to `Process.Start(tempFile)` even if `powercfg` failed — throws `FileNotFoundException`.

**Fix:** Find around line 116:

```csharp
var result = await _processExecutorService.ExecuteAsync(
    "powercfg",
    $"/batteryreport /output \"{tempFile}\"",
    requireElevation: true,
    ct);
```

Change to:

```csharp
var result = await _processExecutorService.ExecuteAsync(
    "powercfg",
    $"/batteryreport /output \"{tempFile}\"",
    requireElevation: false,
    ct: ct);

if (!result.IsSuccess)
{
    ErrorMessage = result.ErrorMessage ?? "Failed to generate battery report.";
    return;
}

if (!System.IO.File.Exists(tempFile))
{
    ErrorMessage = "Battery report file was not created.";
    return;
}

Process.Start(new ProcessStartInfo(tempFile) { UseShellExecute = true });
```

---

## 🟠 Fix 2.6 — `Icon="Search24"` on ui:TextBox throws XamlParseException

**Files (7 total):**
- `src/Modules/SysAdminX.DriverManager/Views/DriverManagerView.xaml:137`
- `src/Modules/SysAdminX.PatchManager/Views/PatchManagerView.xaml:112`
- `src/Modules/SysAdminX.NetworkToolkit/Views/NetworkToolkitView.xaml:127`
- `src/Modules/SysAdminX.LogsViewer/Views/LogsViewerView.xaml:97`
- (Also check: ActiveDirectory, Microsoft365, Azure — but those are deleted per Part 1)

**Problem:** `<ui:TextBox Icon="Search24" />` — the `Icon` property is type `IconElement?`, and WPF-UI 3.0.5 does NOT ship a `TypeConverter` for it. At runtime this throws `XamlParseException: Cannot convert "Search24" to type "IconElement"` and the page renders blank or crashes.

**Fix:** In each file, find every `Icon="Search24"` attribute on a `<ui:TextBox>` and convert to element syntax. Example:

**Before:**
```xml
<ui:TextBox PlaceholderText="Search drivers..." Icon="Search24" Text="{Binding SearchQuery, UpdateSourceTrigger=PropertyChanged}" />
```

**After:**
```xml
<ui:TextBox PlaceholderText="Search drivers..." Text="{Binding SearchQuery, UpdateSourceTrigger=PropertyChanged}">
    <ui:TextBox.Icon>
        <ui:SymbolIcon Symbol="Search24" />
    </ui:TextBox.Icon>
</ui:TextBox>
```

Repeat for all 7 occurrences across the 4 remaining files (the 3 deleted modules don't matter).

---

## 🟠 Fix 2.7 — DriverManagerView binding `IsError` should be `HasError`

**File:** `src/Modules/SysAdminX.DriverManager/Views/DriverManagerView.xaml:20`

**Problem:** `<DataTrigger Binding="{Binding IsError}" Value="True">` — but the VM has `HasError`, not `IsError`. The trigger never fires, error-state styling is dead.

**Fix:** Find line 20:

```xml
<DataTrigger Binding="{Binding IsError}" Value="True">
```

Change to:

```xml
<DataTrigger Binding="{Binding HasError}" Value="True">
```

---

## 🟠 Fix 2.8 — `NavigationViewPageProvider.GetPage` returns null silently

**File:** `src/SysAdminX.Shell/Services/NavigationViewPageProvider.cs`

**Problem:** If a `NavigationViewItem.TargetPageType` points to a View not registered in DI, `GetService` returns `null`. WPF-UI's NavigationView receives `null` and either shows a blank page or throws an internal NRE.

**Fix:** Find around lines 37–39:

```csharp
return _serviceProvider.GetService(pageType) as FrameworkElement;
```

Replace with:

```csharp
var page = _serviceProvider.GetService(pageType) as FrameworkElement;
if (page is null)
{
    _logger.LogError("Navigation target page is not registered in DI: {PageType}", pageType.FullName);
    throw new InvalidOperationException(
        $"Page '{pageType.FullName}' is not registered in the DI container. " +
        "Add it in App.xaml.cs ConfigureServices().");
}
return page;
```

Make sure the class has an `ILogger<NavigationViewPageProvider> _logger` field injected via constructor. If not, add it:

```csharp
private readonly IServiceProvider _serviceProvider;
private readonly ILogger<NavigationViewPageProvider> _logger;

public NavigationViewPageProvider(IServiceProvider serviceProvider, ILogger<NavigationViewPageProvider> logger)
{
    _serviceProvider = serviceProvider;
    _logger = logger;
}
```

---

## 🟡 Fix 2.9 — App.OnStartup is `async void` (silent crash risk)

**File:** `src/SysAdminX.App/App.xaml.cs`

**Problem:** `private async void OnStartup(...)`. If any awaited line throws (e.g., DI misconfiguration, settings load failure), the exception propagates to `DispatcherUnhandledException` which may not display properly during early startup — app silently dies.

**Fix:** Wrap the entire method body in try/catch. Find the method (around line 42) and refactor:

```csharp
private async void OnStartup(object sender, StartupEventArgs e)
{
    try
    {
        _serviceProvider = ConfigureServices();
        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        var settings = await settingsService.LoadSettingsAsync();

        ApplyTheme(settings.Theme);

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
    catch (Exception ex)
    {
        System.Windows.MessageBox.Show(
            $"Fatal startup error:\n\n{ex}",
            "SysAdminX — Startup Failed",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        Shutdown(1);
    }
}
```

(Adjust the variable names to match your actual code — the key change is the try/catch wrapper.)

---

## 🟡 Fix 2.10 — Remove dead code from MainWindowViewModel

**File:** `src/SysAdminX.Shell/ViewModels/MainWindowViewModel.cs`

**Problem:** The VM defines `ObservableCollection<NavigationMenuItem> MenuItems` and `FooterItems` and populates 17 items in `InitializeMenuItems()`. But `MainWindow.xaml` hardcodes the `NavigationViewItem` elements directly in XAML — never binds to these collections. The `NavigationMenuItem` class, the collections, and the `NavigateToPage` command are all dead code.

**Fix (Option A — delete the dead code, recommended):**

Delete these from `MainWindowViewModel.cs`:
- `MenuItems` property (around line 39)
- `FooterItems` property (around line 40)
- `InitializeMenuItems()` method (lines 71–209)
- `NavigateToPage` command + method (lines 215–240)
- `NavigationMenuItem` class definition (lines 246–276)
- Any call to `InitializeMenuItems()` in the constructor

Keep only: `CurrentPageTitle` property (still bound in XAML).

**Fix (Option B — wire it up properly):**

In `MainWindow.xaml`, replace all hardcoded `<ui:NavigationViewItem>` elements with:

```xml
<ui:NavigationView.MenuItems>
    <ui:NavigationViewItem Content="Dashboard" TargetPageType="local:DashboardView">
        <ui:NavigationView.Icon>
            <ui:SymbolIcon Symbol="Apps24" />
        </ui:NavigationView.Icon>
    </ui:NavigationViewItem>
    <!-- ... etc ... -->
</ui:NavigationView.MenuItems>
```

…and bind `ItemsSource="{Binding MenuItems}"` with an `ItemTemplate`. Option A is simpler.

---

## 🟡 Fix 2.11 — SystemCleanupService.CalculateSpaceAsync returns 0 (stub)

**File:** `src/SysAdminX.Infrastructure/Services/SystemCleanupService.cs`

**Problem:** Around line 104, `CalculateSpaceAsync` always returns `Result<long>.Success(0)`. The space calculation feature is broken.

**Fix:** Replace the stub body with actual size calculation. Example:

```csharp
public async Task<Result<long>> CalculateSpaceAsync(IEnumerable<CleanupItemModel> items, CancellationToken ct = default)
{
    long totalBytes = 0;
    foreach (var item in items.Where(i => i.IsSelected))
    {
        try
        {
            if (!Directory.Exists(item.Path))
                continue;

            totalBytes += await Task.Run(() =>
                Directory.EnumerateFiles(item.Path, "*", new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true
                })
                .Sum(f => new FileInfo(f).Length), ct);
        }
        catch (UnauthorizedAccessException) { /* skip */ }
        catch (IOException) { /* skip */ }
    }
    return Result<long>.Success(totalBytes);
}
```

---

## 🟡 Fix 2.12 — Move fire-and-forget loads out of constructors

**Files:**
- `src/Modules/SysAdminX.Reports/ViewModels/ReportsViewModel.cs:40` — `LoadHistoryCommand.Execute(null);`
- `src/Modules/SysAdminX.Settings/ViewModels/SettingsViewModel.cs:38` — `LoadSettingsCommand.Execute(null);`
- `src/Modules/SysAdminX.SecurityCenter/ViewModels/SecurityCenterViewModel.cs:56` — `LoadDataCommand.Execute(null);`
- `src/Modules/SysAdminX.LogsViewer/ViewModels/LogsViewerViewModel.cs:64` — `RefreshLogsCommand.Execute(null);`

**Problem:** Calling `command.Execute(null)` in a constructor is fire-and-forget. If the command throws, the exception is lost (or crashes via `DispatcherUnhandledException`). Also breaks design-time data.

**Fix:** In each View's code-behind, subscribe to the `Loaded` event and call the load command there.

**Example for `ReportsView.xaml.cs`:**

```csharp
public ReportsView()
{
    InitializeComponent();
    Loaded += ReportsView_Loaded;
}

private async void ReportsView_Loaded(object sender, RoutedEventArgs e)
{
    if (DataContext is ReportsViewModel vm && !vm.IsLoaded)
    {
        try
        {
            await vm.LoadHistoryCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            // log + show error in UI
        }
    }
}
```

Add an `IsLoaded` flag to the VM to prevent double-loading. Repeat the same pattern for the other 3 files.

---

## 🟡 Fix 2.13 — LogsService only reads most recent log file

**File:** `src/Modules/SysAdminX.LogsViewer/Services/LogsService.cs`

**Problem:** Line 53 only reads `logFiles.First()`. If today's log is empty but yesterday's has entries, history is lost.

**Fix:** Find around line 53 and replace the single-file read with a multi-file merge:

```csharp
var logFiles = Directory.GetFiles(_logDirectory, "sysadminx-*.log")
    .OrderByDescending(f => f)
    .Take(7)  // last 7 days
    .ToList();

var entries = new List<LogEntryModel>();
foreach (var file in logFiles)
{
    try
    {
        var lines = await File.ReadAllLinesAsync(file, ct);
        foreach (var line in lines)
        {
            if (LogEntryModel.TryParse(line, out var entry))
                entries.Add(entry);
        }
    }
    catch (IOException) { /* skip unreadable file */ }
}

return Result<List<LogEntryModel>>.Success(entries.OrderByDescending(e => e.Timestamp).ToList());
```

(Adjust `TryParse` to match your actual `LogEntryModel` API — if it doesn't have a `TryParse`, add one.)

---

## 🟡 Fix 2.14 — ServiceManager missing `finally { IsLoading = false }`

**File:** `src/Modules/SysAdminX.ServiceManager/ViewModels/ServiceManagerViewModel.cs`

**Problem:** Lines 97, 111, 125, 139 set `IsLoading = true` before async calls but never reset to `false` in a `finally`. If the service throws (unlikely since it returns `Result`), the spinner stays forever.

**Fix:** Wrap each async load in try/finally:

```csharp
IsLoading = true;
try
{
    var result = await _serviceService.GetServicesAsync(ct);
    if (result.IsSuccess)
        Services = new ObservableCollection<WindowsServiceModel>(result.Data);
    else
        HasError = true;
}
finally
{
    IsLoading = false;
}
```

---

# PART 3 — Recommended Improvements (Optional, Do After Part 1+2)

These are not blocking but will make the app more robust. Skip if you just want it running.

## 3.1 Remove unused `SysAdminX.Data` project

**Why:** The project references EF Core Sqlite but contains only an empty `DataAssemblyMarker.cs` — no DbContext, no entities. `App.csproj` references it but never uses it. Dead weight.

**Action:**
1. Remove `<ProjectReference Include="..\SysAdminX.Data\SysAdminX.Data.csproj" />` from `src/SysAdminX.App/SysAdminX.App.csproj`.
2. Delete `src/SysAdminX.Data/` folder.
3. Remove the `SysAdminX.Data` project block from `SysAdminX.sln`.

## 3.2 Remove dead `IAppConfigService` interface

**File:** `src/SysAdminX.Core/Interfaces/IAppConfigService.cs`

**Why:** Never implemented, never registered in DI. `ISettingsService` (in Settings module) does the same job with a JSON file.

**Action:** Delete the file.

## 3.3 Remove unnecessary Infrastructure references

**Files:** `Dashboard.csproj`, `DeviceDetails.csproj`, `DriverManager.csproj`, `PatchManager.csproj`, `NetworkToolkit.csproj`, `Reports.csproj`

**Why:** These modules reference Infrastructure directly but only use Core interfaces (the actual Infrastructure implementations are injected via DI at runtime). The direct reference is unnecessary and breaks the "modules depend only on abstractions" pattern.

**Action:** In each file, delete the line:
```xml
<ProjectReference Include="..\..\SysAdminX.Infrastructure\SysAdminX.Infrastructure.csproj" />
```

## 3.4 Remove `InverseBooleanConverter.Execute` dead method

**File:** `src/SysAdminX.App/ValueConverters.cs:16`

**Action:** Delete the `public object Execute(object value)` method — looks like copy-paste from `ICommand`, not part of `IValueConverter`.

## 3.5 Standardize module service location

**Why:** Currently inconsistent — `IBatteryManagerService` lives in the module, but `IServiceManagerService` lives in Core with implementation in Infrastructure.

**Action:** Pick one pattern (recommended: interface + implementation both in the module, Infrastructure only for cross-cutting OS services like WMI/Registry/PowerShell/Process). Migrate the outliers over time.

## 3.6 Add unit tests

**Why:** The `ExecuteScriptAsync` bug (Fix 2.1) would have been caught instantly by one test calling `SecurityService.GetDefenderStatusAsync()` and asserting the result is not empty.

**Action:** Add a `SysAdminX.Tests` project (xUnit), reference `SysAdminX.Core` + `SysAdminX.Infrastructure`, write smoke tests for each service.

---

# PART 4 — Final Build & Run Checklist

After applying all of Part 1 + Part 2 fixes:

1. **Clean solution:** `dotnet clean SysAdminX.sln`
2. **Restore packages:** `dotnet restore SysAdminX.sln`
3. **Build:** `dotnet build SysAdminX.sln` — should succeed with 0 errors, only nullable warnings.
4. **Set startup project:** Right-click `SysAdminX.App` → "Set as Startup Project".
5. **Press F5** to debug, or `Ctrl+F5` to run without debugging.

## Smoke-test each module after launch

| Module | What to verify |
|--------|----------------|
| Dashboard | CPU/RAM gauges show numbers, sparkline updates every 2s |
| Device Details | Cards populate with OS, CPU, RAM, motherboard, GPU, disk, network info |
| Driver Manager | Table loads with ~15+ rows, search filters live, status chips work, "Scan" button shows spinner |
| Patch Manager | "Missing Updates" tab shows updates, "Install All" runs progress bar, "Update History" tab shows entries |
| Network Toolkit | Ping Sweep / Port Scan / Active Connections / WiFi — each tab loads data |
| Troubleshooting | Click "Run" on SFC Scan — console opens, runs, returns, UI does NOT freeze |
| AI Assistant | Type a message, get a response (needs API key configured in Settings) |
| Reports | History loads on page open (Fix 2.12), generate button works |
| Settings | Loads current settings (Fix 2.12), theme toggle works, save shows toast |
| Logs Viewer | Logs load (Fix 2.12), level filter works, search works, auto-refresh toggles |
| Security Center | All 6 cards (Defender, BitLocker, Firewall, UAC, Update, Secure Boot) show real status (Fix 2.1) |
| Remote Support | Session list loads, "Connect" opens modal |
| Battery Manager | Battery level ring shows, "Generate report" creates HTML file (Fix 2.5) |
| Service Manager | Services list loads (Fix 2.14), Start/Stop/Restart buttons work |
| System Cleanup | Space calculation shows real byte count (Fix 2.11) |
| Software Manager | Installed software list loads, "Uninstall" runs |
| Portable Tools | Tool list loads, "Download" works |

---

# Summary

- **Part 1** removes 3 dead modules (M365, Azure, AD).
- **Part 2** fixes 14 concrete bugs — 5 critical (red), 3 high (orange), 6 medium (yellow).
- **Part 3** is optional cleanup for long-term maintainability.
- **Part 4** is the final build + smoke-test checklist.

After Parts 1 + 2, the app should build cleanly and every module should actually function. Expected total fix time: 60–90 minutes for someone familiar with the codebase.

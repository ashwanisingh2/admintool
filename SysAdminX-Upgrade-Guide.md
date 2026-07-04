# SysAdminX — Upgrade & Feature Adoption Guide

**Goal:** Port the best features from `SolasCarePro` (github.com/ashwanisingh2/SolasCarePro) into your `SysAdminX` WPF tool — features that SysAdminX currently does NOT have.

**Source project:** Solas Care Pro v2.0.0 (Electron + React + WPF + 30 PowerShell scripts)
**Target project:** SysAdminX (.NET 8 WPF + WPF-UI, 17 modules after removing M365/Azure/AD)

---

## How to use this document

Each section follows the same template:
1. **Feature name** + emoji + priority
2. **What it does** (one-paragraph explanation)
3. **Why SysAdminX needs it** (gap analysis vs current 17 modules)
4. **How SolasCarePro does it** (PowerShell script + React UI patterns)
5. **How to port it to SysAdminX** (concrete file paths, classes, XAML structure)
6. **Files to create / modify** (checklist)
7. **Acceptance criteria** (how to test it works)

After applying all sections, SysAdminX goes from 17 modules → 25 modules with significantly richer functionality.

---

# PART A — Critical Features to Add (Priority Order)

## A1. One-Click Care Wizard 🔴 [Priority 1]

### What it does
A multi-step repair orchestrator that runs in sequence:
1. Create System Restore Point
2. Junk Cleanup (with preview + 30-second undo)
3. Network Optimization (with active-download detection)
4. SFC Scan (with live progress parsing + minimize-to-tray)
5. SSD TRIM (skipped automatically for HDDs)
6. Security Audit (Defender + Firewall status check)

Each step shows real-time progress, console output, estimated time remaining, and can be paused/cancelled.

### Why SysAdminX needs it
SysAdminX's Troubleshooting module runs each fix as an **isolated** action — user must manually click 13 separate "Run" buttons. No chaining, no restore point first, no progress streaming. A 30-minute SFC scan shows nothing but a frozen spinner.

### How SolasCarePro does it
- **Frontend:** `src/components/OneClickCare.jsx` — a state machine with phases: `idle → restore → restore-failed → junk-scan → junk-preview → junk-undo → network-check → network-warn → network-run → sfc → trim → security → complete`
- **Backend:** `scripts/iobit_one_click_care.ps1` — wraps each step in a `Run-Step` function that emits `[STEP_START]` / `[STEP_SUCCESS]` / `[STEP_ERROR]` markers. Frontend parses stdout line-by-line via Electron's `child_process.spawn`.
- **SFC progress parsing:** regex `Verification (\d+)% complete` extracts percentage, multiplied by elapsed time to estimate remaining time.
- **Restore Point safeguard:** queries `HKLM:\...\SystemRestore` registry, sets `SystemRestorePointCreationFrequency = 0` before creating the point (Windows otherwise blocks points < 24h apart).

### How to port it to SysAdminX
**Create new module:** `src/Modules/SysAdminX.OneClickCare/`

**Project structure:**
```
SysAdminX.OneClickCare/
├── SysAdminX.OneClickCare.csproj
├── AssemblyInfo.cs
├── Views/
│   ├── OneClickCareView.xaml
│   └── OneClickCareView.xaml.cs
├── ViewModels/
│   └── OneClickCareViewModel.cs
├── Services/
│   ├── IOneClickCareService.cs
│   └── OneClickCareService.cs
└── Models/
    ├── CareStepModel.cs
    ├── CarePhase.cs        // enum: Idle, Restore, JunkScan, JunkPreview, JunkUndo, NetworkCheck, NetworkRun, Sfc, Trim, Security, Complete
    └── JunkItemModel.cs
```

**Service implementation pattern** (uses your existing `IProcessExecutorService` + `IPowerShellService`):

```csharp
public class OneClickCareService : IOneClickCareService
{
    private readonly IProcessExecutorService _processService;
    private readonly ILogger<OneClickCareService> _logger;

    public event EventHandler<StepProgressEventArgs>? StepProgressChanged;

    public async Task RunCareSequenceAsync(IEnumerable<CareStepModel> steps, CancellationToken ct)
    {
        foreach (var step in steps)
        {
            StepProgressChanged?.Invoke(this, new StepProgressEventArgs(step.Name, "started", 0));
            try
            {
                // Stream stdout by spawning with output redirection
                var result = await _processService.ExecuteAsync(
                    "powershell.exe",
                    $"-NoProfile -ExecutionPolicy Bypass -File \"{step.ScriptPath}\"",
                    requireElevation: true,
                    ct: ct);

                if (!result.IsSuccess)
                {
                    StepProgressChanged?.Invoke(this, new StepProgressEventArgs(step.Name, "failed", 0, result.ErrorMessage));
                    return; // stop the sequence
                }
                StepProgressChanged?.Invoke(this, new StepProgressEventArgs(step.Name, "success", 100));
            }
            catch (OperationCanceledException)
            {
                StepProgressChanged?.Invoke(this, new StepProgressEventArgs(step.Name, "cancelled", 0));
                return;
            }
        }
        StepProgressChanged?.Invoke(this, new StepProgressEventArgs("All steps", "complete", 100));
    }
}
```

**SFC progress streaming:** SolasCarePro streams stdout; you need to extend `ProcessExecutorService` to accept a `IProgress<string>` callback. Add this method:

```csharp
public async Task<Result<string>> ExecuteStreamingAsync(
    string fileName, string arguments, bool requireElevation,
    IProgress<string>? outputProgress, CancellationToken ct)
{
    // Same as ExecuteAsync but invokes outputProgress.Report(line) for each stdout line.
    // The OneClickCareViewModel subscribes and parses SFC progress.
}
```

**ViewModel SFC parsing:**

```csharp
private void OnStepProgress(object? sender, StepProgressEventArgs e)
{
    if (e.StepName == "SFC Scan" && e.OutputLine is string line)
    {
        var match = Regex.Match(line, @"Verification (\d+)% complete");
        if (match.Success)
        {
            SfcProgress = int.Parse(match.Groups[1].Value);
            EstimateRemainingTime(SfcProgress);
        }
    }
}
```

**XAML layout** (vertical stepper with 6 cards, each showing status icon + progress bar):

```xml
<ItemsControl ItemsSource="{Binding Steps}">
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <Border Padding="16" Margin="0,4" CornerRadius="8" BorderThickness="1">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    <ui:SymbolIcon Symbol="{Binding StatusIcon}" Grid.Column="0"/>
                    <StackPanel Grid.Column="1">
                        <TextBlock Text="{Binding Name}" FontWeight="SemiBold"/>
                        <ProgressBar Value="{Binding Progress}" Maximum="100" Margin="0,4,0,0"/>
                    </StackPanel>
                    <TextBlock Text="{Binding EstimatedTimeRemaining}" Grid.Column="2"/>
                </Grid>
            </Border>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

**Files to create/modify:**
- [ ] Create `src/Modules/SysAdminX.OneClickCare/` (full module)
- [ ] Copy `scripts/iobit_one_click_care.ps1` → `src/SysAdminX.Infrastructure/Scripts/iobit_one_click_care.ps1` (set Build Action = Embedded Resource)
- [ ] Add module to `SysAdminX.sln`, `Shell.csproj`, `App.csproj`
- [ ] Register in `App.xaml.cs`: `services.AddTransient<OneClickCareView>(); services.AddSingleton<IOneClickCareService, OneClickCareService>();`
- [ ] Add `<ui:NavigationViewItem>` in `MainWindow.xaml` under TOOLS section
- [ ] Extend `IProcessExecutorService` with `ExecuteStreamingAsync`
- [ ] Add "Minimize to tray" capability to `MainWindow` (see section C1)

**Acceptance criteria:**
- [ ] Clicking "Start One-Click Care" runs all 6 steps in sequence
- [ ] SFC step shows live `Verification X% complete` progress
- [ ] Estimated time remaining updates every 5 seconds
- [ ] Cancel button stops the sequence cleanly
- [ ] Minimizing to tray keeps the scan running in background
- [ ] Failure of any step stops the sequence and shows error toast

---

## A2. Auto-Pilot (Scheduled Maintenance) 🔴 [Priority 2]

### What it does
A weekly automated task scheduler that runs the One-Click Care sequence at a user-chosen time (default: Sunday 3 AM). Uses Windows Task Scheduler with `SYSTEM` account + `RunLevel Highest`. UI shows: enabled toggle, day/time picker, action checkboxes (junk/network/drivers/sfc/trim), live countdown to next run, last-run result, and a "Run task now" button.

### Why SysAdminX needs it
SysAdminX has no scheduling. Users must remember to run maintenance manually. SolasCarePro's Auto-Pilot is the single most useful feature for non-technical users.

### How SolasCarePro does it
- **Frontend:** `src/components/AutoPilot.jsx` — countdown timer with `useEffect` polling `check_task_status.ps1` every 30s
- **Backend:** 3 scripts:
  - `scripts/schedule_care.ps1` — registers task with `New-ScheduledTaskPrincipal -UserId "SYSTEM" -RunLevel Highest`
  - `scripts/check_task_status.ps1` — queries `Get-ScheduledTask` + last run result
  - `scripts/unschedule_care.ps1` — unregisters the task
- **Verification:** queries `schtasks /Query /XML` and asserts `<RunLevel>HighestAvailable</RunLevel>` is present

### How to port it to SysAdminX
**Create new module:** `src/Modules/SysAdminX.AutoPilot/`

**Critical security note:** Register the task as `SYSTEM` (not the user), so it runs even if the user is logged off. Verify `<RunLevel>HighestAvailable</RunLevel>` is in the XML after registration — if not, the task silently runs without admin rights and SFC/TRIM fail.

**Service code (skeleton):**

```csharp
public class AutoPilotService : IAutoPilotService
{
    private readonly IProcessExecutorService _process;

    public async Task<Result> ScheduleAsync(string dayOfWeek, string time, AutoPilotActions actions, CancellationToken ct)
    {
        var scriptPath = ExtractEmbeddedScript("schedule_care.ps1");
        var result = await _process.ExecuteAsync(
            "powershell.exe",
            $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -Day {dayOfWeek} -Time {time}",
            requireElevation: true, ct: ct);

        if (!result.IsSuccess) return Result.Failure(result.ErrorMessage);

        // Verify RunLevel Highest is in the task XML
        var verify = await _process.ExecuteAsync("schtasks.exe", "/Query /TN SolasSystemCarePro_WeeklyCare /XML", false, ct);
        if (!verify.IsSuccess || !verify.Data.Contains("<RunLevel>HighestAvailable</RunLevel>"))
            return Result.Failure("Task registered but RunLevel verification failed.");

        return Result.Success();
    }

    public async Task<Result<AutoPilotTaskInfo>> GetStatusAsync(CancellationToken ct)
    {
        var scriptPath = ExtractEmbeddedScript("check_task_status.ps1");
        var result = await _process.ExecuteAsync("powershell.exe", $"-File \"{scriptPath}\"", false, ct);
        if (!result.IsSuccess) return Result<AutoPilotTaskInfo>.Failure(result.ErrorMessage);
        var info = JsonSerializer.Deserialize<AutoPilotTaskInfo>(result.Data);
        return Result<AutoPilotTaskInfo>.Success(info!);
    }

    public async Task<Result> UnscheduleAsync(CancellationToken ct) { ... }
}
```

**ViewModel polling pattern:**

```csharp
public AutoPilotViewModel(IAutoPilotService service)
{
    _service = service;
    _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
    _pollTimer.Tick += async (s, e) => await RefreshStatusAsync();
    _pollTimer.Start();
}

private async Task RefreshStatusAsync()
{
    var result = await _service.GetStatusAsync(CancellationToken.None);
    if (result.IsSuccess)
    {
        TaskInfo = result.Data;
        UpdateCountdown();  // calculate days/hours/minutes to next run
    }
}
```

**Files to create/modify:**
- [ ] Create `src/Modules/SysAdminX.AutoPilot/`
- [ ] Copy 3 scripts as embedded resources
- [ ] Add DI registrations, NavItem in MainWindow
- [ ] Add "Run task now" button → calls `schtasks /Run /TN SolasSystemCarePro_WeeklyCare`
- [ ] Add notifications (toast) when task completes — use `TaskScheduler` event log monitoring

**Acceptance criteria:**
- [ ] User can enable/disable the schedule
- [ ] Countdown timer updates every second
- [ ] Last-run result shows Success/Failed + timestamp
- [ ] "Run task now" executes the schedule immediately
- [ ] Task XML verification catches silent RunLevel downgrade
- [ ] Disabling the schedule properly unregisters the task

---

## A3. System Restore Point Manager 🟠 [Priority 3]

### What it does
Before any risky operation (driver uninstall, registry edit, SFC, junk cleanup), automatically create a system restore point. UI shows: list of existing restore points, "Create now" button, "Restore" button (opens Windows System Restore UI), and an "Enable System Protection" button if it's disabled.

### Why SysAdminX needs it
SysAdminX's Troubleshooting runs `sfc /scannow` and DISM without any safety net. A failed SFC can leave Windows in an unbootable state. SolasCarePro creates a restore point BEFORE every repair — and verifies it succeeded.

### How SolasCarePro does it
- **Script:** `scripts/create_restore_point.ps1`
- **Pre-check:** queries `HKLM:\Software\Microsoft\Windows NT\CurrentVersion\SystemRestore` to see if protection is enabled on C:
- **Workaround:** sets `SystemRestorePointCreationFrequency = 0` to bypass Windows' 24-hour minimum interval
- **Verification:** queries WMI `Win32_SystemRestore` after creation to confirm the point exists

### How to port it to SysAdminX
**Add to:** `src/SysAdminX.Infrastructure/Services/SystemRestoreService.cs` (new file)

```csharp
public interface ISystemRestoreService
{
    Task<Result<bool>> IsProtectionEnabledAsync(CancellationToken ct);
    Task<Result> EnableProtectionAsync(string driveLetter, CancellationToken ct);
    Task<Result<SystemRestorePoint>> CreatePointAsync(string description, CancellationToken ct);
    Task<Result<List<SystemRestorePoint>>> ListPointsAsync(CancellationToken ct);
    Task<Result> RestoreToPointAsync(int sequenceNumber, CancellationToken ct);
}

public class SystemRestoreService : ISystemRestoreService
{
    // All methods call PowerShell via IProcessExecutorService
    // Scripts embedded as resources, extracted to temp at runtime
}
```

**Integration point:** modify `OneClickCareService` (section A1) to call `ISystemRestoreService.CreatePointAsync` as the first step.

**UI:** add a new sub-tab in the existing `Settings` module (or `Troubleshooting`) showing the restore points list + create/restore buttons.

**Files to create/modify:**
- [ ] Create `src/SysAdminX.Infrastructure/Services/SystemRestoreService.cs`
- [ ] Add interface to `src/SysAdminX.Core/Interfaces/ISystemRestoreService.cs`
- [ ] Copy `scripts/create_restore_point.ps1` and `scripts/enable_restore.ps1` as embedded resources
- [ ] Register in DI in `App.xaml.cs`
- [ ] Add UI panel in Settings module
- [ ] Wire `OneClickCareService` to call `CreatePointAsync` first

**Acceptance criteria:**
- [ ] "Create Restore Point" button works and verifies via WMI
- [ ] List of existing restore points loads with timestamps + descriptions
- [ ] "Restore" button launches `rstrui.exe` (Windows System Restore UI)
- [ ] If protection is disabled, shows clear warning + "Enable Protection" button
- [ ] Every One-Click Care run starts with restore point creation

---

## A4. Driver Manager Upgrade — PnP Reset + Registry Backup 🟠 [Priority 4]

### What it does
SysAdminX's Driver Manager only shows a list + "Update" action. SolasCarePro adds:
- **Disable device** (with automatic registry key backup to `.reg` file before disabling)
- **Enable device**
- **Rollback driver**
- **"Registry Safe Mode" toggle** — prevents disabling if registry backup fails
- **"Restore Backup" button** — imports the `.reg` file back

### Why SysAdminX needs it
A driver that's blue-screening Windows needs to be disabled — but if disabling makes things worse, you need a one-click rollback. SolasCarePro's pattern of "backup registry first, then act" is the gold standard for risky device operations.

### How SolasCarePro does it
- **Script:** `scripts/repair_driver.ps1` — accepts params: `-Action disable|enable|rollback -HardwareId <id>`
- **Registry backup:** `reg export "HKLM\SYSTEM\CurrentControlSet\Enum\$hwid" "$env:TEMP\solas_driver_backup_$hwid.reg" /y`
- **PnP commands:** `Disable-PnpDevice -InstanceId $id -Confirm:$false` / `Enable-PnpDevice` / `pnputil /delete-driver`
- **Safe Mode check:** if `reg export` fails, the script aborts with `[SAFE_MODE_ABORT]` marker

### How to port it to SysAdminX
**Modify:** `src/Modules/SysAdminX.DriverManager/Services/DriverManagerService.cs`

Add 3 new methods to the interface:

```csharp
public interface IDriverManagerService
{
    // existing
    Task<Result<List<DriverInfoModel>>> GetDriversAsync(CancellationToken ct);
    Task<Result> UpdateDriverAsync(string hardwareId, CancellationToken ct);

    // NEW from SolasCarePro
    Task<Result<string>> DisableDriverWithBackupAsync(string hardwareId, CancellationToken ct);
    // returns the backup .reg file path
    Task<Result> EnableDriverAsync(string hardwareId, CancellationToken ct);
    Task<Result> RollbackDriverAsync(string hardwareId, CancellationToken ct);
    Task<Result> RestoreFromBackupAsync(string backupFilePath, CancellationToken ct);
}
```

**XAML:** extend the existing Actions dropdown in `DriverManagerView.xaml` to include Disable / Enable / Rollback / Restore Backup. Add a "Registry Safe Mode" toggle in the toolbar.

**Files to create/modify:**
- [ ] Copy `scripts/repair_driver.ps1` as embedded resource
- [ ] Extend `IDriverManagerService` + `DriverManagerService`
- [ ] Extend `DriverManagerView.xaml` Actions column
- [ ] Add "Registry Safe Mode" toggle + "Restore Backup" button
- [ ] Add file picker dialog for restoring `.reg` backup

**Acceptance criteria:**
- [ ] Disable creates a `.reg` backup before disabling; backup path shown in toast
- [ ] If backup fails, Disable is aborted (when Safe Mode is on)
- [ ] Enable works on a previously-disabled device
- [ ] Rollback removes the current driver via `pnputil /delete-driver`
- [ ] Restore Backup imports a chosen `.reg` file via `reg import`

---

## A5. Privacy Cleaner 🟠 [Priority 5]

### What it does
Scans and cleans privacy-sensitive data across categories:
- Browser History (Chrome, Edge, Firefox, Brave, Opera)
- Cookies & Site Data
- DNS Cache
- Thumbnail Cache
- Recent Documents
- Clipboard History
- Windows Telemetry data
- Recycle Bin

Each category shows a checkbox + size estimate. User selects → "Clean Selected" runs PowerShell to delete the files.

### Why SysAdminX needs it
SysAdminX has a "System Cleanup" module, but it's a generic junk cleaner (temp files, etc.) — no privacy-specific categories. SolasCarePro's Privacy Cleaner is purpose-built for privacy and includes per-browser handling.

### How to port it to SysAdminX
**Create new module:** `src/Modules/SysAdminX.PrivacyCleaner/`

**Model:**

```csharp
public class PrivacyCategoryModel
{
    public string Id { get; set; }            // "browserHistory"
    public string Name { get; set; }          // "Browser History"
    public string Description { get; set; }
    public string Icon { get; set; }          // Lucide/SymbolIcon name
    public long EstimatedSizeBytes { get; set; }
    public bool IsSelected { get; set; }
    public string PowerShellCommand { get; set; }  // the cleanup command
}
```

**Categories to implement** (copy from SolasCarePro's `PrivacyCleaner.jsx`):
1. Browser History — Chrome/Edge/Firefox/Brave/Opera history files
2. Cookies & Site Data — same browsers' cookie databases
3. DNS Cache — `Clear-DnsClientCache; ipconfig /flushdns`
4. Thumbnail Cache — `%LOCALAPPDATA%\Microsoft\Windows\Explorer\thumbcache_*.db`
5. Recent Documents — `%APPDATA%\Microsoft\Windows\Recent\*`
6. Clipboard History — `Clear-Clipboard` + `Remove-Item` of clipboard cache
7. Windows Telemetry — `Clear-DiagnosticData` (Win10+) or registry keys
8. Recycle Bin — `Clear-RecycleBin -Force`

**Size estimation:** run a separate "scan" command per category that uses `Get-ChildItem -Recurse | Measure-Object -Property Length -Sum`. Show progress bar during scan.

**UI:** grid of cards, each card has icon + name + description + size + checkbox. Top bar: "Select All" / "Select None" / "Scan" / "Clean Selected".

**Files to create/modify:**
- [ ] Create `src/Modules/SysAdminX.PrivacyCleaner/`
- [ ] Create `scripts/privacy_scan.ps1` + `scripts/privacy_clean.ps1` (embedded resources)
- [ ] Add DI registrations + NavItem (under TOOLS section)

**Acceptance criteria:**
- [ ] Scan shows realistic sizes per category within 5 seconds
- [ ] Clean Selected deletes only checked categories
- [ ] Browser cleanup correctly handles all 5 supported browsers
- [ ] Recycle Bin clearing asks for confirmation
- [ ] After cleaning, sizes show as 0 / "Cleaned"

---

## A6. Browser Repair Tool 🟡 [Priority 6]

### What it does
Detects installed browsers (Chrome, Edge, Firefox, Brave, Opera) via registry scan, then offers per-browser repair actions:
- Clear cache
- Reset to defaults
- Re-register (if installation is corrupt)
- Show installation path

### Why SysAdminX needs it
SysAdminX has no browser tool. Browser issues (corrupt cache, broken default associations) are some of the most common helpdesk tickets.

### How to port it to SysAdminX
**Create new module:** `src/Modules/SysAdminX.BrowserRepair/`

**Detection logic** (PowerShell, run via `IPowerShellService.ExecuteCommandAsync`):
- Chrome: `Test-Path "HKLM:\SOFTWARE\Google\Update\Clients\{8A69D345-D569-443e-A1D1-3F2F95F3934B}"`
- Edge: built-in (always present on Win10+)
- Firefox: `Test-Path "HKLM:\SOFTWARE\Mozilla\Mozilla Firefox"`
- Brave: `Test-Path "HKCU:\SOFTWARE\BraveSoftware\Brave-Browser"`
- Opera: `Test-Path "HKCU:\SOFTWARE\Opera Software"`

**Cleanup commands** per browser (run via `IProcessExecutorService`):
- Clear cache: `Remove-Item "$env:LOCALAPPDATA\<browser>\User Data\Default\Cache\*" -Recurse -Force`
- Reset: delete `Preferences` and `Local State` JSON files in the user data folder
- Re-register: re-run the browser's installer with `/register` flag

**UI:** 5 cards (one per browser), each shows installed badge + cache size + 3 buttons (Clear Cache / Reset / Re-register).

**Files to create/modify:**
- [ ] Create `src/Modules/SysAdminX.BrowserRepair/`
- [ ] Copy `scripts/browser_reset.ps1` as embedded resource
- [ ] Add DI registrations + NavItem (under TOOLS)

**Acceptance criteria:**
- [ ] Detects all 5 browsers correctly
- [ ] Not-installed browsers show as "Not Found" (greyed out)
- [ ] Clear Cache deletes only cache, preserves passwords/bookmarks
- [ ] Reset warns the user with a confirmation dialog
- [ ] Each action shows a toast on completion

---

## A7. BSOD / Crash Dump Analyzer 🟡 [Priority 7]

### What it does
Scans `C:\Windows\Minidump\` for `.dmp` files, parses each to extract:
- BugCheck code (e.g. `0x000000D1`)
- BugCheck name (e.g. `DRIVER_IRQL_NOT_LESS_OR_EQUAL`)
- Likely cause (driver/module name)
- Timestamp
- Stack trace (top 5 frames)

Generates a styled HTML report at `%TEMP%\sysadminx_bsod_report.html` and opens it in the default browser.

### Why SysAdminX needs it
SysAdminX's `LogsViewer` shows Windows Event Log entries — but BSODs often don't appear in the Event Log with useful detail. SolasCarePro parses the actual minidump files for the real cause.

### How SolasCarePro does it
- **Script:** `scripts/analyze_bsod.ps1`
- **Fallback:** if minidump parsing fails (no `DbgHelp.dll` access), falls back to querying Windows Error Reporting events via `Get-WinEvent -LogName "System" -FilterHashtable @{ProviderName="Microsoft-Windows-WER-SystemErrorReporting"}`
- **BugCheck mapping:** hardcoded hashtable mapping ~50 common codes to human-readable causes
- **HTML report:** generated with here-string template, opened via `Invoke-Item`

### How to port it to SysAdminX
**Create new module:** `src/Modules/SysAdminX.BsodAnalyzer/`

OR — better — extend the existing `LogsViewer` module with a new "Crash Dumps" sub-tab.

**Recommended: extend LogsViewer:**
- Add 3 sub-tabs to `LogsViewerView.xaml`: "Event Logs" (existing) | "Crash Dumps" | "Application Errors"
- Crash Dumps tab calls a new `IBsodAnalyzerService`
- HTML report generation: use a Razor Light or just string interpolation in C#

**Service skeleton:**

```csharp
public interface IBsodAnalyzerService
{
    Task<Result<List<BsodEntryModel>>> AnalyzeDumpsAsync(CancellationToken ct);
    Task<Result<string>> GenerateHtmlReportAsync(IEnumerable<BsodEntryModel> entries, CancellationToken ct);
}
```

**BugCheck mapping:** copy SolasCarePro's hashtable into a `static Dictionary<uint, string>` in C#.

**Files to create/modify:**
- [ ] Copy `scripts/analyze_bsod.ps1` as embedded resource
- [ ] Add `IBsodAnalyzerService` + `BsodAnalyzerService` to Infrastructure
- [ ] Add `BsodEntryModel` to Core/Models
- [ ] Extend `LogsViewerView.xaml` with sub-tabs
- [ ] Extend `LogsViewerViewModel` with `BsodEntries` collection + `AnalyzeDumpsCommand`

**Acceptance criteria:**
- [ ] Lists all `.dmp` files in `C:\Windows\Minidump\` with timestamps
- [ ] Each entry shows BugCheck code + name + likely cause
- [ ] "Generate Report" button creates HTML and opens it in browser
- [ ] Falls back to WER events if no dumps exist
- [ ] Empty state shows "No crashes detected" message

---

## A8. Performance Mode Profiles 🟡 [Priority 8]

### What it does
3 pre-set performance profiles the user can apply with one click:
- **Gaming Mode:** Ultimate Performance power plan, Game Mode on, HAGS on, background apps off, notifications off, visual effects=Performance
- **Work Mode:** Balanced plan, background apps on, notifications on, visual effects=Balanced
- **Power Saver:** Power Saver plan, background apps off, GPU=Maximum efficiency

Each profile applies ~8 settings via PowerShell registry edits + `powercfg` commands.

### Why SysAdminX needs it
SysAdminX has no power/performance controls. SolasCarePro's profiles are the kind of feature users actively want — especially gamers and laptop users.

### How to port it to SysAdminX
**Create new module:** `src/Modules/SysAdminX.PerformanceMode/`

**Model:**

```csharp
public class PerformanceProfile
{
    public string Id { get; set; }            // "gaming"
    public string Name { get; set; }          // "Gaming Mode"
    public string Description { get; set; }
    public string IconName { get; set; }      // "Zap24" / "Cpu24" / "Battery24"
    public Dictionary<string, object> Settings { get; set; }
    // e.g. { "PowerPlan": "Ultimate Performance", "GameMode": true, ... }
}

public class PerformanceSettingsApplier
{
    public async Task<Result> ApplyAsync(PerformanceProfile profile, CancellationToken ct)
    {
        // 1. Set power plan: powercfg /setactive <guid>
        //    Ultimate Performance GUID: e9a42b02-d5df-448d-aa00-03f14749eb61
        // 2. Set Game Mode: HKCU:\Software\Microsoft\GameBar AllowAutoGameMode = 1
        // 3. Set HAGS: HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers HwSchMode = 2
        // 4. Background apps: HKCU:\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications GlobalUserDisabled
        // 5. Visual effects: SystemPropertiesPerformance.exe /s (or registry)
        // ... etc
    }
}
```

**UI:** 3 large cards side-by-side, each with icon + name + description + "Apply" button. Currently-active profile shows a checkmark badge. Clicking "Apply" runs the PowerShell script + shows toast.

**Files to create/modify:**
- [ ] Create `src/Modules/SysAdminX.PerformanceMode/`
- [ ] Create `scripts/apply_performance_profile.ps1` (embedded resource)
- [ ] Add DI registrations + NavItem (under SYSTEM)

**Acceptance criteria:**
- [ ] 3 profile cards render with correct icons + descriptions
- [ ] Apply button runs the script with elevation
- [ ] Currently-active profile is detected on page load (read current power plan GUID)
- [ ] Toast confirms each setting applied
- [ ] Re-applying the same profile is idempotent

---

## A9. Startup Manager 🟡 [Priority 9]

### What it does
Lists all apps configured to start with Windows (from registry `Run` keys + Startup folder + Task Scheduler). For each: name, command path, source (HKCU/HKLM/Startup Folder/Task), enabled/disabled status, and an "impact" rating (High/Medium/Low based on whether the app pre-loads heavy resources). User can enable/disable each entry.

### Why SysAdminX needs it
SysAdminX has no startup manager. SolasCarePro's includes impact rating + multiple source locations.

### How to port it to SysAdminX
**Create new module:** `src/Modules/SysAdminX.StartupManager/`

**Sources to scan:**
1. `HKCU:\Software\Microsoft\Windows\CurrentVersion\Run`
2. `HKLM:\Software\Microsoft\Windows\CurrentVersion\Run`
3. `HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run` (enabled/disabled flag)
4. `%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\` (folder)
5. `Get-ScheduledTask` (tasks with logon trigger)

**Enable/disable:** flip the `StartupApproved` binary value (3 bytes: `02 00 00 00` = disabled, `06 00 00 00` = enabled). Don't delete the entry — just disable it.

**Impact rating:** heuristic — `High` if the command path matches known heavy apps (Spotify, Discord, Teams, Steam, OneDrive), `Medium` for system tray apps, `Low` for utilities.

**UI:** table with columns: Name, Source, Command, Impact (badge), Status (toggle). Search box. "Open Startup Folder" button.

**Files to create/modify:**
- [ ] Create `src/Modules/SysAdminX.StartupManager/`
- [ ] Copy `scripts/get_startup_apps.ps1` + `scripts/toggle_startup_app.ps1` as embedded resources
- [ ] Add DI registrations + NavItem (under SYSTEM)

**Acceptance criteria:**
- [ ] Lists entries from all 5 sources
- [ ] Toggle changes the StartupApproved binary value
- [ ] Status persists across app restarts
- [ ] Impact badges display correctly
- [ ] "Open Startup Folder" opens Explorer to the right path

---

## A10. Large File Finder 🟡 [Priority 10]

### What it does
Scans a chosen drive for files larger than a user-set threshold (default 100 MB). Shows results in a sortable table: file path, size, last modified, file type. User can select multiple files and either delete them or move to a backup folder.

### Why SysAdminX needs it
SysAdminX has no disk-space analyzer. SolasCarePro's is simple but useful for finding forgotten ISO files, old installers, etc.

### How to port it to SysAdminX
**Create new module:** `src/Modules/SysAdminX.LargeFileFinder/`

**Scan command:** PowerShell `Get-ChildItem -Path "C:\" -Recurse -File -ErrorAction SilentlyContinue | Where-Object Length -gt 100MB | Select-Object FullName, Length, LastWriteTime, Extension | Sort-Object Length -Descending | Select-Object -First 100`

**UI:**
- Drive selector dropdown (populated from `Get-PSDrive -PSProvider FileSystem`)
- Min-size slider (10MB - 1GB)
- "Scan" button with progress bar (PowerShell streams progress via `Write-Progress`)
- Results table with checkboxes
- "Delete Selected" button (with confirmation dialog)
- "Move to Backup Folder" button (opens folder picker)

**Files to create/modify:**
- [ ] Create `src/Modules/SysAdminX.LargeFileFinder/`
- [ ] Create `scripts/scan_large_files.ps1` (embedded resource)
- [ ] Add DI registrations + NavItem (under SYSTEM)

**Acceptance criteria:**
- [ ] Drive selector populates correctly
- [ ] Scan shows progress bar (parse `Write-Progress` output)
- [ ] Results table is sortable by size/date/name
- [ ] Delete asks for confirmation
- [ ] Move-to-backup uses a folder picker dialog

---

## A11. Registry Manager 🟡 [Priority 11]

### What it does
Backup and restore the Windows registry. Creates dated backups of HKLM and HKCU as `.reg` files. Lists all backups with timestamp + size. User can restore any backup with one click.

### Why SysAdminX needs it
SysAdminX has a `RegistryService` interface in Core but it's only for reading values — no backup/restore. SolasCarePro's Registry Manager is a proper backup tool.

### How to port it to SysAdminX
**Create new module:** `src/Modules/SysAdminX.RegistryManager/`

**Backup command:** `reg export HKLM "%BACKUP_DIR%\HKLM_<timestamp>.reg" /y` + `reg export HKCU "%BACKUP_DIR%\HKCU_<timestamp>.reg" /y`

**Restore command:** `reg import "<file>.reg"` (shows UAC prompt via elevation)

**Backup directory:** `%APPDATA%\SysAdminX\RegBackups\`

**UI:** table of backups (timestamp, label, HKLM size, HKCU size, actions: Restore / Delete). "Create New Backup" form with optional label. "Open Backup Folder" button.

**Files to create/modify:**
- [ ] Create `src/Modules/SysAdminX.RegistryManager/`
- [ ] Copy `scripts/registry_backup.ps1` as embedded resource
- [ ] Add DI registrations + NavItem (under SECURITY)

**Acceptance criteria:**
- [ ] Backup creates both HKLM and HKCU files with timestamped names
- [ ] Backup list loads within 2 seconds
- [ ] Restore asks for confirmation (irreversible action)
- [ ] Delete removes both `.reg` files
- [ ] Backup folder is created automatically if missing

---

## A12. SSD TRIM Optimizer 🟡 [Priority 12]

### What it does
Lists all drives with their type (SSD/HDD). For SSDs, runs TRIM optimization. For HDDs, the option is disabled (with explanation tooltip). Auto-detects if TRIM is enabled at the OS level; if not, enables it first.

### Why SysAdminX needs it
SysAdminX has no disk optimization. SolasCarePro correctly distinguishes SSD from HDD and uses the right command per Windows version.

### How to port it to SysAdminX
**Extend existing module:** `src/Modules/SysAdminX.SystemCleanup/` (add a "TRIM Optimization" sub-tab)

OR add as a feature of `DeviceDetails` (since it already shows storage info).

**Recommended: extend DeviceDetails** with a "Storage Optimization" section below the storage info card.

**Drive type detection:** `Get-PhysicalDisk | Select-Object DeviceId, FriendlyName, MediaType` (returns `SSD` or `HDD`)

**TRIM status check:** `fsutil behavior query DisableDeleteNotify` (0 = enabled, 1 = disabled)

**TRIM command:**
- Windows 10+: `Optimize-Volume -DriveLetter C -ReTrim -Verbose`
- Windows 7/8: `defrag C: /L /V`

**UI:** list of drives with type badge + "Run TRIM" button (disabled for HDDs with tooltip "TRIM is for SSDs only").

**Files to create/modify:**
- [ ] Copy `scripts/run_trim.ps1` as embedded resource
- [ ] Add `ITrimService` + `TrimService` to Infrastructure
- [ ] Extend `DeviceDetailsView.xaml` with "Storage Optimization" section
- [ ] Extend `DeviceDetailsViewModel` with `Drives` collection + `RunTrimCommand`

**Acceptance criteria:**
- [ ] Drive list shows correct type (SSD/HDD)
- [ ] TRIM button disabled for HDDs
- [ ] Clicking TRIM enables OS-level TRIM if disabled, then runs ReTrim
- [ ] Progress shown via `Optimize-Volume -Verbose` output streaming
- [ ] Success toast on completion

---

## A13. Component Store Cleanup (DISM) 🟡 [Priority 13]

### What it does
Two-step DISM workflow:
1. **Analyze:** runs `DISM /Online /Cleanup-Image /AnalyzeComponentStore` — shows total component store size + reclaimable space
2. **Cleanup:** runs `DISM /Online /Cleanup-Image /StartComponentCleanup` — actually reclaims the space

### Why SysAdminX needs it
SysAdminX's `SystemCleanup` module deletes temp files but never touches the WinSxS component store, which can grow to 10+ GB on systems with many updates.

### How to port it to SysAdminX
**Extend existing module:** `src/Modules/SysAdminX.SystemCleanup/`

Add a new section "Component Store (WinSxS)" with:
- "Analyze" button → runs DISM analyze, parses output for `Component Store Size` and `Reclaimable Space`
- Displays both numbers prominently
- "Cleanup" button (disabled until analyze completes) → runs DISM cleanup
- Progress bar (DISM streams progress percentages)

**Output parsing:** SolasCarePro's `component_cleanup.ps1` uses regex:
- `(?i)(Component Store Size|Actual Size of Component Store)\s*:\s*([\d\.]+)\s*(GB|MB)`
- `(?i)Reclaimable\s*Space\s*:\s*([\d\.]+)\s*(GB|MB)`

**Files to create/modify:**
- [ ] Copy `scripts/component_cleanup.ps1` as embedded resource
- [ ] Extend `SystemCleanupView.xaml` with Component Store section
- [ ] Extend `SystemCleanupViewModel` with `AnalyzeComponentStoreCommand` + `CleanupComponentStoreCommand`
- [ ] Use `ExecuteStreamingAsync` (from section A1) to parse DISM progress

**Acceptance criteria:**
- [ ] Analyze shows component store size + reclaimable space
- [ ] Cleanup button disabled until analyze completes
- [ ] Cleanup runs with elevation
- [ ] Progress bar updates from DISM's streamed output
- [ ] After cleanup, re-analyze shows reduced size

---

## A14. Hardware Diagnostics — RAM Test Scheduler 🟢 [Priority 14]

### What it does
Schedules a Windows Memory Diagnostic (`MdSched.exe`) reboot test and reads the result after the next reboot. Shows: test date, result (Pass/Fail/No results), and a "Schedule Test" button.

### Why SysAdminX needs it
SysAdminX has no RAM diagnostics. SolasCarePro's reads the WMI `Win32_OperatingSystem` + event log to retrieve the result.

### How to port it to SysAdminX
**Extend:** `src/Modules/SysAdminX.Troubleshooting/` — add a "Hardware Diagnostics" sub-tab.

**Result retrieval:** query `Get-WinEvent -LogName "System" -MaxEvents 200 | Where-Object { $_.ProviderName -eq "Microsoft-Windows-MemoryDiagnostics-Results" }` — the event message contains the test result.

**Schedule:** launch `MdSched.exe` (Windows Memory Diagnostic UI) which lets user choose "Restart now and check" or "Check on next restart".

**Files to create/modify:**
- [ ] Copy `scripts/ram_diagnostic.ps1` as embedded resource
- [ ] Extend `TroubleshootingView.xaml` with Hardware Diagnostics sub-tab
- [ ] Extend `TroubleshootingViewModel` with `RamTestResult` + `ScheduleRamTestCommand` + `CheckRamResultCommand`

**Acceptance criteria:**
- [ ] "Schedule Test" launches MdSched.exe
- [ ] "Check Result" reads the latest memory diagnostic event log entry
- [ ] Shows "No results found" if test hasn't been run
- [ ] Shows Pass/Fail with test timestamp

---

## A15. Junk Cleanup with 30-Second Undo 🟢 [Priority 15]

### What it does
SysAdminX's `SystemCleanup` deletes files directly. SolasCarePro's version:
1. **Preview:** shows list of files to be deleted with sizes
2. **Backup:** moves files to a temp folder (not deletes them) — `%TEMP%\solas_junk_undo\<guid>\`
3. **30-second countdown:** UI shows a countdown timer with "Undo" button
4. **Final delete:** after 30s, the temp folder is permanently deleted
5. **Undo:** if user clicks Undo during the countdown, files are moved back

### Why SysAdminX needs it
Safety net. A user accidentally cleaning an important temp file currently has no recourse.

### How to port it to SysAdminX
**Modify:** `src/SysAdminX.Infrastructure/Services/SystemCleanupService.cs`

Replace the direct `Directory.Delete` calls with a "move to temp" pattern:

```csharp
public async Task<Result<CleanupResultModel>> CleanAsync(IEnumerable<CleanupItemModel> items, CancellationToken ct)
{
    var backupDir = Path.Combine(Path.GetTempPath(), $"sysadminx_junk_undo_{Guid.NewGuid():N}");
    Directory.CreateDirectory(backupDir);

    long totalBytes = 0;
    var movedFiles = new List<(string OriginalPath, string BackupPath)>();

    foreach (var item in items.Where(i => i.IsSelected))
    {
        foreach (var file in Directory.EnumerateFiles(item.Path, "*", SearchOption.AllDirectories))
        {
            try
            {
                var relativePath = Path.GetRelativePath(item.Path, file);
                var backupPath = Path.Combine(backupDir, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
                File.Move(file, backupPath);
                totalBytes += new FileInfo(backupPath).Length;
                movedFiles.Add((file, backupPath));
            }
            catch (IOException) { /* skip locked files */ }
            catch (UnauthorizedAccessException) { /* skip */ }
        }
    }

    return Result<CleanupResultModel>.Success(new CleanupResultModel
    {
        TotalBytesFreed = totalBytes,
        BackupDirectory = backupDir,
        MovedFiles = movedFiles,
        UndoExpiresAt = DateTime.UtcNow.AddSeconds(30)
    });
}

public async Task<Result> UndoAsync(string backupDirectory, List<(string OriginalPath, string BackupPath)> movedFiles, CancellationToken ct)
{
    foreach (var (original, backup) in movedFiles)
    {
        if (File.Exists(backup))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(original)!);
            File.Move(backup, original);
        }
    }
    Directory.Delete(backupDirectory, recursive: true);
    return Result.Success();
}

public async Task<Result> FinalizeAsync(string backupDirectory, CancellationToken ct)
{
    if (Directory.Exists(backupDirectory))
        Directory.Delete(backupDirectory, recursive: true);
    return Result.Success();
}
```

**UI:** after a successful clean, show a banner "X GB cleaned. Undo within 30s" with a countdown timer + Undo button. Use `DispatcherTimer` for the countdown.

**Files to create/modify:**
- [ ] Modify `SystemCleanupService.cs` (above pattern)
- [ ] Add `CleanupResultModel` to Core/Models
- [ ] Extend `SystemCleanupView.xaml` with undo banner
- [ ] Extend `SystemCleanupViewModel` with `UndoCommand` + countdown timer

**Acceptance criteria:**
- [ ] Clean shows preview of files before executing
- [ ] After clean, undo banner appears with 30s countdown
- [ ] Undo restores all files to original locations
- [ ] Countdown expires → backup folder is deleted
- [ ] Locked files are skipped silently (not counted in freed space)

---

# PART B — UI/UX Patterns to Adopt

## B1. Real-time WMI Performance Counters

SolasCarePro's Dashboard uses real WMI queries (not static mock data) for CPU/RAM/Disk/Network. SysAdminX's Dashboard already does this via `SystemHealthService` — but SolasCarePro's pattern of returning "Data Unavailable" gracefully on WMI failure is better than crashing.

**Action:** in `SystemHealthService.cs`, wrap every WMI query in try/catch and return `null` / `"Data Unavailable"` instead of throwing. Already partially done — verify all paths.

## B2. Live Command Output Console

SolasCarePro has a reusable `CommandOutput` component that shows a terminal-style live console with timestamps. Every long-running action streams its output there.

**Action:** create a reusable `CommandOutputControl` UserControl in `SysAdminX.Shell/Controls/`. Use it in:
- OneClickCare
- Troubleshooting
- PatchManager (install progress)
- Anywhere commands stream output

```xml
<controls:CommandOutputControl Output="{Binding CommandOutput}"
                                IsRunning="{Binding IsCommandRunning}"
                                AutoScroll="True" />
```

## B3. Notification Context (toast manager)

SolasCarePro has a centralized `NotificationContext` that any component can call to show a toast.

**Action:** SysAdminX should already have toast support (WPF-UI has `Toast`). Verify every action that needs feedback actually shows a toast. Audit:
- Driver update success/failure
- Patch install success/failure
- Service start/stop
- Any command execution

## B4. System Tray Integration

SolasCarePro minimizes to system tray during long-running SFC scans — the scan keeps running even if the user closes the window.

**Action:** add `NotifyIcon` to `MainWindow`. Behavior:
- Minimize button → hide window, show tray icon
- Double-click tray icon → restore window
- Right-click tray icon → context menu (Restore / Exit)
- Long-running operations keep running when minimized

Use `H.NotifyIcon.Wpf` NuGet package (modern NotifyIcon for WPF).

## B5. Compatibility Mode Banner (Windows 7/8)

SolasCarePro detects Windows version and shows a warning banner if running on < Windows 10. CMD fallbacks are auto-used for modern cmdlets.

**Action:** SysAdminX targets `net8.0-windows` which requires Windows 10+ — but if you ever downgrade the target, add the banner. Otherwise, this is N/A.

---

# PART C — Architectural Improvements

## C1. Embedded PowerShell Scripts (instead of file paths)

**Problem:** SysAdminX's `ExecuteScriptAsync` expects a file path — fragile, scripts get lost.

**Solution:** embed all `.ps1` scripts as resources in the Infrastructure project. At runtime, extract to `%TEMP%\sysadminx_scripts\<guid>\<name>.ps1` and pass that path.

```csharp
public static string ExtractEmbeddedScript(string scriptName)
{
    var assembly = Assembly.GetExecutingAssembly();
    var resourceName = $"SysAdminX.Infrastructure.Scripts.{scriptName}";
    var tempPath = Path.Combine(Path.GetTempPath(), "sysadminx_scripts", Guid.NewGuid().ToString("N"), scriptName);
    Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);
    using var stream = assembly.GetManifestResourceStream(resourceName);
    using var reader = new StreamReader(stream);
    File.WriteAllText(tempPath, reader.ReadToEnd());
    return tempPath;
}
```

Mark every `.ps1` file as `<EmbeddedResource>` in `SysAdminX.Infrastructure.csproj`.

## C2. Streaming Process Executor

Already covered in A1 — extend `IProcessExecutorService` with `ExecuteStreamingAsync(IProgress<string>)`.

## C3. Bug: ExecuteScriptAsync vs ExecuteCommandAsync

The root cause of the SecurityCenter bug (already in Part 2 of the fix document) is a misleading API name. SolasCarePro avoids this by having separate, clearly-named methods.

**Action:** rename in `IPowerShellService`:
- `ExecuteScriptAsync(string scriptPath)` → `ExecuteScriptFileAsync(string scriptPath)` (expects file path)
- Add `ExecuteScriptContentAsync(string scriptContent)` (writes content to temp file, then executes)
- `ExecuteCommandAsync(string command)` (existing — for one-liners)

This prevents the bug from recurring.

## C4. Centralized Settings Sync (Registry + JSON)

SolasCarePro syncs "Run at Windows Startup" toggle to `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`. SysAdminX's `SettingsService` only writes a JSON file.

**Action:** extend `SettingsViewModel` with "Run at Windows Startup" toggle. When toggled, write/delete the registry value (use existing `IRegistryService`).

## C5. Privilege Elevation Loop Prevention

SolasCarePro writes a timestamped flag to `%TEMP%\solas_relaunch.flag` before triggering UAC. If a relaunch is attempted again within 30 seconds, it stops and prompts the user to manually run as admin.

**Action:** add this to `App.xaml.cs` `OnStartup`. If the app needs elevation (detected via `WindowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator)`), check the flag first.

---

# PART D — Implementation Roadmap

## Phase 1 (Week 1) — Foundation
- A3 System Restore Point Manager (needed before any risky action)
- A1 One-Click Care Wizard (the flagship feature)
- B4 System Tray Integration
- C1 Embedded PowerShell Scripts refactor
- C3 Fix ExecuteScriptAsync naming

## Phase 2 (Week 2) — Scheduling & Drivers
- A2 Auto-Pilot Scheduler
- A4 Driver Manager Upgrade (PnP reset + registry backup)
- A15 Junk Cleanup with 30-second undo
- B2 Reusable CommandOutputControl

## Phase 3 (Week 3) — Cleaning & Diagnostics
- A5 Privacy Cleaner
- A6 Browser Repair Tool
- A7 BSOD Analyzer (extend LogsViewer)
- A12 SSD TRIM Optimizer (extend DeviceDetails)
- A13 Component Store Cleanup (extend SystemCleanup)

## Phase 4 (Week 4) — Power User Features
- A8 Performance Mode Profiles
- A9 Startup Manager
- A10 Large File Finder
- A11 Registry Manager
- A14 RAM Test Scheduler (extend Troubleshooting)
- B3 Toast audit + B5 Compatibility banner (if needed)

## Final Module Count

After all phases:
- **Original 17** modules (post-Part-1 fixes)
- **+8 new modules** (OneClickCare, AutoPilot, PrivacyCleaner, BrowserRepair, BsodAnalyzer, PerformanceMode, StartupManager, LargeFileFinder, RegistryManager — that's 9 actually, but BsodAnalyzer extends LogsViewer so 8 new + 1 extension)
- **4 extended modules** (DriverManager, DeviceDetails, SystemCleanup, LogsViewer, Troubleshooting)
- **Total: ~25 modules**

---

# Appendix — SolasCarePro Script Inventory (30 scripts)

Use these as ready-made PowerShell backends for the features above. Each script can be copy-pasted into `SysAdminX.Infrastructure/Scripts/` as an embedded resource.

| Script | Purpose | Use for feature |
|--------|---------|----------------|
| `activation_check.ps1` | Windows activation status | DeviceDetails |
| `analyze_bsod.ps1` | BSOD minidump parser | A7 BSOD Analyzer |
| `battery_report.ps1` | powercfg battery report | BatteryManager (existing) |
| `browser_reset.ps1` | Browser cache reset | A6 Browser Repair |
| `check_task_status.ps1` | Task Scheduler status | A2 Auto-Pilot |
| `check_windows_updates.ps1` | Windows Update scan | PatchManager (existing) |
| `component_cleanup.ps1` | DISM WinSxS cleanup | A13 Component Store |
| `create_restore_point.ps1` | System Restore point | A3 Restore Point Manager |
| `disk_health.ps1` | Disk SMART health | DeviceDetails (extend) |
| `enable_restore.ps1` | Enable System Protection | A3 Restore Point Manager |
| `generate_report.ps1` | HTML report generator | Reports (existing) |
| `get_drives_info.ps1` | Drive list + type (SSD/HDD) | A12 TRIM + DeviceDetails |
| `get_startup_apps.ps1` | Startup apps scan | A9 Startup Manager |
| `hardware_info.ps1` | Hardware inventory | DeviceDetails (existing) |
| `install_windows_updates.ps1` | Install updates | PatchManager (existing) |
| `iobit_one_click_care.ps1` | One-click care sequence | A1 One-Click Care |
| `junk_cleanup.ps1` | Junk file cleanup | A15 Junk Cleanup |
| `network_optimize.ps1` | Network reset + traffic check | NetworkToolkit (extend) |
| `ram_diagnostic.ps1` | RAM test result reader | A14 RAM Test |
| `registry_backup.ps1` | Registry backup/restore | A11 Registry Manager |
| `repair_driver.ps1` | Driver disable/enable/rollback | A4 Driver Manager Upgrade |
| `run_trim.ps1` | SSD TRIM optimization | A12 TRIM Optimizer |
| `scan_drivers.ps1` | Driver scan | DriverManager (existing) |
| `scan_software_updates.ps1` | winget upgrade list | PatchManager (existing) |
| `schedule_care.ps1` | Register scheduled task | A2 Auto-Pilot |
| `service_repair.ps1` | Service start/stop/diagnose | ServiceManager (extend) |
| `toggle_startup_app.ps1` | Enable/disable startup app | A9 Startup Manager |
| `unschedule_care.ps1` | Unregister scheduled task | A2 Auto-Pilot |
| `update_software.ps1` | winget upgrade install | PatchManager (existing) |
| `windows_info.ps1` | Windows OS info | DeviceDetails (existing) |

---

## Summary

- **Part A** adds 15 features (8 new modules + 7 extensions to existing modules)
- **Part B** adopts 5 UI/UX patterns
- **Part C** addresses 5 architectural improvements
- **Part D** gives a 4-week phased rollout plan
- **Appendix** maps 30 ready-made PowerShell scripts to features

After Part A + B + C, SysAdminX becomes a comprehensive sysadmin suite matching (and exceeding) SolasCarePro's capabilities — but in a native WPF app that runs as a single binary without Node.js/Electron overhead.

**Expected total effort:** 80–120 hours for one developer, spread across 4 weeks.

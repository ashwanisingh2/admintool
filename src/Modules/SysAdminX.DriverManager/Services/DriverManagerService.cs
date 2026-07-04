// -----------------------------------------------------------------------
// <copyright file="DriverManagerService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.DriverManager.Services;

/// <summary>
/// Implementation of <see cref="IDriverManagerService"/> using PnPUtil.
/// </summary>
public class DriverManagerService : IDriverManagerService
{
    private readonly ILogger<DriverManagerService> _logger;
    private readonly IProcessExecutorService _processService;

    public DriverManagerService(ILogger<DriverManagerService> logger, IProcessExecutorService processService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _processService = processService ?? throw new ArgumentNullException(nameof(processService));
    }

    public async Task<Result<List<DriverInfoModel>>> ScanDriversAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Starting Driver Scan using pnputil");

            // 1. Get 3rd party drivers (for versions, dates, providers)
            var driversResult = await _processService.ExecuteAsync("pnputil", "/enum-drivers", false, ct);
            if (!driversResult.IsSuccess)
                return Result<List<DriverInfoModel>>.Failure(driversResult.ErrorMessage ?? "Failed to enum drivers");

            var driverDict = ParseDrivers(driversResult.Value ?? "");

            // 2. Get active devices
            var devicesResult = await _processService.ExecuteAsync("pnputil", "/enum-devices", false, ct);
            if (!devicesResult.IsSuccess)
                return Result<List<DriverInfoModel>>.Failure(devicesResult.ErrorMessage ?? "Failed to enum devices");

            var result = ParseDevices(devicesResult.Value ?? "", driverDict);
            
            _logger.LogInformation("Driver Scan completed. Found {Count} devices.", result.Count);
            
            return Result<List<DriverInfoModel>>.Success(result);
        }
        catch (OperationCanceledException)
        {
            return Result<List<DriverInfoModel>>.Cancelled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Driver scan failed");
            return Result<List<DriverInfoModel>>.Failure(ex.Message, ex);
        }
    }

    public async Task<Result> ExportDriversAsync(string destinationFolder, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Exporting drivers to {Path}", destinationFolder);
            
            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }

            // requires elevation
            var result = await _processService.ExecuteAsync("pnputil", $"/export-driver * \"{destinationFolder}\"", true, ct);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("Driver export successful");
                return Result.Success();
            }
            
            _logger.LogError("Driver export failed: {Error}", result.ErrorMessage);
            return Result.Failure(result.ErrorMessage ?? "Unknown export error");
        }
        catch (OperationCanceledException)
        {
            return Result.Cancelled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Driver export failed exception");
            return Result.Failure(ex.Message, ex);
        }
    }

    private Dictionary<string, (string Version, string Date, string Provider)> ParseDrivers(string output)
    {
        var dict = new Dictionary<string, (string, string, string)>(StringComparer.OrdinalIgnoreCase);
        
        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        string currentName = "";
        string provider = "";
        string version = "";
        string date = "";

        foreach (var line in lines)
        {
            var parts = line.Split(new[] { ':' }, 2);
            if (parts.Length != 2) continue;

            var key = parts[0].Trim();
            var val = parts[1].Trim();

            if (key == "Published Name")
            {
                if (!string.IsNullOrEmpty(currentName))
                {
                    dict[currentName] = (version, date, provider);
                }
                currentName = val;
                provider = "";
                version = "";
                date = "";
            }
            else if (key == "Provider Name")
            {
                provider = val;
            }
            else if (key == "Driver Version")
            {
                // Format usually: "10/30/2023 7.0.22.3" or "04/16/2020 1.0.184.0"
                var vParts = val.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (vParts.Length == 2)
                {
                    date = vParts[0];
                    version = vParts[1];
                }
                else
                {
                    version = val;
                }
            }
        }

        if (!string.IsNullOrEmpty(currentName))
        {
            dict[currentName] = (version, date, provider);
        }

        return dict;
    }

    private List<DriverInfoModel> ParseDevices(string output, Dictionary<string, (string Version, string Date, string Provider)> driverDict)
    {
        var list = new List<DriverInfoModel>();
        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        DriverInfoModel? currentDevice = null;
        string tempDesc = "", tempClass = "", tempMfg = "", tempStatus = "", tempInf = "", tempHwid = "";

        foreach (var line in lines)
        {
            var parts = line.Split(new[] { ':' }, 2);
            if (parts.Length != 2) continue;

            var key = parts[0].Trim();
            var val = parts[1].Trim();

            if (key == "Instance ID")
            {
                if (currentDevice != null)
                {
                    currentDevice = EnrichDevice(currentDevice, tempInf, tempDesc, tempClass, tempMfg, tempStatus, tempHwid, driverDict);
                    list.Add(currentDevice);
                }
                currentDevice = new DriverInfoModel();
                tempDesc = ""; tempClass = ""; tempMfg = ""; tempStatus = ""; tempInf = ""; tempHwid = val;
            }
            else if (key == "Device Description") tempDesc = val;
            else if (key == "Class Name") tempClass = val;
            else if (key == "Manufacturer Name") tempMfg = val;
            else if (key == "Status") tempStatus = val;
            else if (key == "Driver Name") tempInf = val;
        }

        if (currentDevice != null)
        {
            currentDevice = EnrichDevice(currentDevice, tempInf, tempDesc, tempClass, tempMfg, tempStatus, tempHwid, driverDict);
            list.Add(currentDevice);
        }

        return list;
    }

    private DriverInfoModel EnrichDevice(DriverInfoModel baseModel, string inf, string desc, string cls, string mfg, string status, string hwid, Dictionary<string, (string Version, string Date, string Provider)> driverDict)
    {
        var model = baseModel with
        {
            DeviceName = desc,
            ClassName = cls,
            Manufacturer = mfg,
            Status = status,
            InfName = inf,
            HardwareId = hwid
        };

        if (!string.IsNullOrEmpty(inf) && driverDict.TryGetValue(inf, out var dInfo))
        {
            model = model with
            {
                Version = dInfo.Version,
                Date = dInfo.Date,
                Provider = dInfo.Provider
            };
        }
        else if (inf.StartsWith("machine.inf", StringComparison.OrdinalIgnoreCase) || 
                 inf.StartsWith("volume.inf", StringComparison.OrdinalIgnoreCase) ||
                 mfg.Contains("Microsoft"))
        {
            // Default built-in drivers usually don't show up in /enum-drivers
            model = model with
            {
                Version = "Built-in",
                Provider = "Microsoft"
            };
        }
        else if (string.IsNullOrEmpty(inf))
        {
            model = model with
            {
                Version = "None",
                Provider = "Unknown",
                Status = "No Driver"
            };
        }

        return model;
    }

    private async Task<string> ExtractScriptAsync()
    {
        var assembly = typeof(DriverManagerService).Assembly;
        var resourceName = "SysAdminX.DriverManager.Scripts.repair_driver.ps1";
        var tempPath = Path.Combine(Path.GetTempPath(), "repair_driver.ps1");
        
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) throw new FileNotFoundException($"Resource {resourceName} not found.");
        
        using var fileStream = File.Create(tempPath);
        await stream.CopyToAsync(fileStream);
        
        return tempPath;
    }

    public async Task<Result<string>> DisableDriverWithBackupAsync(string hardwareId, bool safeMode, CancellationToken ct = default)
    {
        try
        {
            var scriptPath = await ExtractScriptAsync();
            var safeModeStr = safeMode ? "$true" : "$false";
            var result = await _processService.ExecuteAsync("powershell", $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -Action disable -HardwareId \"{hardwareId}\" -SafeMode {safeModeStr}", true, ct);
            
            if (result.Value != null && result.Value.Contains("[SAFE_MODE_ABORT]"))
            {
                return Result<string>.Failure("Operation aborted: Registry backup failed and Safe Mode is enabled.");
            }
            if (!result.IsSuccess)
            {
                return Result<string>.Failure(result.ErrorMessage ?? "Failed to disable driver");
            }
            
            var backupPathLine = result.Value?.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                      .FirstOrDefault(l => l.StartsWith("BACKUP_PATH:"));
            var backupPath = backupPathLine != null ? backupPathLine.Substring("BACKUP_PATH:".Length) : string.Empty;
            
            return Result<string>.Success(backupPath);
        }
        catch (Exception ex)
        {
            return Result<string>.Failure(ex.Message, ex);
        }
    }

    public async Task<Result> EnableDriverAsync(string hardwareId, CancellationToken ct = default)
    {
        try
        {
            var scriptPath = await ExtractScriptAsync();
            var result = await _processService.ExecuteAsync("powershell", $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -Action enable -HardwareId \"{hardwareId}\"", true, ct);
            return result.IsSuccess ? Result.Success() : Result.Failure(result.ErrorMessage ?? "Failed to enable driver");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, ex);
        }
    }

    public async Task<Result> RollbackDriverAsync(string hardwareId, CancellationToken ct = default)
    {
        try
        {
            var scriptPath = await ExtractScriptAsync();
            var result = await _processService.ExecuteAsync("powershell", $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -Action rollback -HardwareId \"{hardwareId}\"", true, ct);
            return result.IsSuccess ? Result.Success() : Result.Failure(result.ErrorMessage ?? "Failed to rollback driver");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, ex);
        }
    }

    public async Task<Result> RestoreFromBackupAsync(string backupFilePath, CancellationToken ct = default)
    {
        try
        {
            var scriptPath = await ExtractScriptAsync();
            var result = await _processService.ExecuteAsync("powershell", $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -Action restore -BackupFilePath \"{backupFilePath}\"", true, ct);
            return result.IsSuccess ? Result.Success() : Result.Failure(result.ErrorMessage ?? "Failed to restore backup");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, ex);
        }
    }

    public async Task<Result<List<string>>> ScanDriverUpdatesAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Scanning for driver updates via Windows Update COM Object");

            string psScript = @"
$Session = New-Object -ComObject Microsoft.Update.Session
$Searcher = $Session.CreateUpdateSearcher()
$Searcher.Online = $true
$Result = $Searcher.Search(""IsInstalled=0 and Type='Driver'"")
foreach($update in $Result.Updates) {
    Write-Output $update.Title
}
";
            var result = await _processService.ExecuteAsync("powershell", $"-NoProfile -ExecutionPolicy Bypass -Command \"{psScript.Replace("\r\n", "\n").Replace("\"", "\\\"")}\"", true, ct);
            
            if (!result.IsSuccess)
            {
                return Result<List<string>>.Failure(result.ErrorMessage ?? "Failed to scan driver updates");
            }

            var updates = (result.Value ?? "").Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                              .Where(x => !string.IsNullOrWhiteSpace(x))
                                              .ToList();
                                              
            return Result<List<string>>.Success(updates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan driver updates");
            return Result<List<string>>.Failure(ex.Message, ex);
        }
    }

    public async Task<Result<bool>> InstallDriverUpdatesAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Installing driver updates via Windows Update COM Object");

            string psScriptPath = Path.Combine(Path.GetTempPath(), "SysAdminX_InstallDrivers.ps1");
            string psScript = @"
Write-Host 'Scanning for driver updates...' -ForegroundColor Cyan
$Session = New-Object -ComObject Microsoft.Update.Session
$Searcher = $Session.CreateUpdateSearcher()
$Searcher.Online = $true
$Result = $Searcher.Search(""IsInstalled=0 and Type='Driver'"")

if ($Result.Updates.Count -eq 0) {
    Write-Host 'No driver updates found.' -ForegroundColor Green
} else {
    Write-Host ""Found $($Result.Updates.Count) driver updates. Downloading..."" -ForegroundColor Yellow
    $Downloader = $Session.CreateUpdateDownloader()
    $Downloader.Updates = $Result.Updates
    $Downloader.Download()
    
    Write-Host 'Installing updates...' -ForegroundColor Yellow
    $Installer = $Session.CreateUpdateInstaller()
    $Installer.Updates = $Result.Updates
    $InstallResult = $Installer.Install()
    
    Write-Host 'Installation completed.' -ForegroundColor Green
    if ($InstallResult.RebootRequired) {
        Write-Host 'A reboot is required to finish installing drivers.' -ForegroundColor Red
    }
}
Write-Host 'Press any key to exit...'
$null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
";
            await File.WriteAllTextAsync(psScriptPath, psScript, ct);
            
            await _processService.ExecuteAsync("cmd.exe", $"/c powershell -NoProfile -ExecutionPolicy Bypass -File \"{psScriptPath}\"", true, ct);
            
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install driver updates");
            return Result<bool>.Failure(ex.Message, ex);
        }
    }

    public async Task<Result<List<DriverInfoModel>>> ScanUnsignedDriversAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Scanning for unsigned drivers using driverquery");

            var result = await _processService.ExecuteAsync("driverquery", "/si /FO CSV", false, ct);
            if (!result.IsSuccess)
            {
                return Result<List<DriverInfoModel>>.Failure(result.ErrorMessage ?? "Failed to run driverquery");
            }

            var list = new List<DriverInfoModel>();
            var lines = (result.Value ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            // Skip the header
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                var parts = line.Split(new[] { "\",\"" }, StringSplitOptions.None);
                if (parts.Length >= 4)
                {
                    // "DeviceName","InfName","IsSigned","Manufacturer"
                    var isSignedStr = parts[2].Trim('"').ToUpperInvariant();
                    if (isSignedStr == "FALSE")
                    {
                        var devName = parts[0].Trim('"');
                        var infName = parts[1].Trim('"');
                        var mfg = parts[3].Trim('"');

                        list.Add(new DriverInfoModel
                        {
                            DeviceName = devName,
                            InfName = infName,
                            Manufacturer = mfg,
                            Provider = mfg,
                            Status = "Unsigned",
                            Version = "Unknown",
                            ClassName = "Unknown"
                        });
                    }
                }
            }

            return Result<List<DriverInfoModel>>.Success(list);
        }
        catch (OperationCanceledException)
        {
            return Result<List<DriverInfoModel>>.Cancelled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan unsigned drivers");
            return Result<List<DriverInfoModel>>.Failure(ex.Message, ex);
        }
    }

    public Task<Result<OemUpdaterInfoModel?>> CheckOemUpdaterAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Checking for OEM update tools");

            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            var pathsToCheck = new[]
            {
                new { Name = "Dell Command Update", Path = Path.Combine(programFilesX86, "Dell", "CommandUpdate", "dcu-cli.exe") },
                new { Name = "Dell Command Update", Path = Path.Combine(programFiles, "Dell", "CommandUpdate", "dcu-cli.exe") },
                new { Name = "Lenovo System Update", Path = Path.Combine(programFilesX86, "Lenovo", "System Update", "tvsu.exe") },
                new { Name = "HP Support Assistant", Path = Path.Combine(programFilesX86, "Hewlett-Packard", "HP Support Framework", "HPSF.exe") }
            };

            foreach (var item in pathsToCheck)
            {
                if (File.Exists(item.Path))
                {
                    return Task.FromResult(Result<OemUpdaterInfoModel?>.Success(new OemUpdaterInfoModel
                    {
                        Name = item.Name,
                        ExecutablePath = item.Path,
                        IsInstalled = true
                    }));
                }
            }
            
            return Task.FromResult(Result<OemUpdaterInfoModel?>.Success(null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check OEM updaters");
            return Task.FromResult(Result<OemUpdaterInfoModel?>.Failure(ex.Message, ex));
        }
    }
}

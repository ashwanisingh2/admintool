// -----------------------------------------------------------------------
// <copyright file="TroubleshootingService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.Troubleshooting.Services;

/// <summary>
/// Implementation of <see cref="ITroubleshootingService"/>.
/// </summary>
public class TroubleshootingService : ITroubleshootingService
{
    private readonly ILogger<TroubleshootingService> _logger;
    private readonly IProcessExecutorService _processService;
    private readonly IPowerShellService _powerShellService;

    public TroubleshootingService(ILogger<TroubleshootingService> logger, IProcessExecutorService processService, IPowerShellService powerShellService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _processService = processService ?? throw new ArgumentNullException(nameof(processService));
        _powerShellService = powerShellService ?? throw new ArgumentNullException(nameof(powerShellService));
    }

    public async Task<Result<TroubleshootingActionModel>> RunSfcScanAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Launching SFC Scan");
        // sfc /scannow needs elevation
        // We execute cmd.exe /k to keep the window open so the user can see the result
        var result = await _processService.ExecuteAsync("cmd.exe", "/c sfc /scannow", true, ct);
        
        return Result<TroubleshootingActionModel>.Success(new TroubleshootingActionModel
        {
            ActionName = "System File Checker",
            Description = "Scans and repairs corrupted Windows system files.",
            IsSuccess = result.IsSuccess,
            OutputMessage = result.IsSuccess ? "SFC Scan completed. Check the external console window for details." : result.ErrorMessage ?? "Failed to launch SFC."
        });
    }

    public async Task<Result<TroubleshootingActionModel>> RunDismCheckHealthAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Launching DISM CheckHealth");
        var result = await _processService.ExecuteAsync("cmd.exe", "/c DISM /Online /Cleanup-Image /CheckHealth", true, ct);
        
        return Result<TroubleshootingActionModel>.Success(new TroubleshootingActionModel
        {
            ActionName = "DISM Check Health",
            Description = "Checks whether the image has been flagged as corrupted.",
            IsSuccess = result.IsSuccess,
            OutputMessage = result.IsSuccess ? "DISM Check completed. Check the external console window for details." : result.ErrorMessage ?? "Failed to launch DISM."
        });
    }

    public async Task<Result<TroubleshootingActionModel>> RunDismRestoreHealthAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Launching DISM RestoreHealth");
        var result = await _processService.ExecuteAsync("cmd.exe", "/c DISM /Online /Cleanup-Image /RestoreHealth", true, ct);
        
        return Result<TroubleshootingActionModel>.Success(new TroubleshootingActionModel
        {
            ActionName = "DISM Restore Health",
            Description = "Repairs the Windows image using Windows Update.",
            IsSuccess = result.IsSuccess,
            OutputMessage = result.IsSuccess ? "DISM Restore completed. Check the external console window for details." : result.ErrorMessage ?? "Failed to launch DISM."
        });
    }

    public async Task<Result<TroubleshootingActionModel>> ClearTempFilesAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Clearing Temp Files");
        
        return await Task.Run(() =>
        {
            try
            {
                long bytesFreed = 0;
                string[] tempPaths = {
                    Path.GetTempPath(),
                    Environment.GetEnvironmentVariable("TEMP", EnvironmentVariableTarget.Machine) ?? @"C:\Windows\Temp"
                };

                foreach (var path in tempPaths)
                {
                    if (Directory.Exists(path))
                    {
                        var di = new DirectoryInfo(path);
                        foreach (var file in di.GetFiles())
                        {
                            try
                            {
                                bytesFreed += file.Length;
                                file.Delete();
                            }
                            catch { /* Ignore locked files */ }
                        }
                        foreach (var dir in di.GetDirectories())
                        {
                            try { dir.Delete(true); }
                            catch { /* Ignore locked dirs */ }
                        }
                    }
                }

                double mbFreed = bytesFreed / 1024.0 / 1024.0;

                return Result<TroubleshootingActionModel>.Success(new TroubleshootingActionModel
                {
                    ActionName = "Clear Temporary Files",
                    Description = "Deletes files in user and system temp directories.",
                    IsSuccess = true,
                    OutputMessage = $"Successfully freed {mbFreed:F2} MB of temporary files."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear temp files");
                return Result<TroubleshootingActionModel>.Failure(ex.Message, ex);
            }
        }, ct);
    }

    public async Task<Result<TroubleshootingActionModel>> ToggleFastStartupAsync(bool enable, CancellationToken ct = default)
    {
        _logger.LogInformation("Toggling Fast Startup to {State}", enable);
        
        string val = enable ? "1" : "0";
        var result = await _processService.ExecuteAsync("powercfg", $"/h {(enable ? "on" : "off")}", true, ct);

        if (result.IsSuccess)
        {
            // Set registry key for hiberboot
            await _processService.ExecuteAsync("reg", $@"add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power"" /v HiberbootEnabled /t REG_DWORD /d {val} /f", true, ct);
        }

        return Result<TroubleshootingActionModel>.Success(new TroubleshootingActionModel
        {
            ActionName = enable ? "Enable Fast Startup" : "Disable Fast Startup",
            Description = "Toggles Windows Fast Startup (Hybrid Sleep).",
            IsSuccess = result.IsSuccess,
            OutputMessage = result.IsSuccess ? $"Fast Startup has been {(enable ? "enabled" : "disabled")}." : result.ErrorMessage ?? "Failed to toggle Fast Startup."
        });
    }

    public async Task<Result<TroubleshootingActionModel>> RunChkdskAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Launching CHKDSK");
        var result = await _processService.ExecuteAsync("cmd.exe", "/c echo y | chkdsk C: /f /r", true, ct);
        
        return Result<TroubleshootingActionModel>.Success(new TroubleshootingActionModel
        {
            ActionName = "Schedule CHKDSK",
            Description = "Schedules a disk check on the next restart.",
            IsSuccess = result.IsSuccess,
            OutputMessage = result.IsSuccess ? "CHKDSK scheduled for next reboot. Check the external console window for details." : result.ErrorMessage ?? "Failed to schedule CHKDSK."
        });
    }

    public async Task<Result<TroubleshootingActionModel>> ResetWindowsUpdateAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Resetting Windows Update components");
        string cmds = "net stop wuauserv & net stop bits & net stop cryptSvc & net stop msiserver & " +
                      "ren C:\\Windows\\SoftwareDistribution SoftwareDistribution.old & " +
                      "ren C:\\Windows\\System32\\catroot2 catroot2.old & " +
                      "net start wuauserv & net start bits & net start cryptSvc & net start msiserver";
        var result = await _processService.ExecuteAsync("cmd.exe", $"/c {cmds}", true, ct);
        
        return Result<TroubleshootingActionModel>.Success(new TroubleshootingActionModel
        {
            ActionName = "Reset Windows Update",
            Description = "Stops WU services, renames cache folders, and restarts services.",
            IsSuccess = result.IsSuccess,
            OutputMessage = result.IsSuccess ? "Windows Update components reset successfully. Check console for details." : result.ErrorMessage ?? "Failed to reset Windows Update."
        });
    }

    public async Task<Result<TroubleshootingActionModel>> FixPrintSpoolerAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Fixing Print Spooler");
        string cmds = "net stop spooler & del /Q /F /S \"%systemroot%\\System32\\Spool\\Printers\\*.*\" & net start spooler";
        var result = await _processService.ExecuteAsync("cmd.exe", $"/c {cmds}", true, ct);
        
        return Result<TroubleshootingActionModel>.Success(new TroubleshootingActionModel
        {
            ActionName = "Fix Print Spooler",
            Description = "Stops spooler service, clears stuck print jobs, and restarts service.",
            IsSuccess = result.IsSuccess,
            OutputMessage = result.IsSuccess ? "Print spooler cleared and restarted." : result.ErrorMessage ?? "Failed to fix print spooler."
        });
    }

    public async Task<Result<TroubleshootingActionModel>> FlushDnsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Flushing DNS Cache");
        var result = await _processService.ExecuteAsync("cmd.exe", "/c ipconfig /flushdns", true, ct);
        
        return Result<TroubleshootingActionModel>.Success(new TroubleshootingActionModel
        {
            ActionName = "Flush DNS Cache",
            Description = "Clears the DNS resolver cache.",
            IsSuccess = result.IsSuccess,
            OutputMessage = result.IsSuccess ? "DNS cache successfully flushed." : result.ErrorMessage ?? "Failed to flush DNS cache."
        });
    }

    public async Task<Result<TroubleshootingActionModel>> ResetWinsockAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Resetting Winsock");
        var result = await _processService.ExecuteAsync("cmd.exe", "/c netsh winsock reset", true, ct);
        
        return Result<TroubleshootingActionModel>.Success(new TroubleshootingActionModel
        {
            ActionName = "Reset Winsock",
            Description = "Resets the Windows Sockets API.",
            IsSuccess = result.IsSuccess,
            OutputMessage = result.IsSuccess ? "Winsock reset successfully. A restart is required." : result.ErrorMessage ?? "Failed to reset Winsock."
        });
    }

    public async Task<Result<TroubleshootingActionModel>> ResetTcpIpAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Resetting TCP/IP stack");
        var result = await _processService.ExecuteAsync("cmd.exe", "/c netsh int ip reset", true, ct);
        
        return Result<TroubleshootingActionModel>.Success(new TroubleshootingActionModel
        {
            ActionName = "Reset TCP/IP",
            Description = "Resets TCP/IP stack to default state.",
            IsSuccess = result.IsSuccess,
            OutputMessage = result.IsSuccess ? "TCP/IP reset successfully. A restart is required." : result.ErrorMessage ?? "Failed to reset TCP/IP."
        });
    }

    public async Task<Result<TroubleshootingActionModel>> RebuildIconCacheAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Rebuilding Icon Cache");
        string cmds = "ie4uinit.exe -show & taskkill /IM explorer.exe /F & " +
                      "del /A /Q \"%localappdata%\\IconCache.db\" & " +
                      "del /A /F /Q \"%localappdata%\\Microsoft\\Windows\\Explorer\\iconcache*\" & " +
                      "del /A /F /Q \"%localappdata%\\Microsoft\\Windows\\Explorer\\thumbcache*\" & " +
                      "start explorer.exe";
        
        // This is safe to run without wait for the whole thing (explorer restart), but we'll wait.
        var result = await _processService.ExecuteAsync("cmd.exe", $"/c {cmds}", true, ct);
        
        return Result<TroubleshootingActionModel>.Success(new TroubleshootingActionModel
        {
            ActionName = "Rebuild Icon/Thumbnail Cache",
            Description = "Clears and rebuilds corrupted icon and thumbnail caches.",
            IsSuccess = result.IsSuccess,
            OutputMessage = result.IsSuccess ? "Icon cache rebuilt successfully. Explorer was restarted." : result.ErrorMessage ?? "Failed to rebuild icon cache."
        });
    }

    public async Task<Result<TroubleshootingActionModel>> ResetWindowsSearchAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Resetting Windows Search Index");
        string cmds = "net stop WSearch & " +
                      "rd /s /q \"%ProgramData%\\Microsoft\\Search\\Data\\Applications\\Windows\" & " +
                      "net start WSearch";
        var result = await _processService.ExecuteAsync("cmd.exe", $"/c {cmds}", true, ct);
        
        return Result<TroubleshootingActionModel>.Success(new TroubleshootingActionModel
        {
            ActionName = "Reset Windows Search",
            Description = "Deletes search index database and restarts search service.",
            IsSuccess = result.IsSuccess,
            OutputMessage = result.IsSuccess ? "Windows Search index reset successfully." : result.ErrorMessage ?? "Failed to reset Windows Search."
        });
    }

    private async Task<string> ExtractScriptAsync(string resourceName)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.ps1");
        using var stream = typeof(TroubleshootingService).Assembly.GetManifestResourceStream(resourceName);
        if (stream == null) throw new FileNotFoundException($"Embedded resource {resourceName} not found.");
        using var fileStream = File.Create(tempFile);
        await stream.CopyToAsync(fileStream);
        return tempFile;
    }

    public async Task<Result<TroubleshootingActionModel>> ScheduleRamTestAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Scheduling RAM Test");
        string tempFile = null;
        try
        {
            tempFile = await ExtractScriptAsync("SysAdminX.Troubleshooting.Scripts.ram_diagnostic.ps1");
            var parameters = new System.Collections.Generic.Dictionary<string, object>
            {
                { "Action", "Schedule" }
            };
            var result = await _powerShellService.ExecuteScriptFileAsync(tempFile, parameters, ct);
            return Result<TroubleshootingActionModel>.Success(new TroubleshootingActionModel
            {
                ActionName = "Schedule RAM Test",
                Description = "Schedules Windows Memory Diagnostic on next restart.",
                IsSuccess = result.IsSuccess,
                OutputMessage = result.IsSuccess ? "Windows Memory Diagnostic tool launched." : result.ErrorMessage ?? "Failed to launch MdSched."
            });
        }
        finally
        {
            if (tempFile != null && File.Exists(tempFile))
            {
                try { File.Delete(tempFile); } catch { }
            }
        }
    }

    public async Task<Result<string>> CheckRamTestResultAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Checking RAM Test Result");
        string tempFile = null;
        try
        {
            tempFile = await ExtractScriptAsync("SysAdminX.Troubleshooting.Scripts.ram_diagnostic.ps1");
            var parameters = new System.Collections.Generic.Dictionary<string, object>
            {
                { "Action", "CheckResult" }
            };
            var result = await _powerShellService.ExecuteScriptFileAsync(tempFile, parameters, ct);
            if (!result.IsSuccess)
            {
                return Result<string>.Failure(result.ErrorMessage);
            }
            
            // Output might have newlines from powershell, trim it
            var output = result.Value?.Trim();
            
            if (string.IsNullOrEmpty(output))
                return Result<string>.Success("No results found");
                
            return Result<string>.Success(output);
        }
        finally
        {
            if (tempFile != null && File.Exists(tempFile))
            {
                try { File.Delete(tempFile); } catch { }
            }
        }
    }
}

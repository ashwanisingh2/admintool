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

    public TroubleshootingService(ILogger<TroubleshootingService> logger, IProcessExecutorService processService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _processService = processService ?? throw new ArgumentNullException(nameof(processService));
    }

    public async Task<Result<TroubleshootingActionModel>> RunSfcScanAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Launching SFC Scan");
        // sfc /scannow needs elevation
        // We execute cmd.exe /k to keep the window open so the user can see the result
        var result = await _processService.ExecuteAsync("cmd.exe", "/c sfc /scannow & pause", true, ct);
        
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
        var result = await _processService.ExecuteAsync("cmd.exe", "/c DISM /Online /Cleanup-Image /CheckHealth & pause", true, ct);
        
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
        var result = await _processService.ExecuteAsync("cmd.exe", "/c DISM /Online /Cleanup-Image /RestoreHealth & pause", true, ct);
        
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
}

// -----------------------------------------------------------------------
// <copyright file="PatchManagerService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.PatchManager.Services;

/// <summary>
/// Implementation of <see cref="IPatchManagerService"/> using WMI (Win32_QuickFixEngineering).
/// </summary>
public class PatchManagerService : IPatchManagerService
{
    private readonly ILogger<PatchManagerService> _logger;
    private readonly IWmiService _wmiService;
    private readonly IProcessExecutorService _processService;

    public PatchManagerService(ILogger<PatchManagerService> logger, IWmiService wmiService, IProcessExecutorService processService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _wmiService = wmiService ?? throw new ArgumentNullException(nameof(wmiService));
        _processService = processService ?? throw new ArgumentNullException(nameof(processService));
    }

    public async Task<Result<List<UpdateInfoModel>>> GetInstalledUpdatesAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Querying installed updates via WMI");

            var result = await _wmiService.QueryAsync("SELECT * FROM Win32_QuickFixEngineering", ct);
            if (!result.IsSuccess || result.Value == null)
            {
                return Result<List<UpdateInfoModel>>.Failure(result.ErrorMessage ?? "Failed to query updates", result.Exception);
            }

            var updates = new List<UpdateInfoModel>();

            foreach (var item in result.Value)
            {
                var update = new UpdateInfoModel
                {
                    HotFixId = item.TryGetValue("HotFixID", out var id) ? id?.ToString() ?? "Unknown" : "Unknown",
                    Description = item.TryGetValue("Description", out var desc) ? desc?.ToString() ?? "Update" : "Update",
                    InstalledBy = item.TryGetValue("InstalledBy", out var by) ? by?.ToString() ?? "" : "",
                    InstalledOn = item.TryGetValue("InstalledOn", out var on) ? on?.ToString() ?? "" : "",
                    Source = "WMI"
                };

                // Some WMI versions return hexadecimal string for date or mm/dd/yyyy. Let's keep it as string for display.
                updates.Add(update);
            }

            _logger.LogInformation("Found {Count} installed updates", updates.Count);
            return Result<List<UpdateInfoModel>>.Success(updates);
        }
        catch (OperationCanceledException)
        {
            return Result<List<UpdateInfoModel>>.Cancelled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get installed updates");
            return Result<List<UpdateInfoModel>>.Failure(ex.Message, ex);
        }
    }

    public async Task<Result<List<SoftwarePackageModel>>> GetSoftwareUpdatesAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Scanning for software updates using winget");

            var result = await _processService.ExecuteAsync("winget", "upgrade --accept-source-agreements --accept-package-agreements", requireElevation: false, ct: ct);
            
            // It might return failure if there's an exit code, but we still want to parse Value if available
            var output = result.Value ?? string.Empty;

            var packages = new List<SoftwarePackageModel>();
            var lines = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            bool isDataStarted = false;
            foreach (var line in lines)
            {
                if (line.StartsWith("Name") && line.Contains("Id") && line.Contains("Version"))
                {
                    isDataStarted = true;
                    continue;
                }
                
                if (line.StartsWith("---") || string.IsNullOrWhiteSpace(line)) continue;

                if (isDataStarted)
                {
                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 4)
                    {
                        var package = new SoftwarePackageModel
                        {
                            Name = string.Join(" ", parts.Take(parts.Length - 3)),
                            Id = parts[parts.Length - 3],
                            Version = parts[parts.Length - 2],
                            AvailableVersion = parts[parts.Length - 1]
                        };
                        packages.Add(package);
                    }
                }
            }

            _logger.LogInformation("Found {Count} software updates available", packages.Count);
            return Result<List<SoftwarePackageModel>>.Success(packages);
        }
        catch (OperationCanceledException)
        {
            return Result<List<SoftwarePackageModel>>.Cancelled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get software updates");
            return Result<List<SoftwarePackageModel>>.Failure(ex.Message, ex);
        }
    }

    public async Task<Result<bool>> UpgradeAllSoftwareAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Upgrading all software using winget");
            // Run interactive so the user can see the progress of winget
            var result = await _processService.ExecuteAsync("cmd.exe", "/c winget upgrade --all --accept-source-agreements --accept-package-agreements & pause", requireElevation: true, ct: ct);
            
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upgrade all software");
            return Result<bool>.Failure(ex.Message, ex);
        }
    }
}

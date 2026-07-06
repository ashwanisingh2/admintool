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
            var result = await _processService.ExecuteAsync("cmd.exe", "/c winget upgrade --all --accept-source-agreements --accept-package-agreements", requireElevation: true, ct: ct);
            
            return Result<bool>.Success(result.IsSuccess);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upgrade all software");
            return Result<bool>.Failure(ex.Message, ex);
        }
    }

    public async Task<Result<List<MissingUpdateModel>>> GetMissingUpdatesAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Scanning for missing Windows Updates");
            
            string psScript = @"
$updateSession = New-Object -ComObject Microsoft.Update.Session
$updateSearcher = $updateSession.CreateUpdateSearcher()
$searchResult = $updateSearcher.Search(""IsInstalled=0 and Type='Software' and IsHidden=0"")
$updates = $searchResult.Updates
$result = @()
for ($i = 0; $i -lt $updates.Count; $i++) {
    $update = $updates.Item($i)
    $kbArticles = @()
    foreach ($kb in $update.KBArticleIDs) {
        $kbArticles += 'KB' + $kb
    }
    $result += [PSCustomObject]@{
        Title = $update.Title
        Description = $update.Description
        KBArticles = $kbArticles -join ', '
        IsDownloaded = $update.IsDownloaded
    }
}
@($result) | ConvertTo-Json -Depth 2 -Compress
";
            string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ScanUpdates.ps1");
            await System.IO.File.WriteAllTextAsync(tempFile, psScript, ct);
            
            var result = await _processService.ExecuteAsync("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -File \"{tempFile}\"", requireElevation: false, ct: ct);
            
            if (System.IO.File.Exists(tempFile))
            {
                System.IO.File.Delete(tempFile);
            }

            if (!result.IsSuccess)
            {
                return Result<List<MissingUpdateModel>>.Failure(result.ErrorMessage ?? "Failed to scan updates");
            }

            var output = result.Value?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(output) || output == "null")
            {
                return Result<List<MissingUpdateModel>>.Success(new List<MissingUpdateModel>());
            }

            var updates = System.Text.Json.JsonSerializer.Deserialize<List<MissingUpdateModel>>(output);
            return Result<List<MissingUpdateModel>>.Success(updates ?? new List<MissingUpdateModel>());
        }
        catch (OperationCanceledException)
        {
            return Result<List<MissingUpdateModel>>.Cancelled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get missing updates");
            return Result<List<MissingUpdateModel>>.Failure(ex.Message, ex);
        }
    }

    public async Task<Result<InstallUpdatesResultModel>> InstallMissingUpdatesAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Installing missing Windows Updates");
            
            string resultFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $@"InstallUpdateResult_{Guid.NewGuid()}.json");
            string safeResultFile = resultFile.Replace("'", "''");
            
            string psScript = $@"
$updateSession = New-Object -ComObject Microsoft.Update.Session
$updateSearcher = $updateSession.CreateUpdateSearcher()
Write-Host 'Searching for missing updates...' -ForegroundColor Cyan
$searchResult = $updateSearcher.Search(""IsInstalled=0 and Type='Software' and IsHidden=0"")
$updates = $searchResult.Updates

if ($updates.Count -eq 0) {{
    Write-Host 'No updates found to install.' -ForegroundColor Green
    $json = @{{ RebootRequired = $false; ResultCode = 2 }}
    $json | ConvertTo-Json -Compress | Out-File -FilePath '{safeResultFile}' -Encoding UTF8
    Start-Sleep -Seconds 2
    exit 0
}}

Write-Host ""Found $($updates.Count) updates. Starting download..."" -ForegroundColor Cyan
$downloader = $updateSession.CreateUpdateDownloader()
$downloader.Updates = $updates
$downloader.Download()

Write-Host 'Download complete. Starting installation...' -ForegroundColor Cyan
$installer = $updateSession.CreateUpdateInstaller()
$installer.Updates = $updates
$installResult = $installer.Install()

Write-Host 'Installation complete.' -ForegroundColor Green
$json = @{{
    RebootRequired = $installResult.RebootRequired
    ResultCode = $installResult.ResultCode
}}
$json | ConvertTo-Json -Compress | Out-File -FilePath '{safeResultFile}' -Encoding UTF8

";
            string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "InstallUpdates.ps1");
            await System.IO.File.WriteAllTextAsync(tempFile, psScript, ct);
            
            var result = await _processService.ExecuteAsync("cmd.exe", $"/c powershell.exe -NoProfile -ExecutionPolicy Bypass -File \"{tempFile}\"", requireElevation: true, ct: ct);
            
            if (System.IO.File.Exists(tempFile))
            {
                System.IO.File.Delete(tempFile);
            }

            if (!System.IO.File.Exists(resultFile))
            {
                return Result<InstallUpdatesResultModel>.Failure("Installation result file not found. Process may have failed or been cancelled.");
            }

            var jsonContent = await System.IO.File.ReadAllTextAsync(resultFile, ct);
            System.IO.File.Delete(resultFile);

            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var installResult = System.Text.Json.JsonSerializer.Deserialize<InstallUpdatesResultModel>(jsonContent, options);
            return Result<InstallUpdatesResultModel>.Success(installResult ?? new InstallUpdatesResultModel());
        }
        catch (OperationCanceledException)
        {
            return Result<InstallUpdatesResultModel>.Cancelled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install missing updates");
            return Result<InstallUpdatesResultModel>.Failure(ex.Message, ex);
        }
    }
}

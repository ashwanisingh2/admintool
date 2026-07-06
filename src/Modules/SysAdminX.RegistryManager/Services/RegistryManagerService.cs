using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;
using SysAdminX.RegistryManager.Models;

namespace SysAdminX.RegistryManager.Services;



public class RegistryManagerService : IRegistryManagerService
{
    private readonly IPowerShellService _powerShellService;
    private readonly ILogger<RegistryManagerService> _logger;
    private readonly string _backupDir;
    private readonly string _metadataFile;

    public RegistryManagerService(IPowerShellService powerShellService, ILogger<RegistryManagerService> logger)
    {
        _powerShellService = powerShellService;
        _logger = logger;
        
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _backupDir = Path.Combine(appData, "SysAdminX", "RegBackups");
        _metadataFile = Path.Combine(_backupDir, "backups.json");
    }

    private async Task<string> GetScriptContentAsync()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "SysAdminX.RegistryManager.Scripts.registry_backup.ps1";
        
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) throw new FileNotFoundException($"Resource {resourceName} not found.");
        
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    private async Task SaveMetadataAsync(List<RegistryBackupModel> backups)
    {
        if (!Directory.Exists(_backupDir))
        {
            Directory.CreateDirectory(_backupDir);
        }
        var json = JsonSerializer.Serialize(backups);
        await File.WriteAllTextAsync(_metadataFile, json);
    }

    private async Task<List<RegistryBackupModel>> LoadMetadataAsync()
    {
        if (!File.Exists(_metadataFile))
        {
            return new List<RegistryBackupModel>();
        }
        var json = await File.ReadAllTextAsync(_metadataFile);
        return JsonSerializer.Deserialize<List<RegistryBackupModel>>(json) ?? new List<RegistryBackupModel>();
    }

    public async Task<Result<RegistryBackupModel>> CreateBackupAsync(string label, CancellationToken ct = default)
    {
        try
        {
            var script = await GetScriptContentAsync();
            var parameters = new Dictionary<string, object>
            {
                { "Action", "Backup" },
                { "BackupDir", _backupDir }
            };

            var result = await _powerShellService.ExecuteScriptContentAsync(script, parameters, ct);
            if (!result.IsSuccess) return Result<RegistryBackupModel>.Failure(result.ErrorMessage ?? "Unknown error");

            var outputLines = result.Value.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var successLine = outputLines.FirstOrDefault(l => l.StartsWith("SUCCESS|"));
            if (successLine == null) return Result<RegistryBackupModel>.Failure("Unexpected output from backup script.");

            var parts = successLine.Split('|');
            var backup = new RegistryBackupModel
            {
                Timestamp = parts[1],
                Label = label,
                HklmFilePath = parts[2].TrimEnd('\r'),
                HklmSizeBytes = long.Parse(parts[3]),
                HkcuFilePath = parts[4].TrimEnd('\r'),
                HkcuSizeBytes = long.Parse(parts[5]),
                CreatedAt = DateTime.Now
            };

            var backups = await LoadMetadataAsync();
            backups.Add(backup);
            await SaveMetadataAsync(backups);

            return Result<RegistryBackupModel>.Success(backup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create registry backup.");
            return Result<RegistryBackupModel>.Failure(ex.Message);
        }
    }

    public async Task<Result<List<RegistryBackupModel>>> GetBackupsAsync(CancellationToken ct = default)
    {
        try
        {
            var backups = await LoadMetadataAsync();
            return Result<List<RegistryBackupModel>>.Success(backups.OrderByDescending(b => b.CreatedAt).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get registry backups.");
            return Result<List<RegistryBackupModel>>.Failure(ex.Message);
        }
    }

    public async Task<Result> RestoreBackupAsync(string filePath, CancellationToken ct = default)
    {
        try
        {
            var script = await GetScriptContentAsync();
            var parameters = new Dictionary<string, object>
            {
                { "Action", "Restore" },
                { "BackupFile", filePath }
            };

            var result = await _powerShellService.ExecuteScriptContentAsync(script, parameters, ct);
            if (!result.IsSuccess) return Result.Failure(result.ErrorMessage ?? "Unknown error");

            if (!result.Value.Contains("SUCCESS")) return Result.Failure("Unexpected output during restore.");

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore registry backup.");
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result> DeleteBackupAsync(RegistryBackupModel backup, CancellationToken ct = default)
    {
        try
        {
            if (File.Exists(backup.HklmFilePath)) File.Delete(backup.HklmFilePath);
            if (File.Exists(backup.HkcuFilePath)) File.Delete(backup.HkcuFilePath);

            var backups = await LoadMetadataAsync();
            var toRemove = backups.FirstOrDefault(b => b.Timestamp == backup.Timestamp);
            if (toRemove != null)
            {
                backups.Remove(toRemove);
                await SaveMetadataAsync(backups);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete registry backup.");
            return Result.Failure(ex.Message);
        }
    }

    public void OpenBackupFolder()
    {
        if (Directory.Exists(_backupDir))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _backupDir,
                UseShellExecute = true,
                Verb = "open"
            });
        }
    }
}

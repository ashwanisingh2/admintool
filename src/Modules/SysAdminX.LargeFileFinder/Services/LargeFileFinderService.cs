using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;
using SysAdminX.LargeFileFinder.Models;

namespace SysAdminX.LargeFileFinder.Services;

public class LargeFileFinderService : ILargeFileFinderService
{
    private readonly IPowerShellService _powerShellService;
    private readonly ILogger<LargeFileFinderService> _logger;

    public LargeFileFinderService(IPowerShellService powerShellService, ILogger<LargeFileFinderService> logger)
    {
        _powerShellService = powerShellService;
        _logger = logger;
    }

    public async Task<Result<List<LargeFileModel>>> ScanFilesAsync(string drive, int minSizeMB, Action<string>? progressCallback = null, CancellationToken ct = default)
    {
        try
        {
            progressCallback?.Invoke($"Scanning {drive} for files > {minSizeMB}MB...");

            var scriptContent = await _powerShellService.ExtractEmbeddedScriptAsync("SysAdminX.LargeFileFinder.Scripts.scan_large_files.ps1", ct);
            
            var parameters = new Dictionary<string, object>
            {
                { "Drive", drive },
                { "MinSizeMB", minSizeMB }
            };

            var result = await _powerShellService.ExecuteScriptContentAsync(scriptContent, parameters, ct);
            
            if (!result.IsSuccess)
            {
                return Result<List<LargeFileModel>>.Failure(result.ErrorMessage ?? "Failed to scan files.");
            }

            if (string.IsNullOrWhiteSpace(result.Data))
            {
                return Result<List<LargeFileModel>>.Success(new List<LargeFileModel>());
            }

            var files = JsonSerializer.Deserialize<List<LargeFileModel>>(result.Data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            return Result<List<LargeFileModel>>.Success(files ?? new List<LargeFileModel>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning large files on {Drive}", drive);
            return Result<List<LargeFileModel>>.Failure(ex.Message);
        }
    }

    public Task<Result> DeleteFileAsync(string filePath, CancellationToken ct = default)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FilePath}", filePath);
            return Task.FromResult(Result.Failure(ex.Message));
        }
    }

    public Task<Result> MoveFileAsync(string sourcePath, string destinationPath, CancellationToken ct = default)
    {
        try
        {
            if (File.Exists(sourcePath))
            {
                File.Move(sourcePath, destinationPath);
            }
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving file {SourcePath} to {DestinationPath}", sourcePath, destinationPath);
            return Task.FromResult(Result.Failure(ex.Message));
        }
    }
}

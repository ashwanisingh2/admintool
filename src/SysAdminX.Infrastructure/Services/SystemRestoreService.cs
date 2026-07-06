using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.Infrastructure.Services;

public class SystemRestoreService : ISystemRestoreService
{
    private readonly IPowerShellService _powerShellService;
    private readonly IProcessExecutorService _processExecutorService;
    private readonly ILogger<SystemRestoreService> _logger;

    public SystemRestoreService(
        IPowerShellService powerShellService,
        IProcessExecutorService processExecutorService,
        ILogger<SystemRestoreService> logger)
    {
        _powerShellService = powerShellService;
        _processExecutorService = processExecutorService;
        _logger = logger;
    }

    private async Task<string> GetScriptContentAsync(CancellationToken ct)
    {
        return await _powerShellService.ExtractEmbeddedScriptAsync("system_restore.ps1", ct);
    }

    public async Task<Result<bool>> IsProtectionEnabledAsync(CancellationToken ct)
    {
        try
        {
            var script = await GetScriptContentAsync(ct);
            var parameters = new Dictionary<string, object> { { "Action", "CheckStatus" } };
            var result = await _powerShellService.ExecuteScriptContentAsync(script, parameters, ct);
            if (!result.IsSuccess) return Result<bool>.Failure(result.ErrorMessage);

            using var doc = JsonDocument.Parse(result.Value);
            return Result<bool>.Success(doc.RootElement.GetProperty("isEnabled").GetBoolean());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check system restore status");
            return Result<bool>.Failure(ex.Message);
        }
    }

    public async Task<Result> EnableProtectionAsync(string driveLetter, CancellationToken ct)
    {
        try
        {
            var script = await GetScriptContentAsync(ct);
            var parameters = new Dictionary<string, object>
            {
                { "Action", "EnableProtection" },
                { "DriveLetter", driveLetter }
            };
            var result = await _powerShellService.ExecuteScriptContentAsync(script, parameters, ct);
            if (!result.IsSuccess) return Result.Failure(result.ErrorMessage);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable system protection on {DriveLetter}", driveLetter);
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result<SystemRestorePoint>> CreatePointAsync(string description, CancellationToken ct)
    {
        try
        {
            var script = await GetScriptContentAsync(ct);
            var parameters = new Dictionary<string, object>
            {
                { "Action", "CreatePoint" },
                { "Description", description }
            };
            var result = await _powerShellService.ExecuteScriptContentAsync(script, parameters, ct);
            if (!result.IsSuccess) return Result<SystemRestorePoint>.Failure(result.ErrorMessage);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var point = JsonSerializer.Deserialize<SystemRestorePoint>(result.Value, options);
            
            if (point == null) return Result<SystemRestorePoint>.Failure("Failed to parse restore point data");

            return Result<SystemRestorePoint>.Success(point);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create system restore point");
            return Result<SystemRestorePoint>.Failure(ex.Message);
        }
    }

    public async Task<Result<List<SystemRestorePoint>>> ListPointsAsync(CancellationToken ct)
    {
        try
        {
            var script = await GetScriptContentAsync(ct);
            var parameters = new Dictionary<string, object> { { "Action", "ListPoints" } };
            var result = await _powerShellService.ExecuteScriptContentAsync(script, parameters, ct);
            
            if (!result.IsSuccess) return Result<List<SystemRestorePoint>>.Failure(result.ErrorMessage);
            
            if (string.IsNullOrWhiteSpace(result.Value))
            {
                return Result<List<SystemRestorePoint>>.Success(new List<SystemRestorePoint>());
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var points = JsonSerializer.Deserialize<List<SystemRestorePoint>>(result.Value, options) ?? new List<SystemRestorePoint>();
            return Result<List<SystemRestorePoint>>.Success(points);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list system restore points");
            return Result<List<SystemRestorePoint>>.Failure(ex.Message);
        }
    }

    public async Task<Result> RestoreToPointAsync(int sequenceNumber, CancellationToken ct)
    {
        try
        {
            // The easiest way to start system restore GUI is rstrui.exe
            // Since there's no safe headless way to rollback the live OS via script without WinPE
            // System Restore via GUI is the standard approach.
            var result = await _processExecutorService.ExecuteAsync("rstrui.exe", "", false, ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch system restore UI");
            return Result.Failure(ex.Message);
        }
    }
}

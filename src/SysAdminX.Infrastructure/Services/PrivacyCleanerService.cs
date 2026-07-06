using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.Infrastructure.Services;

public class PrivacyCleanerService : IPrivacyCleanerService
{
    private readonly IPowerShellService _powerShellService;
    private readonly ILogger<PrivacyCleanerService> _logger;

    public PrivacyCleanerService(
        IPowerShellService powerShellService,
        ILogger<PrivacyCleanerService> logger)
    {
        _powerShellService = powerShellService;
        _logger = logger;
    }

    public async Task<Result<long>> ScanCategoryAsync(string categoryId, CancellationToken ct)
    {
        try
        {
            var script = await _powerShellService.ExtractEmbeddedScriptAsync("privacy_scan.ps1", ct);
            var parameters = new Dictionary<string, object> { { "CategoryId", categoryId } };
            var result = await _powerShellService.ExecuteScriptContentAsync(script, parameters, ct);
            
            if (!result.IsSuccess) return Result<long>.Failure(result.ErrorMessage);

            if (long.TryParse(result.Value.Trim(), out var size))
            {
                return Result<long>.Success(size);
            }
            return Result<long>.Success(0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan privacy category {CategoryId}", categoryId);
            return Result<long>.Failure(ex.Message);
        }
    }

    public async Task<Result> CleanCategoryAsync(string categoryId, CancellationToken ct)
    {
        try
        {
            var script = await _powerShellService.ExtractEmbeddedScriptAsync("privacy_clean.ps1", ct);
            var parameters = new Dictionary<string, object> { { "CategoryId", categoryId } };
            var result = await _powerShellService.ExecuteScriptContentAsync(script, parameters, ct);
            
            if (!result.IsSuccess) return Result.Failure(result.ErrorMessage);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clean privacy category {CategoryId}", categoryId);
            return Result.Failure(ex.Message);
        }
    }
}

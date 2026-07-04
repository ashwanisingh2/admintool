using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.Infrastructure.Services;

public class BrowserRepairService : IBrowserRepairService
{
    private readonly IPowerShellService _powerShellService;
    private readonly ILogger<BrowserRepairService> _logger;

    public BrowserRepairService(
        IPowerShellService powerShellService,
        ILogger<BrowserRepairService> logger)
    {
        _powerShellService = powerShellService;
        _logger = logger;
    }

    private async Task<Result> ExecuteBrowserActionAsync(string action, string browserId, CancellationToken ct)
    {
        try
        {
            var script = await _powerShellService.ExtractEmbeddedScriptAsync("browser_reset.ps1", ct);
            var parameters = new Dictionary<string, object> 
            { 
                { "Action", action },
                { "BrowserId", browserId }
            };
            var result = await _powerShellService.ExecuteScriptContentAsync(script, parameters, ct);
            
            if (!result.IsSuccess) return Result.Failure(result.ErrorMessage);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute browser action {Action} for {BrowserId}", action, browserId);
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result<List<BrowserRepairModel>>> GetBrowsersAsync(CancellationToken ct)
    {
        try
        {
            var script = await _powerShellService.ExtractEmbeddedScriptAsync("browser_reset.ps1", ct);
            var parameters = new Dictionary<string, object> { { "Action", "GetBrowsers" } };
            var result = await _powerShellService.ExecuteScriptContentAsync(script, parameters, ct);
            
            if (!result.IsSuccess) return Result<List<BrowserRepairModel>>.Failure(result.ErrorMessage);

            if (string.IsNullOrWhiteSpace(result.Data))
                return Result<List<BrowserRepairModel>>.Success(new List<BrowserRepairModel>());

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var browsers = JsonSerializer.Deserialize<List<BrowserRepairModel>>(result.Data, options);

            return Result<List<BrowserRepairModel>>.Success(browsers ?? new List<BrowserRepairModel>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get browsers");
            return Result<List<BrowserRepairModel>>.Failure(ex.Message);
        }
    }

    public Task<Result> ClearCacheAsync(string browserId, CancellationToken ct) => ExecuteBrowserActionAsync("ClearCache", browserId, ct);
    public Task<Result> ResetBrowserAsync(string browserId, CancellationToken ct) => ExecuteBrowserActionAsync("Reset", browserId, ct);
    public Task<Result> ReRegisterBrowserAsync(string browserId, CancellationToken ct) => ExecuteBrowserActionAsync("ReRegister", browserId, ct);
}

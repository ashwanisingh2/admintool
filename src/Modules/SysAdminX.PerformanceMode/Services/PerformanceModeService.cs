using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.PerformanceMode.Services;

public class PerformanceModeService : IPerformanceModeService
{
    private readonly IPowerShellService _powerShellService;

    public PerformanceModeService(IPowerShellService powerShellService)
    {
        _powerShellService = powerShellService;
    }

    public async Task<Result> ApplyProfileAsync(string profileId, CancellationToken ct = default)
    {
        var scriptContent = await _powerShellService.ExtractEmbeddedScriptAsync("SysAdminX.PerformanceMode.Scripts.apply_performance_profile.ps1", ct);
        var parameters = new Dictionary<string, object> { { "ProfileId", profileId } };
        
        var result = await _powerShellService.ExecuteScriptContentAsync(scriptContent, parameters, ct);
        return result.IsSuccess ? Result.Success() : Result.Failure(result.ErrorMessage);
    }

    public async Task<Result<string>> GetCurrentProfileAsync(CancellationToken ct = default)
    {
        var scriptContent = await _powerShellService.ExtractEmbeddedScriptAsync("SysAdminX.PerformanceMode.Scripts.get_current_profile.ps1", ct);
        var result = await _powerShellService.ExecuteScriptContentAsync(scriptContent, null, ct);
        
        if (result.IsSuccess)
        {
            return Result<string>.Success(result.Value.Trim());
        }
        
        return Result<string>.Failure(result.ErrorMessage);
    }
}

using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.Infrastructure.Services;

public class TrimService : ITrimService
{
    private readonly ILogger<TrimService> _logger;
    private readonly IPowerShellService _powerShellService;

    public TrimService(ILogger<TrimService> logger, IPowerShellService powerShellService)
    {
        _logger = logger;
        _powerShellService = powerShellService;
    }

    public async Task<Result<string>> RunTrimAsync(string driveLetter, CancellationToken ct = default)
    {
        try
        {
            var tempScriptFile = Path.Combine(Path.GetTempPath(), "run_trim.ps1");
            
            var assembly = Assembly.GetExecutingAssembly();
            var scriptContent = await _powerShellService.ExtractEmbeddedScriptAsync("SysAdminX.Infrastructure.Scripts.run_trim.ps1", ct);

            var parameters = new System.Collections.Generic.Dictionary<string, object>
            {
                { "DriveLetter", driveLetter }
            };

            var result = await _powerShellService.ExecuteScriptContentAsync(scriptContent, parameters, ct);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run TRIM on drive {DriveLetter}", driveLetter);
            return Result<string>.Failure("Error running TRIM: " + ex.Message);
        }
    }
}

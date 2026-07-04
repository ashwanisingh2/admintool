using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;
using SysAdminX.AutoPilot.Models;

namespace SysAdminX.AutoPilot.Services;

public class AutoPilotService : IAutoPilotService
{
    private readonly IPowerShellService _powerShellService;

    public AutoPilotService(IPowerShellService powerShellService)
    {
        _powerShellService = powerShellService;
    }

    public async Task<Result> ScheduleAsync(string dayOfWeek, string time, AutoPilotActions actions, CancellationToken ct = default)
    {
        var scriptContent = await _powerShellService.ExtractEmbeddedScriptAsync("SysAdminX.AutoPilot.Scripts.schedule_care.ps1", ct);
        
        var parameters = new Dictionary<string, object>
        {
            { "Day", dayOfWeek },
            { "Time", time }
        };
        
        var result = await _powerShellService.ExecuteScriptContentAsync(scriptContent, parameters, ct);
        if (!result.IsSuccess) return Result.Failure(result.ErrorMessage ?? "Failed to execute schedule script.");

        // Verify RunLevel Highest is in the task XML
        var verify = await _powerShellService.ExecuteCommandAsync("schtasks.exe /Query /TN SolasSystemCarePro_WeeklyCare /XML", ct);
        if (!verify.IsSuccess || (verify.Value != null && !verify.Value.Contains("<RunLevel>HighestAvailable</RunLevel>")))
            return Result.Failure("Task registered but RunLevel verification failed.");

        return Result.Success();
    }

    public async Task<Result<AutoPilotTaskInfo>> GetStatusAsync(CancellationToken ct = default)
    {
        var scriptContent = await _powerShellService.ExtractEmbeddedScriptAsync("SysAdminX.AutoPilot.Scripts.check_task_status.ps1", ct);
        var result = await _powerShellService.ExecuteScriptContentAsync(scriptContent, null, ct);
        
        if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.Value))
            return Result<AutoPilotTaskInfo>.Failure(result.ErrorMessage ?? "Failed to get task status.");
            
        try
        {
            var info = JsonSerializer.Deserialize<AutoPilotTaskInfo>(result.Value);
            return Result<AutoPilotTaskInfo>.Success(info ?? new AutoPilotTaskInfo());
        }
        catch (JsonException ex)
        {
            return Result<AutoPilotTaskInfo>.Failure("Failed to parse task status JSON.", ex);
        }
    }

    public async Task<Result> UnscheduleAsync(CancellationToken ct = default)
    {
        var scriptContent = await _powerShellService.ExtractEmbeddedScriptAsync("SysAdminX.AutoPilot.Scripts.unschedule_care.ps1", ct);
        var result = await _powerShellService.ExecuteScriptContentAsync(scriptContent, null, ct);
        
        if (!result.IsSuccess)
            return Result.Failure(result.ErrorMessage ?? "Failed to unschedule task.");
            
        return Result.Success();
    }
}

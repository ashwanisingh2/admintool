using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.Infrastructure.Services;

public class OneClickCareService : IOneClickCareService
{
    private readonly IPowerShellService _powerShellService;
    private readonly ILogger<OneClickCareService> _logger;

    public event EventHandler<StepProgressEventArgs>? StepProgressChanged;

    public OneClickCareService(
        IPowerShellService powerShellService,
        ILogger<OneClickCareService> logger)
    {
        _powerShellService = powerShellService;
        _logger = logger;
    }

    public async Task RunCareSequenceAsync(IEnumerable<CareStepModel> steps, CancellationToken ct)
    {
        string scriptContent;
        try
        {
            scriptContent = await _powerShellService.ExtractEmbeddedScriptAsync("iobit_one_click_care.ps1", ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load script iobit_one_click_care.ps1");
            return;
        }

        foreach (var step in steps)
        {
            if (ct.IsCancellationRequested)
            {
                StepProgressChanged?.Invoke(this, new StepProgressEventArgs(step.Name, "cancelled", 0));
                return;
            }

            StepProgressChanged?.Invoke(this, new StepProgressEventArgs(step.Name, "started", 0));
            try
            {
                var parameters = new Dictionary<string, object> { { "Action", step.Action } };
                
                // Write temp script file for streaming
                string tempScriptPath = System.IO.Path.GetTempFileName() + ".ps1";
                await System.IO.File.WriteAllTextAsync(tempScriptPath, scriptContent, ct);
                
                var result = await _powerShellService.ExecuteStreamingAsync(
                    tempScriptPath,
                    parameters,
                    line =>
                    {
                        if (line.Contains("[STEP_START]"))
                        {
                            StepProgressChanged?.Invoke(this, new StepProgressEventArgs(step.Name, "running", 10, "", line.Replace("[STEP_START]", "").Trim()));
                        }
                        else if (line.Contains("[STEP_SUCCESS]"))
                        {
                            StepProgressChanged?.Invoke(this, new StepProgressEventArgs(step.Name, "success", 100, "", line.Replace("[STEP_SUCCESS]", "").Trim()));
                        }
                        else if (line.Contains("[STEP_ERROR]"))
                        {
                            StepProgressChanged?.Invoke(this, new StepProgressEventArgs(step.Name, "failed", 0, line.Replace("[STEP_ERROR]", "").Trim()));
                        }
                        else
                        {
                            // SFC parsing
                            var sfcMatch = Regex.Match(line, @"Verification (\d+)% complete");
                            if (sfcMatch.Success)
                            {
                                int progress = int.Parse(sfcMatch.Groups[1].Value);
                                StepProgressChanged?.Invoke(this, new StepProgressEventArgs(step.Name, "running", progress, "", line));
                            }
                            else
                            {
                                StepProgressChanged?.Invoke(this, new StepProgressEventArgs(step.Name, "running", 50, "", line));
                            }
                        }
                    },
                    ct);

                System.IO.File.Delete(tempScriptPath);

                if (!result.IsSuccess || !result.Data)
                {
                    StepProgressChanged?.Invoke(this, new StepProgressEventArgs(step.Name, "failed", 0, result.ErrorMessage));
                    return; // stop the sequence
                }
            }
            catch (OperationCanceledException)
            {
                StepProgressChanged?.Invoke(this, new StepProgressEventArgs(step.Name, "cancelled", 0));
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running step {StepName}", step.Name);
                StepProgressChanged?.Invoke(this, new StepProgressEventArgs(step.Name, "failed", 0, ex.Message));
                return;
            }
        }
        StepProgressChanged?.Invoke(this, new StepProgressEventArgs("All steps", "complete", 100));
    }
}

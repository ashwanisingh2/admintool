// -----------------------------------------------------------------------
// <copyright file="ProcessExecutorService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.Infrastructure;

/// <summary>
/// Implementation of <see cref="IProcessExecutorService"/> using System.Diagnostics.Process.
/// </summary>
public class ProcessExecutorService : IProcessExecutorService
{
    private readonly ILogger<ProcessExecutorService> _logger;

    public ProcessExecutorService(ILogger<ProcessExecutorService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<string>> ExecuteAsync(string fileName, string arguments, bool requireElevation = false, CancellationToken ct = default)
    {
        _logger.LogInformation("Executing process {FileName} {Arguments}", fileName, arguments);

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = requireElevation,
            CreateNoWindow = !requireElevation,
            WindowStyle = requireElevation ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden,
            RedirectStandardOutput = !requireElevation,
            RedirectStandardError = !requireElevation
        };
        
        if (!requireElevation)
        {
            startInfo.StandardOutputEncoding = Encoding.UTF8;
            startInfo.StandardErrorEncoding = Encoding.UTF8;
        }

        if (requireElevation)
        {
            startInfo.Verb = "runas";
        }

        try
        {
            using var process = new Process { StartInfo = startInfo };
            
            var tcs = new TaskCompletionSource<string>();
            var outputBuilder = new StringBuilder();
            
            if (!requireElevation)
            {
                process.OutputDataReceived += (s, e) => {
                    if (e.Data != null) outputBuilder.AppendLine(e.Data);
                };
                process.ErrorDataReceived += (s, e) => {
                    if (e.Data != null) _logger.LogWarning("Process {File} Error: {Err}", fileName, e.Data);
                };
            }

            process.EnableRaisingEvents = true;
            process.Exited += (s, e) => tcs.TrySetResult(outputBuilder.ToString());

            if (!process.Start())
            {
                return Result<string>.Failure($"Failed to start process {fileName}");
            }

            if (!requireElevation)
            {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }
            else
            {
                // Elevated process without redirect (User might see UAC, and we can't capture output easily)
                // For elevation, we usually wrap the command to output to a file and read it back, 
                // but for now we just await exit.
                process.WaitForExit();
                return Result<string>.Success("Elevated command executed.");
            }

            // Wait for exit or cancellation
            await using (ct.Register(() => 
            {
                try { if (!process.HasExited) process.Kill(); } catch { }
                tcs.TrySetCanceled();
            }))
            {
                var output = await tcs.Task;
                if (process.ExitCode != 0)
                {
                    _logger.LogWarning("Process {FileName} exited with code {Code}", fileName, process.ExitCode);
                }
                return Result<string>.Success(output);
            }
        }
        catch (OperationCanceledException)
        {
            return Result<string>.Cancelled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute process {FileName}", fileName);
            return Result<string>.Failure(ex.Message, ex);
        }
    }
}

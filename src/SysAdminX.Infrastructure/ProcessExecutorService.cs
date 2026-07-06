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
            
            var outputBuilder = new StringBuilder();
            var outputLock = new object();
            
            if (!requireElevation)
            {
                process.OutputDataReceived += (s, e) => {
                    if (e.Data != null)
                    {
                        lock (outputLock)
                        {
                            outputBuilder.AppendLine(e.Data);
                        }
                    }
                };
                process.ErrorDataReceived += (s, e) => {
                    if (e.Data != null) _logger.LogWarning("Process {File} Error: {Err}", fileName, e.Data);
                };
            }

            process.EnableRaisingEvents = true;

            if (!process.Start())
            {
                return Result<string>.Failure($"Failed to start process {fileName}");
            }

            if (!requireElevation)
            {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }

            try
            {
                await process.WaitForExitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                try { if (!process.HasExited) process.Kill(); } catch { }
                throw;
            }

            if (process.ExitCode != 0)
            {
                string output;
                lock (outputLock)
                {
                    output = outputBuilder.ToString();
                }

                _logger.LogWarning("Process {FileName} exited with code {Code}", fileName, process.ExitCode);
                return Result<string>.Failure($"Process exited with code {process.ExitCode}{(string.IsNullOrWhiteSpace(output) ? string.Empty : $": {output.Trim()}")}");
            }

            string output;
            lock (outputLock)
            {
                output = outputBuilder.ToString();
            }

            return Result<string>.Success(output);
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

    public async Task<Result<bool>> ExecuteStreamingAsync(string fileName, string arguments, bool requireElevation, IProgress<string> outputProgress, CancellationToken ct = default)
    {
        _logger.LogInformation("Executing streaming process {FileName} {Arguments}", fileName, arguments);

        if (requireElevation)
        {
            // Streaming output from elevated process isn't natively supported without jumping through pipes.
            // For now, we fallback to a standard non-elevated capture if possible, or just skip streaming.
            _logger.LogWarning("ExecuteStreamingAsync called with requireElevation=true. Output cannot be captured natively.");
        }

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
            
            if (!requireElevation && outputProgress != null)
            {
                process.OutputDataReceived += (s, e) => {
                    if (e.Data != null)
                    {
                        outputProgress.Report(e.Data);
                    }
                };
                process.ErrorDataReceived += (s, e) => {
                    if (e.Data != null) _logger.LogWarning("Process {File} Error: {Err}", fileName, e.Data);
                };
            }

            process.EnableRaisingEvents = true;

            if (!process.Start())
            {
                return Result<bool>.Failure($"Failed to start process {fileName}");
            }

            if (!requireElevation)
            {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }

            try
            {
                await process.WaitForExitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                try { if (!process.HasExited) process.Kill(); } catch { }
                throw;
            }

            if (process.ExitCode != 0)
            {
                _logger.LogWarning("Process {FileName} exited with code {Code}", fileName, process.ExitCode);
                return Result<bool>.Failure($"Process exited with code {process.ExitCode}");
            }

            return Result<bool>.Success(true);
        }
        catch (OperationCanceledException)
        {
            return Result<bool>.Cancelled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute streaming process {FileName}", fileName);
            return Result<bool>.Failure(ex.Message, ex);
        }
    }
}

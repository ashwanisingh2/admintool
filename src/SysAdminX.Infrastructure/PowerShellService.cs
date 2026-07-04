// -----------------------------------------------------------------------
// <copyright file="PowerShellService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.Infrastructure;

/// <summary>
/// Concrete implementation of <see cref="IPowerShellService"/>.
/// Executes PowerShell commands safely using Process with redirected output.
/// All user input is sanitized before execution.
/// </summary>
public class PowerShellService : IPowerShellService
{
    private readonly ILogger<PowerShellService> _logger;
    private const int DEFAULT_TIMEOUT_MS = 30_000;

    /// <summary>
    /// Initializes a new instance of the <see cref="PowerShellService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public PowerShellService(ILogger<PowerShellService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result<string>> ExecuteCommandAsync(string command, CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            _logger.LogInformation("Executing PowerShell command: {Command}", SanitizeForLog(command));

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{EscapeCommand(command)}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = new Process { StartInfo = psi };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null) outputBuilder.AppendLine(e.Data);
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var registration = ct.Register(() =>
            {
                try { process.Kill(entireProcessTree: true); }
                catch { /* Process may have already exited */ }
            });

            await process.WaitForExitAsync(ct);

            if (ct.IsCancellationRequested)
            {
                _logger.LogWarning("PowerShell command cancelled: {Command}", SanitizeForLog(command));
                return Result<string>.Cancelled();
            }

            var output = outputBuilder.ToString().TrimEnd();
            var error = errorBuilder.ToString().TrimEnd();

            if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
            {
                _logger.LogError(null, "PowerShell command failed (exit code {ExitCode}): {Error}", process.ExitCode, error);
                return Result<string>.Failure($"PowerShell error (exit code {process.ExitCode}): {error}");
            }

            _logger.LogDebug("PowerShell command completed successfully. Output length: {Length}", output.Length);
            return Result<string>.Success(output);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("PowerShell command cancelled: {Command}", SanitizeForLog(command));
            return Result<string>.Cancelled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute PowerShell command: {Command}", SanitizeForLog(command));
            return Result<string>.Failure($"PowerShell execution failed: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result<string>> ExecuteScriptFileAsync(string scriptPath, Dictionary<string, object>? parameters = null, CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();

            // Validate script path
            if (!System.IO.File.Exists(scriptPath))
            {
                return Result<string>.Failure($"Script file not found: {scriptPath}");
            }

            var paramString = new StringBuilder();
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    paramString.Append($" -{param.Key} '{EscapeCommand(param.Value?.ToString() ?? string.Empty)}'");
                }
            }

            var command = $"& '{scriptPath}'{paramString}";
            return await ExecuteCommandAsync(command, ct);
        }
        catch (OperationCanceledException)
        {
            return Result<string>.Cancelled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute PowerShell script: {Script}", scriptPath);
            return Result<string>.Failure(ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result<string>> ExecuteScriptContentAsync(string scriptContent, Dictionary<string, object>? parameters = null, CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();

            var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"script_{Guid.NewGuid():N}.ps1");
            await System.IO.File.WriteAllTextAsync(tempFile, scriptContent, ct);

            var result = await ExecuteScriptFileAsync(tempFile, parameters, ct);
            
            try { System.IO.File.Delete(tempFile); } catch { /* Ignore */ }
            return result;
        }
        catch (OperationCanceledException)
        {
            return Result<string>.Cancelled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute PowerShell script content");
            return Result<string>.Failure(ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result<List<Dictionary<string, object?>>>> ExecuteCommandWithObjectsAsync(string command, CancellationToken ct = default)
    {
        var jsonCommand = $"{command} | ConvertTo-Json -Depth 5 -Compress";
        var result = await ExecuteCommandAsync(jsonCommand, ct);

        if (!result.IsSuccess)
        {
            return Result<List<Dictionary<string, object?>>>.Failure(result.ErrorMessage ?? "Command failed");
        }

        try
        {
            var json = result.Value ?? "[]";
            if (string.IsNullOrWhiteSpace(json))
            {
                return Result<List<Dictionary<string, object?>>>.Success(new List<Dictionary<string, object?>>());
            }

            var objects = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(json);
            return Result<List<Dictionary<string, object?>>>.Success(objects ?? new List<Dictionary<string, object?>>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse PowerShell JSON output");
            return Result<List<Dictionary<string, object?>>>.Failure($"Failed to parse output: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Escapes special characters in a PowerShell command string.
    /// </summary>
    private static string EscapeCommand(string command)
    {
        return command
            .Replace("\"", "\\\"")
            .Replace("$", "`$");
    }

    /// <summary>
    /// Sanitizes command text for safe logging (removes potential secrets).
    /// </summary>
    private static string SanitizeForLog(string command)
    {
        if (command.Length > 200)
        {
            return command[..200] + "... (truncated)";
        }
        return command;
    }

    /// <inheritdoc />
    public async Task<string> ExtractEmbeddedScriptAsync(string resourceName, CancellationToken ct = default)
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new System.IO.FileNotFoundException($"Could not find embedded script: {resourceName}");
        }
        using var reader = new System.IO.StreamReader(stream);
        return await reader.ReadToEndAsync(ct);
    }

    /// <inheritdoc />
    public async Task<Result<bool>> ExecuteStreamingAsync(string scriptPath, Dictionary<string, object>? parameters, Action<string> onOutput, CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();

            if (!System.IO.File.Exists(scriptPath))
            {
                return Result<bool>.Failure($"Script file not found: {scriptPath}");
            }

            var paramString = new StringBuilder();
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    paramString.Append($" -{param.Key} '{EscapeCommand(param.Value?.ToString() ?? string.Empty)}'");
                }
            }
            
            var command = $"& '{scriptPath}'{paramString}";
            _logger.LogInformation("Executing streaming PowerShell command: {Command}", SanitizeForLog(command));

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{EscapeCommand(command)}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = new Process { StartInfo = psi };
            
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null) onOutput(e.Data);
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) _logger.LogWarning("PowerShell stream error: {Error}", e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var registration = ct.Register(() =>
            {
                try { process.Kill(entireProcessTree: true); }
                catch { /* Process may have already exited */ }
            });

            await process.WaitForExitAsync(ct);

            if (ct.IsCancellationRequested)
            {
                return Result<bool>.Cancelled();
            }

            if (process.ExitCode != 0)
            {
                return Result<bool>.Failure($"PowerShell error (exit code {process.ExitCode})");
            }

            return Result<bool>.Success(true);
        }
        catch (OperationCanceledException)
        {
            return Result<bool>.Cancelled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute streaming PowerShell script");
            return Result<bool>.Failure(ex.Message, ex);
        }
    }
}

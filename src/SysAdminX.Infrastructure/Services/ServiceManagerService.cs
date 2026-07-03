// -----------------------------------------------------------------------
// <copyright file="ServiceManagerService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.Infrastructure.Services;

/// <summary>
/// Implementation of <see cref="IServiceManagerService"/> using ServiceController and WMI.
/// </summary>
public class ServiceManagerService : IServiceManagerService
{
    private readonly ILogger<ServiceManagerService> _logger;
    private readonly IWmiService _wmiService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceManagerService"/> class.
    /// </summary>
    public ServiceManagerService(ILogger<ServiceManagerService> logger, IWmiService wmiService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _wmiService = wmiService ?? throw new ArgumentNullException(nameof(wmiService));
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<WindowsServiceModel>>> GetServicesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting list of Windows Services using WMI");
            
            // We use WMI because it gives us StartName and Description which ServiceController lacks
            var result = await _wmiService.QueryAsync("SELECT Name, DisplayName, Description, State, StartMode, StartName, AcceptStop, AcceptPause FROM Win32_Service", cancellationToken);
            
            if (!result.IsSuccess)
            {
                return Result<IEnumerable<WindowsServiceModel>>.Failure(result.ErrorMessage ?? "Failed to query services");
            }

            var services = new List<WindowsServiceModel>();
            
            foreach (var item in result.Value!)
            {
                services.Add(new WindowsServiceModel
                {
                    Name = item.GetValueOrDefault("Name")?.ToString() ?? string.Empty,
                    DisplayName = item.GetValueOrDefault("DisplayName")?.ToString() ?? string.Empty,
                    Description = item.GetValueOrDefault("Description")?.ToString() ?? string.Empty,
                    Status = item.GetValueOrDefault("State")?.ToString() ?? string.Empty,
                    StartType = item.GetValueOrDefault("StartMode")?.ToString() ?? string.Empty,
                    StartName = item.GetValueOrDefault("StartName")?.ToString() ?? string.Empty,
                    CanStop = bool.TryParse(item.GetValueOrDefault("AcceptStop")?.ToString(), out var canStop) && canStop,
                    CanPauseAndContinue = bool.TryParse(item.GetValueOrDefault("AcceptPause")?.ToString(), out var canPause) && canPause
                });
            }

            return Result<IEnumerable<WindowsServiceModel>>.Success(services.OrderBy(s => s.DisplayName));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get services");
            return Result<IEnumerable<WindowsServiceModel>>.Failure(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> StartServiceAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var sc = new ServiceController(serviceName);
                if (sc.Status != ServiceControllerStatus.Running && sc.Status != ServiceControllerStatus.StartPending)
                {
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                }
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start service {Service}", serviceName);
                return Result<bool>.Failure($"Failed to start: {ex.Message}");
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<bool>> StopServiceAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var sc = new ServiceController(serviceName);
                if (sc.Status != ServiceControllerStatus.Stopped && sc.Status != ServiceControllerStatus.StopPending)
                {
                    if (sc.CanStop)
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                    }
                    else
                    {
                        return Result<bool>.Failure("Service cannot be stopped");
                    }
                }
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop service {Service}", serviceName);
                return Result<bool>.Failure($"Failed to stop: {ex.Message}");
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<bool>> RestartServiceAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        var stopResult = await StopServiceAsync(serviceName, cancellationToken);
        if (!stopResult.IsSuccess)
        {
            return stopResult;
        }

        return await StartServiceAsync(serviceName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<bool>> ChangeStartupTypeAsync(string serviceName, string startMode, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                // We use sc.exe to change start mode since ServiceController doesn't support changing it easily
                // startMode should be "auto", "demand" (Manual), or "disabled"
                string scMode = startMode.ToLowerInvariant() switch
                {
                    "automatic" => "auto",
                    "manual" => "demand",
                    "disabled" => "disabled",
                    _ => throw new ArgumentException("Invalid start mode", nameof(startMode))
                };

                using var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "sc.exe";
                process.StartInfo.Arguments = $"config \"{serviceName}\" start= {scMode}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    return Result<bool>.Success(true);
                }
                else
                {
                    string error = process.StandardError.ReadToEnd();
                    return Result<bool>.Failure($"Failed to change startup type: {error}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to change startup type for {Service}", serviceName);
                return Result<bool>.Failure($"Error changing startup type: {ex.Message}");
            }
        }, cancellationToken);
    }
}

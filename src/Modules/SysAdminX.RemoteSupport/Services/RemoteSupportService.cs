// -----------------------------------------------------------------------
// <copyright file="RemoteSupportService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SysAdminX.RemoteSupport.Services;

/// <summary>
/// Implementation of <see cref="IRemoteSupportService"/> using native Windows tools.
/// </summary>
public class RemoteSupportService : IRemoteSupportService
{
    private readonly ILogger<RemoteSupportService> _logger;

    public RemoteSupportService(ILogger<RemoteSupportService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task LaunchRdpAsync(string hostname)
    {
        try
        {
            _logger.LogInformation("Launching RDP for {Hostname}", hostname);
            
            var args = string.IsNullOrWhiteSpace(hostname) ? "" : $"/v:{hostname}";
            Process.Start(new ProcessStartInfo
            {
                FileName = "mstsc.exe",
                Arguments = args,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch RDP");
        }
        
        return Task.CompletedTask;
    }

    public Task LaunchComputerManagementAsync(string hostname)
    {
        try
        {
            _logger.LogInformation("Launching Computer Management for {Hostname}", hostname);
            
            var args = string.IsNullOrWhiteSpace(hostname) ? "compmgmt.msc" : $"compmgmt.msc /computer=\\\\{hostname}";
            Process.Start(new ProcessStartInfo
            {
                FileName = "mmc.exe",
                Arguments = args,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch Computer Management");
        }
        
        return Task.CompletedTask;
    }
    
    public Task LaunchRemoteCommandPromptAsync(string hostname)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(hostname)) return Task.CompletedTask;
            
            _logger.LogInformation("Launching Remote Command Prompt for {Hostname}", hostname);
            
            // Assume psexec is in path or Sysinternals is available
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c psexec \\\\{hostname} cmd.exe",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch Remote Command Prompt");
        }
        
        return Task.CompletedTask;
    }

    public Task LaunchRemotePowerShellAsync(string hostname)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(hostname)) return Task.CompletedTask;
            
            _logger.LogInformation("Launching Remote PowerShell for {Hostname}", hostname);
            
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c psexec \\\\{hostname} powershell.exe",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch Remote PowerShell");
        }
        
        return Task.CompletedTask;
    }
}

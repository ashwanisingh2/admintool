// -----------------------------------------------------------------------
// <copyright file="SoftwareManagerService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.Infrastructure.Services;

public class SoftwareManagerService : ISoftwareManagerService
{
    private readonly ILogger<SoftwareManagerService> _logger;

    public SoftwareManagerService(ILogger<SoftwareManagerService> logger)
    {
        _logger = logger;
    }

    public async Task<Result<IEnumerable<SoftwareItemModel>>> GetInstalledSoftwareAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var softwareList = new List<SoftwareItemModel>();
                string[] registryKeys = new[]
                {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                    @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
                };

                foreach (var registryKey in registryKeys)
                {
                    using var key = Registry.LocalMachine.OpenSubKey(registryKey);
                    if (key != null)
                    {
                        foreach (var subKeyName in key.GetSubKeyNames())
                        {
                            using var subKey = key.OpenSubKey(subKeyName);
                            if (subKey != null)
                            {
                                var displayName = subKey.GetValue("DisplayName") as string;
                                if (!string.IsNullOrEmpty(displayName))
                                {
                                    softwareList.Add(new SoftwareItemModel
                                    {
                                        DisplayName = displayName,
                                        DisplayVersion = subKey.GetValue("DisplayVersion") as string ?? string.Empty,
                                        Publisher = subKey.GetValue("Publisher") as string ?? string.Empty,
                                        InstallDate = subKey.GetValue("InstallDate") as string ?? string.Empty,
                                        UninstallString = subKey.GetValue("UninstallString") as string ?? string.Empty
                                    });
                                }
                            }
                        }
                    }
                }
                return Result<IEnumerable<SoftwareItemModel>>.Success(softwareList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get software.");
                return Result<IEnumerable<SoftwareItemModel>>.Failure("Failed to get software: " + ex.Message);
            }
        });
    }

    public async Task<Result<bool>> UninstallSoftwareAsync(string uninstallString)
    {
        if (string.IsNullOrEmpty(uninstallString))
        {
            return Result<bool>.Failure("Uninstall string is empty.");
        }

        return await Task.Run(() =>
        {
            try
            {
                var processInfo = new ProcessStartInfo("cmd.exe", $"/c {uninstallString}")
                {
                    UseShellExecute = true,
                    Verb = "runas"
                };

                using var process = Process.Start(processInfo);
                if (process != null)
                {
                    process.WaitForExit();
                }

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to uninstall software.");
                return Result<bool>.Failure("Failed to uninstall software: " + ex.Message);
            }
        });
    }
}

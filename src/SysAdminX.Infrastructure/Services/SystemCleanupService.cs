// -----------------------------------------------------------------------
// <copyright file="SystemCleanupService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.Infrastructure.Services;

public class SystemCleanupService : ISystemCleanupService
{
    private readonly ILogger<SystemCleanupService> _logger;

    public SystemCleanupService(ILogger<SystemCleanupService> logger)
    {
        _logger = logger;
    }

    public async Task<Result<IEnumerable<CleanupItemModel>>> GetCleanupItemsAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var items = new List<CleanupItemModel>
                {
                    new()
                    {
                        Id = "temp",
                        Name = "Temporary Files",
                        Description = "Files in the current user's temporary folder.",
                        SizeBytes = GetDirectorySize(Path.GetTempPath()),
                        IsSelected = true
                    },
                    new()
                    {
                        Id = "windows_temp",
                        Name = "Windows Temporary Files",
                        Description = "Files in the Windows Temp folder.",
                        SizeBytes = GetDirectorySize(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp")),
                        IsSelected = true
                    }
                };
                return Result<IEnumerable<CleanupItemModel>>.Success(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get cleanup items.");
                return Result<IEnumerable<CleanupItemModel>>.Failure("Failed to get cleanup items: " + ex.Message);
            }
        });
    }

    public async Task<Result<long>> CalculateSpaceAsync(IEnumerable<string> itemIds)
    {
        return await Task.Run(() => Result<long>.Success(0));
    }

    public async Task<Result<bool>> PerformCleanupAsync(IEnumerable<string> itemIds)
    {
        return await Task.Run(() =>
        {
            try
            {
                foreach (var id in itemIds)
                {
                    if (id == "temp")
                    {
                        CleanDirectory(Path.GetTempPath());
                    }
                    else if (id == "windows_temp")
                    {
                        CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"));
                    }
                }
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cleanup failed.");
                return Result<bool>.Failure("Cleanup failed: " + ex.Message);
            }
        });
    }

    private long GetDirectorySize(string path)
    {
        if (!Directory.Exists(path)) return 0;
        long size = 0;
        try
        {
            var dirInfo = new DirectoryInfo(path);
            foreach (var fi in dirInfo.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                size += fi.Length;
            }
        }
        catch
        {
            // Ignore access exceptions
        }
        return size;
    }

    private void CleanDirectory(string path)
    {
        if (!Directory.Exists(path)) return;
        var dirInfo = new DirectoryInfo(path);
        
        foreach (var file in dirInfo.EnumerateFiles())
        {
            try { file.Delete(); } catch { }
        }
        foreach (var dir in dirInfo.EnumerateDirectories())
        {
            try { dir.Delete(true); } catch { }
        }
    }
}

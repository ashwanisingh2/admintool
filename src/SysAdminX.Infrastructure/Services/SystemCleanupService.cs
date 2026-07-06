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

    public async Task<Result<IEnumerable<CleanupItemModel>>> GetCleanupItemsAsync(CancellationToken ct = default)
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
                        Path = Path.GetTempPath(),
                        SizeBytes = GetDirectorySize(Path.GetTempPath()),
                        IsSelected = true
                    },
                    new()
                    {
                        Id = "windows_temp",
                        Name = "Windows Temporary Files",
                        Description = "Files in the Windows Temp folder.",
                        Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"),
                        SizeBytes = GetDirectorySize(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp")),
                        IsSelected = true
                    },
                    new()
                    {
                        Id = "windows_update",
                        Name = "Windows Update Cache",
                        Description = "Temporary files created by Windows Update.",
                        Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SoftwareDistribution", "Download"),
                        SizeBytes = GetDirectorySize(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SoftwareDistribution", "Download")),
                        IsSelected = false
                    },
                    new()
                    {
                        Id = "prefetch",
                        Name = "Prefetch Cache",
                        Description = "Files used to speed up application launching.",
                        Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch"),
                        SizeBytes = GetDirectorySize(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch")),
                        IsSelected = false
                    },
                    new()
                    {
                        Id = "dns_cache",
                        Name = "DNS Cache",
                        Description = "Flushes the DNS resolver cache.",
                        Path = string.Empty,
                        SizeBytes = 0,
                        IsSelected = false
                    },
                    new()
                    {
                        Id = "chrome_cache",
                        Name = "Google Chrome Cache",
                        Description = "Temporary internet files for Chrome.",
                        Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "User Data", "Default", "Cache", "Cache_Data"),
                        SizeBytes = GetDirectorySize(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "User Data", "Default", "Cache", "Cache_Data")),
                        IsSelected = false
                    },
                    new()
                    {
                        Id = "edge_cache",
                        Name = "Microsoft Edge Cache",
                        Description = "Temporary internet files for Edge.",
                        Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Edge", "User Data", "Default", "Cache", "Cache_Data"),
                        SizeBytes = GetDirectorySize(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Edge", "User Data", "Default", "Cache", "Cache_Data")),
                        IsSelected = false
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

    public async Task<Result<long>> CalculateSpaceAsync(IEnumerable<string> itemIds, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                long totalSpace = 0;
                foreach (var id in itemIds)
                {
                    if (id == "temp")
                        totalSpace += GetDirectorySize(Path.GetTempPath());
                    else if (id == "windows_temp")
                        totalSpace += GetDirectorySize(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"));
                    else if (id == "windows_update")
                        totalSpace += GetDirectorySize(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SoftwareDistribution", "Download"));
                    else if (id == "prefetch")
                        totalSpace += GetDirectorySize(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch"));
                    else if (id == "chrome_cache")
                        totalSpace += GetDirectorySize(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "User Data", "Default", "Cache", "Cache_Data"));
                    else if (id == "edge_cache")
                        totalSpace += GetDirectorySize(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Edge", "User Data", "Default", "Cache", "Cache_Data"));
                }
                return Result<long>.Success(totalSpace);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate space.");
                return Result<long>.Failure("Failed to calculate space: " + ex.Message);
            }
        });
    }

    public async Task<Result<bool>> PerformCleanupAsync(IEnumerable<string> itemIds, CancellationToken ct = default)
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
                    else if (id == "windows_update")
                    {
                        CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SoftwareDistribution", "Download"));
                    }
                    else if (id == "prefetch")
                    {
                        CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch"));
                    }
                    else if (id == "dns_cache")
                    {
                        try 
                        {
                            var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = "ipconfig",
                                Arguments = "/flushdns",
                                UseShellExecute = false,
                                CreateNoWindow = true
                            });
                            process?.WaitForExit();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to flush DNS.");
                        }
                    }
                    else if (id == "chrome_cache")
                    {
                        CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "User Data", "Default", "Cache", "Cache_Data"));
                    }
                    else if (id == "edge_cache")
                    {
                        CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Edge", "User Data", "Default", "Cache", "Cache_Data"));
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

    public async Task<Result<CleanupResultModel>> CleanAsync(IEnumerable<CleanupItemModel> items, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            var backupDir = Path.Combine(Path.GetTempPath(), $"sysadminx_junk_undo_{Guid.NewGuid():N}");
            Directory.CreateDirectory(backupDir);

            long totalBytes = 0;
            var movedFiles = new List<(string OriginalPath, string BackupPath)>();

            var enumOptions = new EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = true };

            foreach (var item in items)
            {
                if (!item.IsSelected || string.IsNullOrEmpty(item.Path) || !Directory.Exists(item.Path))
                    continue;

                try
                {
                    foreach (var file in Directory.EnumerateFiles(item.Path, "*", enumOptions))
                    {
                        try
                        {
                            var relativePath = Path.GetRelativePath(item.Path, file);
                            var backupPath = Path.Combine(backupDir, relativePath);
                            Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
                            File.Move(file, backupPath);
                            totalBytes += new FileInfo(backupPath).Length;
                            movedFiles.Add((file, backupPath));
                        }
                        catch (IOException) { /* skip locked files */ }
                        catch (UnauthorizedAccessException) { /* skip */ }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to enumerate files in {Path}", item.Path);
                }
            }

            return Result<CleanupResultModel>.Success(new CleanupResultModel
            {
                TotalBytesFreed = totalBytes,
                BackupDirectory = backupDir,
                MovedFiles = movedFiles,
                UndoExpiresAt = DateTime.UtcNow.AddSeconds(30)
            });
        }, ct);
    }

    public async Task<Result> UndoAsync(string backupDirectory, List<(string OriginalPath, string BackupPath)> movedFiles, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                foreach (var (original, backup) in movedFiles)
                {
                    if (File.Exists(backup))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(original)!);
                        File.Move(backup, original);
                    }
                }
                if (Directory.Exists(backupDirectory))
                    Directory.Delete(backupDirectory, recursive: true);
                
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to undo cleanup.");
                return Result.Failure("Failed to undo: " + ex.Message);
            }
        }, ct);
    }

    public async Task<Result> FinalizeAsync(string backupDirectory, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (Directory.Exists(backupDirectory))
                    Directory.Delete(backupDirectory, recursive: true);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to finalize cleanup.");
                return Result.Failure("Failed to finalize: " + ex.Message);
            }
        }, ct);
    }

    private long GetDirectorySize(string path)
    {
        if (!Directory.Exists(path)) return 0;
        long size = 0;
        try
        {
            var dirInfo = new DirectoryInfo(path);
            var enumOptions = new EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = true };
            foreach (var fi in dirInfo.EnumerateFiles("*", enumOptions))
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

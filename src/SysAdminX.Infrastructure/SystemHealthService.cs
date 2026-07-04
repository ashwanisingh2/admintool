// -----------------------------------------------------------------------
// <copyright file="SystemHealthService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.Infrastructure;

/// <summary>
/// Concrete implementation of <see cref="ISystemHealthService"/>.
/// Collects real-time system health metrics using WMI, Performance Counters, and .NET APIs.
/// </summary>
public class SystemHealthService : ISystemHealthService
{
    private readonly ILogger<SystemHealthService> _logger;
    private readonly IWmiService _wmiService;
    private PerformanceCounter? _cpuCounter;
    private readonly object _counterLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemHealthService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="wmiService">The WMI service for hardware queries.</param>
    public SystemHealthService(ILogger<SystemHealthService> logger, IWmiService wmiService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _wmiService = wmiService ?? throw new ArgumentNullException(nameof(wmiService));

        Task.Run(() => InitializeCpuCounter());
    }

    /// <summary>
    /// Initializes the CPU performance counter for real-time CPU usage monitoring.
    /// </summary>
    private void InitializeCpuCounter()
    {
        try
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _cpuCounter.NextValue(); // First call always returns 0, prime it
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize CPU performance counter. Will fallback to WMI.");
            _cpuCounter = null;
        }
    }

    /// <inheritdoc />
    public async Task<Result<SystemHealthModel>> GetSystemHealthAsync(CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            _logger.LogInformation("Starting system health collection");

            // Collect metrics in parallel
            var cpuTask = GetCpuUsageAsync(ct);
            var cpuInfoTask = GetCpuInfoAsync(ct);
            var memoryTask = GetMemoryInfoAsync(ct);
            var diskTask = GetSystemDiskInfoAsync(ct);
            var networkTask = GetNetworkThroughputAsync(ct);
            var uptimeTask = GetUptimeAsync(ct);

            await Task.WhenAll(cpuTask, cpuInfoTask, memoryTask, diskTask, networkTask, uptimeTask);

            var cpuUsage = cpuTask.Result;
            var cpuInfo = cpuInfoTask.Result;
            var memory = memoryTask.Result;
            var disk = diskTask.Result;
            var network = networkTask.Result;
            var uptime = uptimeTask.Result;

            var health = new SystemHealthModel
            {
                CpuUsagePercent = cpuUsage.IsSuccess ? cpuUsage.Value : 0,
                CpuName = cpuInfo.IsSuccess ? cpuInfo.Value?.CpuName ?? "Unknown" : "Unknown",
                CpuCores = cpuInfo.IsSuccess ? cpuInfo.Value?.CpuCores ?? 0 : 0,
                CpuThreads = cpuInfo.IsSuccess ? cpuInfo.Value?.CpuThreads ?? 0 : 0,
                CpuSpeedMhz = cpuInfo.IsSuccess ? cpuInfo.Value?.CpuSpeedMhz ?? 0 : 0,
                TotalMemoryBytes = memory.IsSuccess ? memory.Value?.TotalBytes ?? 0 : 0,
                AvailableMemoryBytes = memory.IsSuccess ? memory.Value?.AvailableBytes ?? 0 : 0,
                TotalDiskBytes = disk.IsSuccess ? disk.Value?.TotalBytes ?? 0 : 0,
                AvailableDiskBytes = disk.IsSuccess ? disk.Value?.FreeBytes ?? 0 : 0,
                SystemDrive = disk.IsSuccess ? disk.Value?.DriveLetter ?? "C:" : "C:",
                NetworkBytesSentPerSec = network.IsSuccess ? network.Value?.BytesSent ?? 0 : 0,
                NetworkBytesReceivedPerSec = network.IsSuccess ? network.Value?.BytesReceived ?? 0 : 0,
                Uptime = uptime.IsSuccess ? uptime.Value : TimeSpan.Zero,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogDebug("System health collected: CPU={Cpu}%, RAM={Ram}%, Disk={Disk}%",
                health.CpuUsagePercent, health.RamUsagePercent, health.DiskUsagePercent);

            return Result<SystemHealthModel>.Success(health);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("System health collection cancelled");
            return Result<SystemHealthModel>.Cancelled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect system health data");
            return Result<SystemHealthModel>.Failure($"Health collection failed: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result<WindowsInfoModel>> GetWindowsInfoAsync(CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            _logger.LogInformation("Collecting Windows version information");

            var osResult = await _wmiService.QueryAsync("SELECT Caption, Version, BuildNumber, OSArchitecture, LastBootUpTime, InstallDate FROM Win32_OperatingSystem", ct);
            if (!osResult.IsSuccess || osResult.Value == null || osResult.Value.Count == 0)
            {
                return Result<WindowsInfoModel>.Failure("Failed to query Windows OS information");
            }

            var os = osResult.Value[0];

            var csResult = await _wmiService.QueryAsync("SELECT Name, Domain, PartOfDomain, Workgroup, UserName FROM Win32_ComputerSystem", ct);
            var cs = csResult.IsSuccess && csResult.Value?.Count > 0 ? csResult.Value[0] : new Dictionary<string, object?>();

            // Parse display version from registry (e.g., "23H2")
            var displayVersion = GetRegistryDisplayVersion();

            var windowsInfo = new WindowsInfoModel
            {
                Edition = os.GetValueOrDefault("Caption")?.ToString()?.Replace("Microsoft ", "") ?? "Unknown",
                Version = displayVersion,
                BuildNumber = $"{os.GetValueOrDefault("Version")?.ToString() ?? "Unknown"}.{os.GetValueOrDefault("BuildNumber")?.ToString() ?? "0"}",
                Architecture = os.GetValueOrDefault("OSArchitecture")?.ToString() ?? "Unknown",
                ComputerName = Environment.MachineName,
                DomainOrWorkgroup = GetDomainOrWorkgroup(cs),
                LastBootTime = ParseWmiDateTime(os.GetValueOrDefault("LastBootUpTime")?.ToString()),
                InstallDate = ParseWmiDateTime(os.GetValueOrDefault("InstallDate")?.ToString()),
                CurrentUser = Environment.UserName
            };

            return Result<WindowsInfoModel>.Success(windowsInfo);
        }
        catch (OperationCanceledException)
        {
            return Result<WindowsInfoModel>.Cancelled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect Windows info");
            return Result<WindowsInfoModel>.Failure(ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public Task<Result<List<DiskInfoModel>>> GetDiskInfoAsync(CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                _logger.LogInformation("Collecting disk information");

                var disks = new List<DiskInfoModel>();
                var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
                .ToList();

            foreach (var drive in drives)
            {
                ct.ThrowIfCancellationRequested();
                disks.Add(new DiskInfoModel
                {
                    DriveLetter = drive.Name.TrimEnd('\\'),
                    VolumeLabel = drive.VolumeLabel,
                    FileSystem = drive.DriveFormat,
                    TotalBytes = (ulong)drive.TotalSize,
                    FreeBytes = (ulong)drive.AvailableFreeSpace
                });
            }

                return Result<List<DiskInfoModel>>.Success(disks);
            }
            catch (OperationCanceledException)
            {
                return Result<List<DiskInfoModel>>.Cancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to collect disk info");
                return Result<List<DiskInfoModel>>.Failure(ex.Message, ex);
            }
        }, ct);
    }

    /// <inheritdoc />
    public Task<Result<double>> GetCpuUsageAsync(CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                lock (_counterLock)
                {
                    if (_cpuCounter != null)
                    {
                        var value = Math.Round(_cpuCounter.NextValue(), 1);
                        return Result<double>.Success(value);
                    }
                }

                // Fallback: WMI-based CPU usage
                using var searcher = new ManagementObjectSearcher("SELECT LoadPercentage FROM Win32_Processor");
                using var results = searcher.Get();

                double totalLoad = 0;
                int count = 0;
                foreach (ManagementObject obj in results)
                {
                    using var _ = obj;
                    var load = obj["LoadPercentage"];
                    if (load != null)
                    {
                        totalLoad += Convert.ToDouble(load);
                        count++;
                    }
                }

                var cpuUsage = count > 0 ? Math.Round(totalLoad / count, 1) : 0;
                return Result<double>.Success(cpuUsage);
            }
            catch (OperationCanceledException)
            {
                return Result<double>.Cancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get CPU usage");
                return Result<double>.Failure(ex.Message, ex);
            }
        }, ct);
    }

    /// <inheritdoc />
    public async Task StartMonitoringAsync(int intervalMs, Action<SystemHealthModel> onUpdate, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting health monitoring with {Interval}ms interval", intervalMs);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var result = await GetSystemHealthAsync(ct);
                if (result.IsSuccess && result.Value != null)
                {
                    onUpdate(result.Value);
                }

                await Task.Delay(intervalMs, ct);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Health monitoring stopped");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during health monitoring cycle, continuing...");
                await Task.Delay(intervalMs, ct);
            }
        }
    }

    #region Private Helper Methods

    private record CpuInfoResult(string CpuName, int CpuCores, int CpuThreads, uint CpuSpeedMhz);

    private async Task<Result<CpuInfoResult>> GetCpuInfoAsync(CancellationToken ct)
    {
        try
        {
            var result = await _wmiService.QueryAsync(
                "SELECT Name, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed FROM Win32_Processor", ct);

            if (!result.IsSuccess || result.Value == null || result.Value.Count == 0)
            {
                return Result<CpuInfoResult>.Failure("No CPU info available");
            }

            var cpu = result.Value[0];
            return Result<CpuInfoResult>.Success(new CpuInfoResult(
                CpuName: cpu.GetValueOrDefault("Name")?.ToString()?.Trim() ?? "Unknown CPU",
                CpuCores: Convert.ToInt32(cpu.GetValueOrDefault("NumberOfCores") ?? 0),
                CpuThreads: Convert.ToInt32(cpu.GetValueOrDefault("NumberOfLogicalProcessors") ?? 0),
                CpuSpeedMhz: Convert.ToUInt32(cpu.GetValueOrDefault("MaxClockSpeed") ?? 0u)
            ));
        }
        catch (Exception ex)
        {
            return Result<CpuInfoResult>.Failure(ex.Message, ex);
        }
    }

    private record MemoryResult(ulong TotalBytes, ulong AvailableBytes);

    private Task<Result<MemoryResult>> GetMemoryInfoAsync(CancellationToken ct)
    {
        return Task.Run(() =>
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                using var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");
                using var results = searcher.Get();

                foreach (ManagementObject obj in results)
                {
                    using var _ = obj;
                    var totalKb = Convert.ToUInt64(obj["TotalVisibleMemorySize"] ?? 0);
                    var freeKb = Convert.ToUInt64(obj["FreePhysicalMemory"] ?? 0);
                    return Result<MemoryResult>.Success(new MemoryResult(totalKb * 1024, freeKb * 1024));
                }

                return Result<MemoryResult>.Failure("No memory info available");
            }
            catch (OperationCanceledException) { return Result<MemoryResult>.Cancelled(); }
            catch (Exception ex) { return Result<MemoryResult>.Failure(ex.Message, ex); }
        }, ct);
    }

    private record DiskResult(string DriveLetter, ulong TotalBytes, ulong FreeBytes);

    private Task<Result<DiskResult>> GetSystemDiskInfoAsync(CancellationToken ct)
    {
        return Task.Run(() =>
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var systemDrive = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
                var driveInfo = new DriveInfo(systemDrive);

                if (driveInfo.IsReady)
                {
                    return Result<DiskResult>.Success(new DiskResult(
                        driveInfo.Name.TrimEnd('\\'),
                        (ulong)driveInfo.TotalSize,
                        (ulong)driveInfo.AvailableFreeSpace));
                }

                return Result<DiskResult>.Failure("System drive not ready");
            }
            catch (OperationCanceledException) { return Result<DiskResult>.Cancelled(); }
            catch (Exception ex) { return Result<DiskResult>.Failure(ex.Message, ex); }
        }, ct);
    }

    private record NetworkResult(long BytesSent, long BytesReceived);

    private Task<Result<NetworkResult>> GetNetworkThroughputAsync(CancellationToken ct)
    {
        return Task.Run(() =>
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                var interfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up
                        && n.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                    .ToList();

                long totalSent = 0;
                long totalReceived = 0;

                foreach (var nic in interfaces)
                {
                    var stats = nic.GetIPStatistics();
                    totalSent += stats.BytesSent;
                    totalReceived += stats.BytesReceived;
                }

                return Result<NetworkResult>.Success(new NetworkResult(totalSent, totalReceived));
            }
            catch (OperationCanceledException) { return Result<NetworkResult>.Cancelled(); }
            catch (Exception ex) { return Result<NetworkResult>.Failure(ex.Message, ex); }
        }, ct);
    }

    private Task<Result<TimeSpan>> GetUptimeAsync(CancellationToken ct)
    {
        return Task.Run(() =>
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
                return Result<TimeSpan>.Success(uptime);
            }
            catch (OperationCanceledException) { return Result<TimeSpan>.Cancelled(); }
            catch (Exception ex) { return Result<TimeSpan>.Failure(ex.Message, ex); }
        }, ct);
    }

    private static string GetRegistryDisplayVersion()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            return key?.GetValue("DisplayVersion")?.ToString() ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private static string GetDomainOrWorkgroup(Dictionary<string, object?> cs)
    {
        var partOfDomain = cs.GetValueOrDefault("PartOfDomain");
        if (partOfDomain != null && Convert.ToBoolean(partOfDomain))
        {
            return cs.GetValueOrDefault("Domain")?.ToString() ?? "Unknown Domain";
        }
        return cs.GetValueOrDefault("Workgroup")?.ToString() ?? "WORKGROUP";
    }

    private static DateTime ParseWmiDateTime(string? wmiDateTime)
    {
        if (string.IsNullOrEmpty(wmiDateTime)) return DateTime.MinValue;

        try
        {
            return ManagementDateTimeConverter.ToDateTime(wmiDateTime);
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    #endregion
}

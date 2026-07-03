// -----------------------------------------------------------------------
// <copyright file="SystemHealthModel.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace SysAdminX.Core.Models;

/// <summary>
/// Represents the current system health metrics including CPU, RAM, disk, and network usage.
/// Used by the Dashboard module for real-time monitoring.
/// </summary>
public record SystemHealthModel
{
    /// <summary>
    /// Gets the CPU usage percentage (0-100).
    /// </summary>
    public double CpuUsagePercent { get; init; }

    /// <summary>
    /// Gets the CPU name/model.
    /// </summary>
    public string CpuName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the number of physical CPU cores.
    /// </summary>
    public int CpuCores { get; init; }

    /// <summary>
    /// Gets the number of logical processors (threads).
    /// </summary>
    public int CpuThreads { get; init; }

    /// <summary>
    /// Gets the CPU base clock speed in MHz.
    /// </summary>
    public uint CpuSpeedMhz { get; init; }

    /// <summary>
    /// Gets the total physical memory in bytes.
    /// </summary>
    public ulong TotalMemoryBytes { get; init; }

    /// <summary>
    /// Gets the available (free) physical memory in bytes.
    /// </summary>
    public ulong AvailableMemoryBytes { get; init; }

    /// <summary>
    /// Gets the used physical memory in bytes.
    /// </summary>
    public ulong UsedMemoryBytes => TotalMemoryBytes - AvailableMemoryBytes;

    /// <summary>
    /// Gets the RAM usage percentage (0-100).
    /// </summary>
    public double RamUsagePercent => TotalMemoryBytes > 0
        ? Math.Round((double)UsedMemoryBytes / TotalMemoryBytes * 100, 1)
        : 0;

    /// <summary>
    /// Gets the total disk space in bytes for the system drive.
    /// </summary>
    public ulong TotalDiskBytes { get; init; }

    /// <summary>
    /// Gets the available disk space in bytes for the system drive.
    /// </summary>
    public ulong AvailableDiskBytes { get; init; }

    /// <summary>
    /// Gets the used disk space in bytes for the system drive.
    /// </summary>
    public ulong UsedDiskBytes => TotalDiskBytes - AvailableDiskBytes;

    /// <summary>
    /// Gets the disk usage percentage (0-100).
    /// </summary>
    public double DiskUsagePercent => TotalDiskBytes > 0
        ? Math.Round((double)UsedDiskBytes / TotalDiskBytes * 100, 1)
        : 0;

    /// <summary>
    /// Gets the system drive letter (e.g., "C:").
    /// </summary>
    public string SystemDrive { get; init; } = "C:";

    /// <summary>
    /// Gets the network bytes sent per second.
    /// </summary>
    public long NetworkBytesSentPerSec { get; init; }

    /// <summary>
    /// Gets the network bytes received per second.
    /// </summary>
    public long NetworkBytesReceivedPerSec { get; init; }

    /// <summary>
    /// Gets the system uptime.
    /// </summary>
    public TimeSpan Uptime { get; init; }

    /// <summary>
    /// Gets the timestamp when this health snapshot was taken.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Represents Windows OS version and build information.
/// </summary>
public record WindowsInfoModel
{
    /// <summary>
    /// Gets the Windows edition (e.g., "Windows 11 Pro").
    /// </summary>
    public string Edition { get; init; } = string.Empty;

    /// <summary>
    /// Gets the Windows version (e.g., "23H2").
    /// </summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>
    /// Gets the OS build number (e.g., "22631.4890").
    /// </summary>
    public string BuildNumber { get; init; } = string.Empty;

    /// <summary>
    /// Gets the OS architecture (e.g., "64-bit").
    /// </summary>
    public string Architecture { get; init; } = string.Empty;

    /// <summary>
    /// Gets the computer name.
    /// </summary>
    public string ComputerName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the domain or workgroup name.
    /// </summary>
    public string DomainOrWorkgroup { get; init; } = string.Empty;

    /// <summary>
    /// Gets the last boot time.
    /// </summary>
    public DateTime LastBootTime { get; init; }

    /// <summary>
    /// Gets the Windows installation date.
    /// </summary>
    public DateTime InstallDate { get; init; }

    /// <summary>
    /// Gets the currently logged-in username.
    /// </summary>
    public string CurrentUser { get; init; } = string.Empty;
}

/// <summary>
/// Represents a single disk drive information for the dashboard.
/// </summary>
public record DiskInfoModel
{
    /// <summary>
    /// Gets the drive letter (e.g., "C:").
    /// </summary>
    public string DriveLetter { get; init; } = string.Empty;

    /// <summary>
    /// Gets the volume label.
    /// </summary>
    public string VolumeLabel { get; init; } = string.Empty;

    /// <summary>
    /// Gets the file system type (e.g., "NTFS").
    /// </summary>
    public string FileSystem { get; init; } = string.Empty;

    /// <summary>
    /// Gets the total size in bytes.
    /// </summary>
    public ulong TotalBytes { get; init; }

    /// <summary>
    /// Gets the free space in bytes.
    /// </summary>
    public ulong FreeBytes { get; init; }

    /// <summary>
    /// Gets the usage percentage.
    /// </summary>
    public double UsagePercent => TotalBytes > 0
        ? Math.Round((double)(TotalBytes - FreeBytes) / TotalBytes * 100, 1)
        : 0;

    /// <summary>
    /// Gets whether the disk is running low on space (less than 10% free).
    /// </summary>
    public bool IsLowSpace => TotalBytes > 0 && (double)FreeBytes / TotalBytes < 0.1;
}

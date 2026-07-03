// -----------------------------------------------------------------------
// <copyright file="ISystemHealthService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SysAdminX.Core.Models;

namespace SysAdminX.Core.Interfaces;

/// <summary>
/// Provides real-time system health monitoring capabilities.
/// Collects CPU, RAM, disk, and network metrics via WMI and performance counters.
/// </summary>
public interface ISystemHealthService
{
    /// <summary>
    /// Gets a snapshot of the current system health metrics.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the system health snapshot.</returns>
    Task<Result<SystemHealthModel>> GetSystemHealthAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets Windows OS version and build information.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing Windows info.</returns>
    Task<Result<WindowsInfoModel>> GetWindowsInfoAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets information about all disk drives.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing a list of disk information.</returns>
    Task<Result<List<DiskInfoModel>>> GetDiskInfoAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the current CPU usage percentage.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the CPU usage (0-100).</returns>
    Task<Result<double>> GetCpuUsageAsync(CancellationToken ct = default);

    /// <summary>
    /// Starts continuous health monitoring at the specified interval.
    /// </summary>
    /// <param name="intervalMs">The polling interval in milliseconds.</param>
    /// <param name="onUpdate">Callback invoked with updated health data.</param>
    /// <param name="ct">Cancellation token to stop monitoring.</param>
    Task StartMonitoringAsync(int intervalMs, Action<SystemHealthModel> onUpdate, CancellationToken ct = default);
}

// -----------------------------------------------------------------------
// <copyright file="IDriverManagerService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SysAdminX.Core.Models;

namespace SysAdminX.DriverManager.Services;

/// <summary>
/// Service for scanning and managing system drivers.
/// </summary>
public interface IDriverManagerService
{
    /// <summary>
    /// Scans the system for all installed device drivers.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing a list of drivers.</returns>
    Task<Result<List<DriverInfoModel>>> ScanDriversAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Exports all 3rd party drivers to a specific folder.
    /// </summary>
    /// <param name="destinationFolder">The folder to export to.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> ExportDriversAsync(string destinationFolder, CancellationToken ct = default);
    Task<Result<List<string>>> ScanDriverUpdatesAsync(CancellationToken ct = default);
    Task<Result<bool>> InstallDriverUpdatesAsync(CancellationToken ct = default);
}

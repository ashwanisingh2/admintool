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
    
    // NEW from SolasCarePro
    Task<Result<string>> DisableDriverWithBackupAsync(string hardwareId, bool safeMode, CancellationToken ct = default);
    Task<Result> EnableDriverAsync(string hardwareId, CancellationToken ct = default);
    Task<Result> RollbackDriverAsync(string hardwareId, CancellationToken ct = default);
    Task<Result> RestoreFromBackupAsync(string backupFilePath, CancellationToken ct = default);
    
    /// <summary>
    /// Exports all 3rd party drivers to a specific folder.
    /// </summary>
    /// <param name="destinationFolder">The folder to export to.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> ExportDriversAsync(string destinationFolder, CancellationToken ct = default);
    Task<Result<List<string>>> ScanDriverUpdatesAsync(CancellationToken ct = default);
    Task<Result<bool>> InstallDriverUpdatesAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Scans the system for unsigned drivers.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing a list of unsigned drivers.</returns>
    Task<Result<List<DriverInfoModel>>> ScanUnsignedDriversAsync(CancellationToken ct = default);

    /// <summary>
    /// Checks for the presence of OEM update tools.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing information about an OEM updater if found.</returns>
    Task<Result<OemUpdaterInfoModel?>> CheckOemUpdaterAsync(CancellationToken ct = default);
}

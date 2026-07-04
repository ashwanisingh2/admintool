// -----------------------------------------------------------------------
// <copyright file="IPatchManagerService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SysAdminX.Core.Models;

namespace SysAdminX.PatchManager.Services;

/// <summary>
/// Service for scanning and managing Windows Updates.
/// </summary>
public interface IPatchManagerService
{
    /// <summary>
    /// Retrieves a list of installed updates (Hotfixes/KBs).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing a list of updates.</returns>
    Task<Result<List<UpdateInfoModel>>> GetInstalledUpdatesAsync(CancellationToken ct = default);
    Task<Result<List<SoftwarePackageModel>>> GetSoftwareUpdatesAsync(CancellationToken ct = default);
    Task<Result<bool>> UpgradeAllSoftwareAsync(CancellationToken ct = default);

    /// <summary>
    /// Scans for missing Windows Updates.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing a list of missing updates.</returns>
    Task<Result<List<MissingUpdateModel>>> GetMissingUpdatesAsync(CancellationToken ct = default);

    /// <summary>
    /// Installs missing Windows Updates.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the installation outcome.</returns>
    Task<Result<InstallUpdatesResultModel>> InstallMissingUpdatesAsync(CancellationToken ct = default);
}

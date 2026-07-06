// -----------------------------------------------------------------------
// <copyright file="ISoftwareManagerService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SysAdminX.Core.Models;

namespace SysAdminX.Core.Interfaces;

/// <summary>
/// Service for enumerating installed software and installing / uninstalling
/// packages via winget or the registry uninstall strings.
/// </summary>
public interface ISoftwareManagerService
{
    /// <summary>Enumerates software registered under HKLM/HKCU Uninstall keys.</summary>
    Task<Result<IEnumerable<SoftwareItemModel>>> GetInstalledSoftwareAsync(CancellationToken ct = default);

    /// <summary>Launches the uninstaller for the given uninstall string.</summary>
    /// <param name="uninstallString">Raw value from the registry UninstallString value.</param>
    /// <param name="appName">Display name of the app, used for logging only.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<bool>> UninstallSoftwareAsync(string uninstallString, string appName, CancellationToken ct = default);

    /// <summary>Installs the given winget package id (e.g. <c>Google.Chrome</c>).</summary>
    Task<Result<bool>> InstallAppViaWingetAsync(string wingetId, CancellationToken ct = default);
}

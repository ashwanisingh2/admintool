// -----------------------------------------------------------------------
// <copyright file="ISettingsService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using SysAdminX.Core.Models;

namespace SysAdminX.Settings.Services;

/// <summary>
/// Service for managing application settings.
/// </summary>
public interface ISettingsService
{
    /// <summary>Loads the current app configuration from disk.</summary>
    Task<AppConfigModel> LoadSettingsAsync(CancellationToken ct = default);

    /// <summary>Saves the given app configuration to disk.</summary>
    Task SaveSettingsAsync(AppConfigModel config, CancellationToken ct = default);
}

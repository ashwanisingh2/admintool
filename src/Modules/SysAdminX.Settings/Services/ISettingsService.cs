// -----------------------------------------------------------------------
// <copyright file="ISettingsService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Threading.Tasks;
using SysAdminX.Core.Models;

namespace SysAdminX.Settings.Services;

/// <summary>
/// Service for managing application settings.
/// </summary>
public interface ISettingsService
{
    Task<AppConfigModel> LoadSettingsAsync();
    Task SaveSettingsAsync(AppConfigModel config);
}

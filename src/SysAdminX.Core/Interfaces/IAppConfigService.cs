// -----------------------------------------------------------------------
// <copyright file="IAppConfigService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using SysAdminX.Core.Models;

namespace SysAdminX.Core.Interfaces;

/// <summary>
/// Provides application configuration management.
/// Settings are stored locally in SQLite and can be exported/imported as JSON.
/// </summary>
public interface IAppConfigService
{
    /// <summary>
    /// Gets a configuration value by key.
    /// </summary>
    /// <typeparam name="T">The expected type of the value.</typeparam>
    /// <param name="key">The configuration key.</param>
    /// <param name="defaultValue">The default value if the key is not found.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The configuration value.</returns>
    Task<Result<T>> GetValueAsync<T>(string key, T defaultValue, CancellationToken ct = default);

    /// <summary>
    /// Sets a configuration value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Success or failure result.</returns>
    Task<Result<bool>> SetValueAsync<T>(string key, T value, CancellationToken ct = default);

    /// <summary>
    /// Gets the current application theme (Dark / Light / System).
    /// </summary>
    AppTheme CurrentTheme { get; }

    /// <summary>
    /// Sets the application theme.
    /// </summary>
    /// <param name="theme">The theme to apply.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<bool>> SetThemeAsync(AppTheme theme, CancellationToken ct = default);
}

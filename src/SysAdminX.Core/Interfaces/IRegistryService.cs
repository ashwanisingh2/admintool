// -----------------------------------------------------------------------
// <copyright file="IRegistryService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using SysAdminX.Core.Models;

namespace SysAdminX.Core.Interfaces;

/// <summary>
/// Service for interacting with the Windows Registry.
/// </summary>
public interface IRegistryService
{
    /// <summary>
    /// Reads a string value from the registry.
    /// </summary>
    /// <param name="hive">The registry hive (e.g., HKEY_LOCAL_MACHINE).</param>
    /// <param name="keyPath">The path to the registry key.</param>
    /// <param name="valueName">The name of the value to read.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the string value.</returns>
    Task<Result<string>> ReadStringValueAsync(string hive, string keyPath, string valueName, CancellationToken ct = default);

    /// <summary>
    /// Reads a DWORD (int) value from the registry.
    /// </summary>
    /// <param name="hive">The registry hive (e.g., HKEY_LOCAL_MACHINE).</param>
    /// <param name="keyPath">The path to the registry key.</param>
    /// <param name="valueName">The name of the value to read.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the int value.</returns>
    Task<Result<int>> ReadDWordValueAsync(string hive, string keyPath, string valueName, CancellationToken ct = default);
}

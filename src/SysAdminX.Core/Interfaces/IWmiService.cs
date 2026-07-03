// -----------------------------------------------------------------------
// <copyright file="IWmiService.cs" company="SysAdminX">
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
/// Provides an abstraction over Windows Management Instrumentation (WMI) queries.
/// All WMI access in the application must go through this service.
/// </summary>
public interface IWmiService
{
    /// <summary>
    /// Executes a WMI query and returns the results as a list of dictionaries.
    /// Each dictionary represents a WMI object with property name/value pairs.
    /// </summary>
    /// <param name="query">The WQL query string (e.g., "SELECT * FROM Win32_Processor").</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing a list of property dictionaries.</returns>
    Task<Result<List<Dictionary<string, object?>>>> QueryAsync(string query, CancellationToken ct = default);

    /// <summary>
    /// Executes a WMI query against a specific WMI namespace.
    /// </summary>
    /// <param name="wmiNamespace">The WMI namespace (e.g., "root\\CIMV2").</param>
    /// <param name="query">The WQL query string.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing a list of property dictionaries.</returns>
    Task<Result<List<Dictionary<string, object?>>>> QueryAsync(string wmiNamespace, string query, CancellationToken ct = default);

    /// <summary>
    /// Gets a single property value from a WMI class.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="query">The WQL query string.</param>
    /// <param name="propertyName">The property name to retrieve.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the property value.</returns>
    Task<Result<T>> GetSingleValueAsync<T>(string query, string propertyName, CancellationToken ct = default);
}

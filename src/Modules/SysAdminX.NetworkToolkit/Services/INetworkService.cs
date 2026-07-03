// -----------------------------------------------------------------------
// <copyright file="INetworkService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SysAdminX.Core.Models;

namespace SysAdminX.NetworkToolkit.Services;

/// <summary>
/// Service for network diagnostics and adapter management.
/// </summary>
public interface INetworkService
{
    /// <summary>
    /// Retrieves all network adapters.
    /// </summary>
    Task<Result<List<NetworkAdapterModel>>> GetNetworkAdaptersAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves active network connections (similar to netstat).
    /// </summary>
    Task<Result<List<NetworkConnectionModel>>> GetActiveConnectionsAsync(CancellationToken ct = default);
}

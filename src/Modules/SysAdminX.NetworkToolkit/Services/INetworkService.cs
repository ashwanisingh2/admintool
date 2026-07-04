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

    /// <summary>
    /// Sends a Wake-on-LAN Magic Packet to the specified MAC address.
    /// </summary>
    Task<Result<bool>> WakeOnLanAsync(string macAddress, CancellationToken ct = default);

    /// <summary>
    /// Scans a range of ports on the specified IP address.
    /// </summary>
    Task<Result<List<PortScanResultModel>>> PortScanAsync(string ipAddress, int startPort, int endPort, CancellationToken ct = default);

    /// <summary>
    /// Pings all IP addresses in the given subnet (1-254) to find active devices.
    /// </summary>
    Task<Result<List<PingSweepResultModel>>> PingSweepAsync(string baseIP, CancellationToken ct = default);
}

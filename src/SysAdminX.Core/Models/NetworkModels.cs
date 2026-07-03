// -----------------------------------------------------------------------
// <copyright file="NetworkAdapterModel.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace SysAdminX.Core.Models;

/// <summary>
/// Represents a network adapter and its configuration.
/// </summary>
public record NetworkAdapterModel
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string MacAddress { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string IPv4Address { get; init; } = string.Empty;
    public string IPv6Address { get; init; } = string.Empty;
    public string SubnetMask { get; init; } = string.Empty;
    public string DefaultGateway { get; init; } = string.Empty;
    public bool IsDhcpEnabled { get; init; }
}

/// <summary>
/// Represents an active network connection (TCP/UDP).
/// </summary>
public record NetworkConnectionModel
{
    public string Protocol { get; init; } = string.Empty;
    public string LocalAddress { get; init; } = string.Empty;
    public int LocalPort { get; init; }
    public string RemoteAddress { get; init; } = string.Empty;
    public int RemotePort { get; init; }
    public string State { get; init; } = string.Empty;
    public int ProcessId { get; init; }
    public string ProcessName { get; init; } = string.Empty;
}

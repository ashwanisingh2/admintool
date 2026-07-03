// -----------------------------------------------------------------------
// <copyright file="ISecurityService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SysAdminX.Core.Models;

namespace SysAdminX.SecurityCenter.Services;

/// <summary>
/// Service for querying security configurations (Defender, BitLocker, Firewall).
/// </summary>
public interface ISecurityService
{
    Task<DefenderStatusModel> GetDefenderStatusAsync(CancellationToken ct = default);
    Task<List<BitLockerStatusModel>> GetBitLockerStatusAsync(CancellationToken ct = default);
    Task<List<FirewallProfileModel>> GetFirewallProfilesAsync(CancellationToken ct = default);
}

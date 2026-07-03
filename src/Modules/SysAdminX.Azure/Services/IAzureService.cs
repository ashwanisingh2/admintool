// -----------------------------------------------------------------------
// <copyright file="IAzureService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SysAdminX.Core.Models;

namespace SysAdminX.Azure.Services;

/// <summary>
/// Service for querying Azure resources via Az PowerShell module.
/// </summary>
public interface IAzureService
{
    Task<List<AzureResourceGroupModel>> GetResourceGroupsAsync(string query, CancellationToken ct = default);
    Task<List<AzureVmModel>> GetVirtualMachinesAsync(string query, CancellationToken ct = default);
    Task<bool> IsConnectedAsync();
}

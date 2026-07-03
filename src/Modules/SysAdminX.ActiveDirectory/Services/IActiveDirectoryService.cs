// -----------------------------------------------------------------------
// <copyright file="IActiveDirectoryService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SysAdminX.Core.Models;

namespace SysAdminX.ActiveDirectory.Services;

/// <summary>
/// Service for querying Active Directory using RSAT.
/// </summary>
public interface IActiveDirectoryService
{
    Task<List<AdUserModel>> SearchUsersAsync(string query, CancellationToken ct = default);
    Task<List<AdGroupModel>> SearchGroupsAsync(string query, CancellationToken ct = default);
}

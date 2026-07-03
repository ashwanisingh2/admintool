// -----------------------------------------------------------------------
// <copyright file="IMicrosoft365Service.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SysAdminX.Core.Models;

namespace SysAdminX.Microsoft365.Services;

/// <summary>
/// Service for querying Microsoft 365 / Exchange Online / AzureAD.
/// </summary>
public interface IMicrosoft365Service
{
    Task<List<M365UserModel>> GetUsersAsync(string query, CancellationToken ct = default);
    Task<List<M365MailboxModel>> GetMailboxesAsync(string query, CancellationToken ct = default);
    Task<bool> IsConnectedAsync();
}

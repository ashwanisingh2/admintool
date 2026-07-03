// -----------------------------------------------------------------------
// <copyright file="ILogsService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using SysAdminX.Core.Models;

namespace SysAdminX.LogsViewer.Services;

/// <summary>
/// Service for reading and parsing application logs.
/// </summary>
public interface ILogsService
{
    Task<List<LogEntryModel>> GetRecentLogsAsync(int maxLines = 1000);
}

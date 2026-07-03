// -----------------------------------------------------------------------
// <copyright file="IReportService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SysAdminX.Core.Models;

namespace SysAdminX.Reports.Services;

/// <summary>
/// Service for generating system reports.
/// </summary>
public interface IReportService
{
    Task<Result<ReportModel>> GeneratePdfReportAsync(string outputPath, CancellationToken ct = default);
    Task<Result<ReportModel>> GenerateJsonReportAsync(string outputPath, CancellationToken ct = default);
    Task<List<ReportModel>> GetReportHistoryAsync();
}

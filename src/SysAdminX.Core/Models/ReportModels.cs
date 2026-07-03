// -----------------------------------------------------------------------
// <copyright file="ReportModels.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace SysAdminX.Core.Models;

/// <summary>
/// Represents a generated system report.
/// </summary>
public record ReportModel
{
    public string ReportId { get; init; } = Guid.NewGuid().ToString("N");
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime GeneratedAt { get; init; } = DateTime.Now;
    public string FilePath { get; init; } = string.Empty;
    public ReportType Type { get; init; } = ReportType.PDF;
    public long FileSizeBytes { get; init; }
}

public enum ReportType
{
    PDF,
    JSON,
    HTML
}

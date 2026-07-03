// -----------------------------------------------------------------------
// <copyright file="UpdateInfoModel.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace SysAdminX.Core.Models;

/// <summary>
/// Represents a Windows Update or Hotfix.
/// </summary>
public record UpdateInfoModel
{
    public string HotFixId { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string InstalledBy { get; init; } = string.Empty;
    public string InstalledOn { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;

    public bool IsSecurityUpdate => Description.Contains("Security", StringComparison.OrdinalIgnoreCase);
}

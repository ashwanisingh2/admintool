// -----------------------------------------------------------------------
// <copyright file="TroubleshootingActionModel.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace SysAdminX.Core.Models;

/// <summary>
/// Represents a troubleshooting action result.
/// </summary>
public record TroubleshootingActionModel
{
    public string ActionName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsSuccess { get; init; }
    public string OutputMessage { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.Now;
}

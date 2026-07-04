// -----------------------------------------------------------------------
// <copyright file="OemUpdaterInfoModel.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace SysAdminX.Core.Models;

public record OemUpdaterInfoModel
{
    public string Name { get; init; } = string.Empty;
    public string ExecutablePath { get; init; } = string.Empty;
    public bool IsInstalled { get; init; }
}

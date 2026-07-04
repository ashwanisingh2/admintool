// -----------------------------------------------------------------------
// <copyright file="CleanupItemModel.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace SysAdminX.Core.Models;

public class CleanupItemModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public bool IsSelected { get; set; }
    public string Path { get; set; } = string.Empty;
}

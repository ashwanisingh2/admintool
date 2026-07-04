// -----------------------------------------------------------------------
// <copyright file="PortableToolModel.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace SysAdminX.Core.Models;

public class PortableToolModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ExecutableName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public bool IsDownloaded { get; set; }
}

// -----------------------------------------------------------------------
// <copyright file="PingSweepResultModel.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace SysAdminX.Core.Models;

/// <summary>
/// Model for ping sweep results.
/// </summary>
public class PingSweepResultModel
{
    public string IPAddress { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public long RoundtripTime { get; set; }
}

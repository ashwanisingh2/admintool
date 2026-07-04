// -----------------------------------------------------------------------
// <copyright file="DriverInfoModel.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace SysAdminX.Core.Models;

/// <summary>
/// Represents a hardware device driver.
/// </summary>
public record DriverInfoModel
{
    public string DeviceName { get; init; } = string.Empty;
    public string HardwareId { get; init; } = string.Empty;
    public string ClassName { get; init; } = string.Empty;
    public string Manufacturer { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Date { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public string InfName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    
    public bool IsMicrosoft => Provider.Contains("Microsoft");
    public bool IsError => Status.Contains("Error") || Status.Contains("Stopped");
}

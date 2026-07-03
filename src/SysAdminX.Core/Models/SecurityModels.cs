// -----------------------------------------------------------------------
// <copyright file="SecurityModels.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace SysAdminX.Core.Models;

/// <summary>
/// Status of Windows Defender.
/// </summary>
public class DefenderStatusModel
{
    public bool IsEnabled { get; set; }
    public bool IsRealTimeProtectionEnabled { get; set; }
    public string AntivirusSignatureVersion { get; set; } = string.Empty;
    public DateTime? AntivirusSignatureLastUpdated { get; set; }
    public string ProductStatus { get; set; } = string.Empty;
}

/// <summary>
/// Status of BitLocker encryption on a volume.
/// </summary>
public class BitLockerStatusModel
{
    public string DriveLetter { get; set; } = string.Empty;
    public string ProtectionStatus { get; set; } = string.Empty;
    public string EncryptionMethod { get; set; } = string.Empty;
    public string VolumeType { get; set; } = string.Empty;
    public decimal EncryptionPercentage { get; set; }
}

/// <summary>
/// Status of Windows Firewall profiles.
/// </summary>
public class FirewallProfileModel
{
    public string Name { get; set; } = string.Empty; // Domain, Private, Public
    public bool IsEnabled { get; set; }
    public string DefaultInboundAction { get; set; } = string.Empty;
    public string DefaultOutboundAction { get; set; } = string.Empty;
}

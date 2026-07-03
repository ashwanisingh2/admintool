// -----------------------------------------------------------------------
// <copyright file="SecurityService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.SecurityCenter.Services;

/// <summary>
/// Implementation of <see cref="ISecurityService"/> using PowerShell.
/// </summary>
public class SecurityService : ISecurityService
{
    private readonly ILogger<SecurityService> _logger;
    private readonly IPowerShellService _psService;

    public SecurityService(ILogger<SecurityService> logger, IPowerShellService psService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _psService = psService ?? throw new ArgumentNullException(nameof(psService));
    }

    public async Task<DefenderStatusModel> GetDefenderStatusAsync(CancellationToken ct = default)
    {
        var result = new DefenderStatusModel();
        
        try
        {
            _logger.LogInformation("Querying Windows Defender status...");
            string script = "Get-MpComputerStatus | Select-Object AMServiceEnabled, RealTimeProtectionEnabled, AntivirusSignatureVersion, AntivirusSignatureLastUpdated, AMProductState | ConvertTo-Json";
            
            var psResult = await _psService.ExecuteScriptAsync(script);
            
            if (psResult.IsSuccess && !string.IsNullOrWhiteSpace(psResult.Value))
            {
                using var doc = System.Text.Json.JsonDocument.Parse(psResult.Value);
                var root = doc.RootElement;
                
                result.IsEnabled = root.TryGetProperty("AMServiceEnabled", out var am) && am.GetBoolean();
                result.IsRealTimeProtectionEnabled = root.TryGetProperty("RealTimeProtectionEnabled", out var rt) && rt.GetBoolean();
                result.AntivirusSignatureVersion = root.TryGetProperty("AntivirusSignatureVersion", out var sig) ? sig.GetString() ?? "" : "";
                
                if (root.TryGetProperty("AntivirusSignatureLastUpdated", out var dt) && dt.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    if (DateTime.TryParse(dt.GetString(), out var date))
                        result.AntivirusSignatureLastUpdated = date;
                }
                
                result.ProductStatus = root.TryGetProperty("AMProductState", out var st) ? st.GetInt32().ToString() : "Unknown";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Defender status");
            result.ProductStatus = "Error (Requires Admin / Defender Disabled)";
        }

        return result;
    }

    public async Task<List<BitLockerStatusModel>> GetBitLockerStatusAsync(CancellationToken ct = default)
    {
        var volumes = new List<BitLockerStatusModel>();
        
        try
        {
            _logger.LogInformation("Querying BitLocker status...");
            string script = "Get-BitLockerVolume | Select-Object MountPoint, VolumeStatus, EncryptionMethod, VolumeType, EncryptionPercentage | ConvertTo-Json";
            
            var psResult = await _psService.ExecuteScriptAsync(script);
            
            if (psResult.IsSuccess && !string.IsNullOrWhiteSpace(psResult.Value))
            {
                using var doc = System.Text.Json.JsonDocument.Parse(psResult.Value);
                
                if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var elem in doc.RootElement.EnumerateArray())
                    {
                        volumes.Add(ParseBitLocker(elem));
                    }
                }
                else if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    volumes.Add(ParseBitLocker(doc.RootElement));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get BitLocker status (requires elevation)");
            volumes.Add(new BitLockerStatusModel { ProtectionStatus = "Access Denied / Not Admin" });
        }

        return volumes;
    }

    private BitLockerStatusModel ParseBitLocker(System.Text.Json.JsonElement elem)
    {
        return new BitLockerStatusModel
        {
            DriveLetter = elem.TryGetProperty("MountPoint", out var mp) ? mp.GetString() ?? "" : "",
            ProtectionStatus = elem.TryGetProperty("VolumeStatus", out var vs) ? GetBitLockerVolumeStatus(vs.GetInt32()) : "Unknown",
            EncryptionMethod = elem.TryGetProperty("EncryptionMethod", out var em) ? GetEncryptionMethod(em.GetInt32()) : "Unknown",
            VolumeType = elem.TryGetProperty("VolumeType", out var vt) ? GetVolumeType(vt.GetInt32()) : "Unknown",
            EncryptionPercentage = elem.TryGetProperty("EncryptionPercentage", out var ep) && ep.ValueKind == System.Text.Json.JsonValueKind.Number ? ep.GetDecimal() : 0m
        };
    }
    
    // Enum mapping helpers based on Win32_EncryptableVolume
    private string GetBitLockerVolumeStatus(int status) => status switch {
        0 => "Fully Decrypted",
        1 => "Fully Encrypted",
        2 => "Encryption In Progress",
        3 => "Decryption In Progress",
        4 => "Encryption Paused",
        5 => "Decryption Paused",
        _ => "Unknown"
    };
    
    private string GetEncryptionMethod(int method) => method switch {
        0 => "None",
        1 => "AES 128 With Diffuser",
        2 => "AES 256 With Diffuser",
        3 => "AES 128",
        4 => "AES 256",
        5 => "Hardware Encryption",
        6 => "XTS AES 128",
        7 => "XTS AES 256",
        _ => "Unknown"
    };

    private string GetVolumeType(int type) => type switch {
        0 => "Operating System",
        1 => "Fixed Data",
        2 => "Removable Data",
        _ => "Unknown"
    };

    public async Task<List<FirewallProfileModel>> GetFirewallProfilesAsync(CancellationToken ct = default)
    {
        var profiles = new List<FirewallProfileModel>();
        
        try
        {
            _logger.LogInformation("Querying Firewall profiles...");
            string script = "Get-NetFirewallProfile | Select-Object Name, Enabled, DefaultInboundAction, DefaultOutboundAction | ConvertTo-Json";
            
            var psResult = await _psService.ExecuteScriptAsync(script);
            
            if (psResult.IsSuccess && !string.IsNullOrWhiteSpace(psResult.Value))
            {
                using var doc = System.Text.Json.JsonDocument.Parse(psResult.Value);
                
                if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var elem in doc.RootElement.EnumerateArray())
                    {
                        profiles.Add(ParseFirewall(elem));
                    }
                }
                else if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    profiles.Add(ParseFirewall(doc.RootElement));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Firewall status");
        }

        return profiles;
    }

    private FirewallProfileModel ParseFirewall(System.Text.Json.JsonElement elem)
    {
        return new FirewallProfileModel
        {
            Name = elem.TryGetProperty("Name", out var n) ? n.GetString() ?? "" : "",
            IsEnabled = elem.TryGetProperty("Enabled", out var e) && e.GetInt32() == 1,
            DefaultInboundAction = elem.TryGetProperty("DefaultInboundAction", out var ia) ? GetActionString(ia.GetInt32()) : "Unknown",
            DefaultOutboundAction = elem.TryGetProperty("DefaultOutboundAction", out var oa) ? GetActionString(oa.GetInt32()) : "Unknown"
        };
    }
    
    private string GetActionString(int action) => action == 2 ? "Block" : action == 4 ? "Allow" : "NotConfigured";
}

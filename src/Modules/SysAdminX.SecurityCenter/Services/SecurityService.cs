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

    public async Task<List<AntivirusProductModel>> GetAntivirusProductsAsync(CancellationToken ct = default)
    {
        var products = new List<AntivirusProductModel>();
        try
        {
            _logger.LogInformation("Querying Antivirus products from WMI SecurityCenter2...");
            string script = "Get-CimInstance -Namespace \"root\\SecurityCenter2\" -ClassName AntiVirusProduct -ErrorAction SilentlyContinue | Select-Object displayName, productState | ConvertTo-Json";
            var psResult = await _psService.ExecuteScriptAsync(script);
            
            if (psResult.IsSuccess && !string.IsNullOrWhiteSpace(psResult.Value))
            {
                using var doc = System.Text.Json.JsonDocument.Parse(psResult.Value);
                if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var elem in doc.RootElement.EnumerateArray())
                        products.Add(ParseAntivirusProduct(elem));
                }
                else if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    products.Add(ParseAntivirusProduct(doc.RootElement));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Antivirus products");
        }
        return products;
    }
    
    private AntivirusProductModel ParseAntivirusProduct(System.Text.Json.JsonElement elem)
    {
        var model = new AntivirusProductModel();
        model.DisplayName = elem.TryGetProperty("displayName", out var dn) ? dn.GetString() ?? "Unknown" : "Unknown";
        
        if (elem.TryGetProperty("productState", out var ps))
        {
            if (ps.ValueKind == System.Text.Json.JsonValueKind.Number)
            {
                int state = ps.GetInt32();
                model.ProductState = $"0x{state:X6} ({state})";
            }
            else
            {
                model.ProductState = ps.ToString() ?? "Unknown";
            }
        }
        else
        {
            model.ProductState = "Unknown";
        }
        return model;
    }

    public async Task<UacStatusModel> GetUacStatusAsync(CancellationToken ct = default)
    {
        var result = new UacStatusModel();
        try
        {
            _logger.LogInformation("Querying UAC status...");
            string script = @"
$path = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System'
$enableLua = (Get-ItemProperty -Path $path -Name EnableLUA -ErrorAction SilentlyContinue).EnableLUA
$consentPrompt = (Get-ItemProperty -Path $path -Name ConsentPromptBehaviorAdmin -ErrorAction SilentlyContinue).ConsentPromptBehaviorAdmin
[PSCustomObject]@{
    EnableLUA = $enableLua
    ConsentPromptBehaviorAdmin = $consentPrompt
} | ConvertTo-Json
";
            var psResult = await _psService.ExecuteScriptAsync(script);
            if (psResult.IsSuccess && !string.IsNullOrWhiteSpace(psResult.Value))
            {
                using var doc = System.Text.Json.JsonDocument.Parse(psResult.Value);
                var root = doc.RootElement;
                result.IsEnabled = root.TryGetProperty("EnableLUA", out var e) && e.ValueKind == System.Text.Json.JsonValueKind.Number && e.GetInt32() == 1;
                if (root.TryGetProperty("ConsentPromptBehaviorAdmin", out var cp) && cp.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    result.ConsentPromptBehavior = GetUacConsentPromptBehavior(cp.GetInt32());
                }
                else
                {
                    result.ConsentPromptBehavior = "Unknown";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get UAC status");
        }
        return result;
    }

    private string GetUacConsentPromptBehavior(int val) => val switch {
        0 => "Elevate without prompting",
        1 => "Prompt for credentials on secure desktop",
        2 => "Prompt for consent on secure desktop",
        3 => "Prompt for credentials",
        4 => "Prompt for consent",
        5 => "Prompt for consent for non-Windows binaries",
        _ => "Unknown"
    };

    public async Task<WindowsUpdateStatusModel> GetWindowsUpdateStatusAsync(CancellationToken ct = default)
    {
        var result = new WindowsUpdateStatusModel();
        try
        {
            _logger.LogInformation("Querying Windows Update status...");
            string script = @"
try {
    $au = New-Object -ComObject Microsoft.Update.AutoUpdate
    $results = $au.Results
    [PSCustomObject]@{
        ServiceEnabled = $au.ServiceEnabled
        LastSearchSuccessDate = $results.LastSearchSuccessDate
        LastInstallationSuccessDate = $results.LastInstallationSuccessDate
    } | ConvertTo-Json
} catch {
    '{}'
}
";
            var psResult = await _psService.ExecuteScriptAsync(script);
            if (psResult.IsSuccess && !string.IsNullOrWhiteSpace(psResult.Value))
            {
                using var doc = System.Text.Json.JsonDocument.Parse(psResult.Value);
                var root = doc.RootElement;
                result.ServiceEnabled = root.TryGetProperty("ServiceEnabled", out var se) && (se.ValueKind == System.Text.Json.JsonValueKind.True || (se.ValueKind == System.Text.Json.JsonValueKind.Number && se.GetInt32() == 1));
                if (root.TryGetProperty("LastSearchSuccessDate", out var lss) && lss.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    if (DateTime.TryParse(lss.GetString(), out var date)) result.LastSearchSuccessDate = date;
                }
                if (root.TryGetProperty("LastInstallationSuccessDate", out var lis) && lis.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    if (DateTime.TryParse(lis.GetString(), out var date)) result.LastInstallationSuccessDate = date;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Windows Update status");
        }
        return result;
    }

    public async Task<SecureBootStatusModel> GetSecureBootStatusAsync(CancellationToken ct = default)
    {
        var result = new SecureBootStatusModel();
        try
        {
            _logger.LogInformation("Querying Secure Boot status...");
            string script = @"
try {
    $sb = Confirm-SecureBootUEFI -ErrorAction Stop
    [PSCustomObject]@{ IsSupported = $true; IsEnabled = $sb } | ConvertTo-Json
} catch {
    [PSCustomObject]@{ IsSupported = $false; IsEnabled = $false } | ConvertTo-Json
}
";
            var psResult = await _psService.ExecuteScriptAsync(script);
            if (psResult.IsSuccess && !string.IsNullOrWhiteSpace(psResult.Value))
            {
                using var doc = System.Text.Json.JsonDocument.Parse(psResult.Value);
                var root = doc.RootElement;
                result.IsSupported = root.TryGetProperty("IsSupported", out var isupp) && (isupp.ValueKind == System.Text.Json.JsonValueKind.True || (isupp.ValueKind == System.Text.Json.JsonValueKind.Number && isupp.GetInt32() == 1));
                result.IsEnabled = root.TryGetProperty("IsEnabled", out var ien) && (ien.ValueKind == System.Text.Json.JsonValueKind.True || (ien.ValueKind == System.Text.Json.JsonValueKind.Number && ien.GetInt32() == 1));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Secure Boot status");
        }
        return result;
    }
}

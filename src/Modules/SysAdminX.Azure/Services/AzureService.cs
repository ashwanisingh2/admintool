// -----------------------------------------------------------------------
// <copyright file="AzureService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.Azure.Services;

/// <summary>
/// Implementation of <see cref="IAzureService"/> using PowerShell (Az Module).
/// </summary>
public class AzureService : IAzureService
{
    private readonly ILogger<AzureService> _logger;
    private readonly IPowerShellService _psService;
    private bool? _isModuleAvailable;

    public AzureService(ILogger<AzureService> logger, IPowerShellService psService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _psService = psService ?? throw new ArgumentNullException(nameof(psService));
    }

    public async Task<bool> IsConnectedAsync()
    {
        if (_isModuleAvailable.HasValue)
            return _isModuleAvailable.Value;

        // Check if Az module exists
        var result = await _psService.ExecuteScriptAsync("Get-Module -ListAvailable Az.Accounts | Select-Object -ExpandProperty Name");
        _isModuleAvailable = result.IsSuccess && !string.IsNullOrEmpty(result.Value) && 
                            result.Value.Contains("Az.Accounts", StringComparison.OrdinalIgnoreCase);
                             
        if (!_isModuleAvailable.Value)
        {
            _logger.LogWarning("Azure PowerShell module (Az) is not available. Please install the Az module.");
        }
        
        return _isModuleAvailable.Value;
    }

    public async Task<List<AzureResourceGroupModel>> GetResourceGroupsAsync(string query, CancellationToken ct = default)
    {
        var rgs = new List<AzureResourceGroupModel>();
        
        if (!await IsConnectedAsync())
            return rgs;

        try
        {
            _logger.LogInformation("Searching Azure Resource Groups for: {Query}", query);
            
            string script = $@"
                try {{
                    if (Get-Command Get-AzResourceGroup -ErrorAction SilentlyContinue) {{
                        $query = ""*{query}*""
                        Get-AzResourceGroup -Name $query -ErrorAction Stop | 
                        Select-Object ResourceGroupName, Location, ProvisioningState | 
                        ConvertTo-Json -Compress
                    }} else {{
                        Write-Error ""Az module not loaded or connected.""
                    }}
                }} catch {{
                    Write-Error $_
                }}
            ";
            
            var psResult = await _psService.ExecuteScriptAsync(script);
            
            if (psResult.IsSuccess && !string.IsNullOrWhiteSpace(psResult.Value) && !psResult.Value.Contains("Write-Error"))
            {
                using var doc = JsonDocument.Parse(psResult.Value);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var elem in doc.RootElement.EnumerateArray())
                    {
                        rgs.Add(ParseResourceGroup(elem));
                    }
                }
                else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    rgs.Add(ParseResourceGroup(doc.RootElement));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search Azure Resource Groups");
        }

        return rgs;
    }

    public async Task<List<AzureVmModel>> GetVirtualMachinesAsync(string query, CancellationToken ct = default)
    {
        var vms = new List<AzureVmModel>();
        
        if (!await IsConnectedAsync())
            return vms;

        try
        {
            _logger.LogInformation("Searching Azure VMs for: {Query}", query);
            
            string script = $@"
                try {{
                    if (Get-Command Get-AzVM -ErrorAction SilentlyContinue) {{
                        $query = ""*{query}*""
                        Get-AzVM -Name $query -ErrorAction Stop | 
                        Select-Object Name, ResourceGroupName, Location, @{{N='VmSize';E={{$_.HardwareProfile.VmSize}}}}, @{{N='OsType';E={{$_.StorageProfile.OsDisk.OsType}}}}, ProvisioningState | 
                        ConvertTo-Json -Compress
                    }} else {{
                        Write-Error ""Az module not loaded or connected.""
                    }}
                }} catch {{
                    Write-Error $_
                }}
            ";
            
            var psResult = await _psService.ExecuteScriptAsync(script);
            
            if (psResult.IsSuccess && !string.IsNullOrWhiteSpace(psResult.Value) && !psResult.Value.Contains("Write-Error"))
            {
                using var doc = JsonDocument.Parse(psResult.Value);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var elem in doc.RootElement.EnumerateArray())
                    {
                        vms.Add(ParseVm(elem));
                    }
                }
                else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    vms.Add(ParseVm(doc.RootElement));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search Azure VMs");
        }

        return vms;
    }

    private AzureResourceGroupModel ParseResourceGroup(JsonElement elem)
    {
        return new AzureResourceGroupModel
        {
            ResourceGroupName = GetString(elem, "ResourceGroupName"),
            Location = GetString(elem, "Location"),
            ProvisioningState = GetString(elem, "ProvisioningState")
        };
    }

    private AzureVmModel ParseVm(JsonElement elem)
    {
        return new AzureVmModel
        {
            Name = GetString(elem, "Name"),
            ResourceGroupName = GetString(elem, "ResourceGroupName"),
            Location = GetString(elem, "Location"),
            VmSize = GetString(elem, "VmSize"),
            OsType = GetString(elem, "OsType"),
            ProvisioningState = GetString(elem, "ProvisioningState")
        };
    }
    
    private string GetString(JsonElement elem, string propName)
    {
        if (elem.TryGetProperty(propName, out var prop) && prop.ValueKind != JsonValueKind.Null)
        {
            return prop.ToString() ?? "";
        }
        return "";
    }
}

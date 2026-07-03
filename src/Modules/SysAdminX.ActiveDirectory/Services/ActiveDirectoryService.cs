// -----------------------------------------------------------------------
// <copyright file="ActiveDirectoryService.cs" company="SysAdminX">
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

namespace SysAdminX.ActiveDirectory.Services;

/// <summary>
/// Implementation of <see cref="IActiveDirectoryService"/> using PowerShell (RSAT).
/// </summary>
public class ActiveDirectoryService : IActiveDirectoryService
{
    private readonly ILogger<ActiveDirectoryService> _logger;
    private readonly IPowerShellService _psService;
    private bool? _isModuleAvailable;

    public ActiveDirectoryService(ILogger<ActiveDirectoryService> logger, IPowerShellService psService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _psService = psService ?? throw new ArgumentNullException(nameof(psService));
    }

    private async Task<bool> CheckModuleAvailabilityAsync()
    {
        if (_isModuleAvailable.HasValue)
            return _isModuleAvailable.Value;

        var result = await _psService.ExecuteScriptAsync("Get-Module -ListAvailable -Name ActiveDirectory | Select-Object -ExpandProperty Name");
        _isModuleAvailable = result.IsSuccess && !string.IsNullOrEmpty(result.Value) && result.Value.Contains("ActiveDirectory", StringComparison.OrdinalIgnoreCase);
        
        if (!_isModuleAvailable.Value)
        {
            _logger.LogWarning("ActiveDirectory PowerShell module is not available. Please install RSAT: Active Directory Domain Services and Lightweight Directory Services Tools.");
        }
        
        return _isModuleAvailable.Value;
    }

    public async Task<List<AdUserModel>> SearchUsersAsync(string query, CancellationToken ct = default)
    {
        var users = new List<AdUserModel>();
        
        if (!await CheckModuleAvailabilityAsync())
            return users;

        try
        {
            _logger.LogInformation("Searching AD users for: {Query}", query);
            
            // Build filter
            string filter = string.IsNullOrWhiteSpace(query) 
                ? "*" 
                : $"Name -like '*{query}*' -or SamAccountName -like '*{query}*'";

            string script = $@"
                try {{
                    Get-ADUser -Filter ""{filter}"" -Properties Enabled, GivenName, Surname -ErrorAction Stop | 
                    Select-Object Name, SamAccountName, UserPrincipalName, Enabled, GivenName, Surname | 
                    ConvertTo-Json -Compress
                }} catch {{
                    Write-Error $_
                }}
            ";
            
            var psResult = await _psService.ExecuteScriptAsync(script);
            
            if (psResult.IsSuccess && !string.IsNullOrWhiteSpace(psResult.Value))
            {
                using var doc = JsonDocument.Parse(psResult.Value);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var elem in doc.RootElement.EnumerateArray())
                    {
                        users.Add(ParseUser(elem));
                    }
                }
                else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    users.Add(ParseUser(doc.RootElement));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search AD users");
        }

        return users;
    }

    public async Task<List<AdGroupModel>> SearchGroupsAsync(string query, CancellationToken ct = default)
    {
        var groups = new List<AdGroupModel>();
        
        if (!await CheckModuleAvailabilityAsync())
            return groups;

        try
        {
            _logger.LogInformation("Searching AD groups for: {Query}", query);
            
            // Build filter
            string filter = string.IsNullOrWhiteSpace(query) 
                ? "*" 
                : $"Name -like '*{query}*' -or SamAccountName -like '*{query}*'";

            string script = $@"
                try {{
                    Get-ADGroup -Filter ""{filter}"" -ErrorAction Stop | 
                    Select-Object Name, SamAccountName, GroupCategory, GroupScope | 
                    ConvertTo-Json -Compress
                }} catch {{
                    Write-Error $_
                }}
            ";
            
            var psResult = await _psService.ExecuteScriptAsync(script);
            
            if (psResult.IsSuccess && !string.IsNullOrWhiteSpace(psResult.Value))
            {
                using var doc = JsonDocument.Parse(psResult.Value);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var elem in doc.RootElement.EnumerateArray())
                    {
                        groups.Add(ParseGroup(elem));
                    }
                }
                else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    groups.Add(ParseGroup(doc.RootElement));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search AD groups");
        }

        return groups;
    }

    private AdUserModel ParseUser(JsonElement elem)
    {
        return new AdUserModel
        {
            Name = GetString(elem, "Name"),
            SamAccountName = GetString(elem, "SamAccountName"),
            UserPrincipalName = GetString(elem, "UserPrincipalName"),
            Enabled = elem.TryGetProperty("Enabled", out var enabled) && enabled.GetBoolean(),
            GivenName = GetString(elem, "GivenName"),
            Surname = GetString(elem, "Surname")
        };
    }

    private AdGroupModel ParseGroup(JsonElement elem)
    {
        return new AdGroupModel
        {
            Name = GetString(elem, "Name"),
            SamAccountName = GetString(elem, "SamAccountName"),
            GroupCategory = elem.TryGetProperty("GroupCategory", out var gc) ? GetGroupCategoryString(gc.GetInt32()) : "",
            GroupScope = elem.TryGetProperty("GroupScope", out var gs) ? GetGroupScopeString(gs.GetInt32()) : ""
        };
    }
    
    private string GetString(JsonElement elem, string propName)
    {
        if (elem.TryGetProperty(propName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString() ?? "";
        }
        return "";
    }
    
    private string GetGroupCategoryString(int cat) => cat == 0 ? "Distribution" : "Security";
    private string GetGroupScopeString(int scope) => scope == 0 ? "Local" : scope == 1 ? "Global" : "Universal";
}

// -----------------------------------------------------------------------
// <copyright file="Microsoft365Service.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.Microsoft365.Services;

/// <summary>
/// Implementation of <see cref="IMicrosoft365Service"/> using PowerShell (ExchangeOnlineManagement / Microsoft.Graph).
/// </summary>
public class Microsoft365Service : IMicrosoft365Service
{
    private readonly ILogger<Microsoft365Service> _logger;
    private readonly IPowerShellService _psService;
    private bool? _isModuleAvailable;

    public Microsoft365Service(ILogger<Microsoft365Service> logger, IPowerShellService psService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _psService = psService ?? throw new ArgumentNullException(nameof(psService));
    }

    public async Task<bool> IsConnectedAsync()
    {
        if (_isModuleAvailable.HasValue)
            return _isModuleAvailable.Value;

        // Check if EXO or Graph modules exist
        var result = await _psService.ExecuteScriptAsync("Get-Module -ListAvailable ExchangeOnlineManagement, Microsoft.Graph.Users | Select-Object -ExpandProperty Name");
        _isModuleAvailable = result.IsSuccess && !string.IsNullOrEmpty(result.Value) && 
                            (result.Value.Contains("ExchangeOnlineManagement", StringComparison.OrdinalIgnoreCase) || 
                             result.Value.Contains("Microsoft.Graph.Users", StringComparison.OrdinalIgnoreCase));
                             
        if (!_isModuleAvailable.Value)
        {
            _logger.LogWarning("M365 PowerShell modules are not available. Please install ExchangeOnlineManagement and Microsoft.Graph.");
        }
        
        return _isModuleAvailable.Value;
    }

    public async Task<List<M365UserModel>> GetUsersAsync(string query, CancellationToken ct = default)
    {
        var users = new List<M365UserModel>();
        
        if (!await IsConnectedAsync())
            return users;

        try
        {
            _logger.LogInformation("Searching M365 users for: {Query}", query);
            
            // Try MSOnline (Get-MsolUser) or Graph (Get-MgUser)
            // We use a simplified script that catches if not connected.
            string script = $@"
                try {{
                    $query = ""*{query}*""
                    if (Get-Command Get-MgUser -ErrorAction SilentlyContinue) {{
                        Get-MgUser -Filter ""startswith(displayName,'$query')"" -Property DisplayName, UserPrincipalName, AccountEnabled, AssignedLicenses -ErrorAction Stop | 
                        Select-Object DisplayName, UserPrincipalName, @{{Name='IsLicensed';Expression={{$_.AssignedLicenses.Count -gt 0}}}}, @{{Name='BlockCredential';Expression={{!$_.AccountEnabled}}}} | 
                        ConvertTo-Json -Compress
                    }} elseif (Get-Command Get-MsolUser -ErrorAction SilentlyContinue) {{
                        Get-MsolUser -SearchString ""$query"" -ErrorAction Stop | 
                        Select-Object DisplayName, UserPrincipalName, IsLicensed, BlockCredential | 
                        ConvertTo-Json -Compress
                    }} else {{
                        Write-Error ""No supported M365 module found connected.""
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
            _logger.LogError(ex, "Failed to search M365 users");
        }

        return users;
    }

    public async Task<List<M365MailboxModel>> GetMailboxesAsync(string query, CancellationToken ct = default)
    {
        var mailboxes = new List<M365MailboxModel>();
        
        if (!await IsConnectedAsync())
            return mailboxes;

        try
        {
            _logger.LogInformation("Searching EXO mailboxes for: {Query}", query);
            
            string script = $@"
                try {{
                    if (Get-Command Get-EXOMailbox -ErrorAction SilentlyContinue) {{
                        $mbxs = Get-EXOMailbox -Anr ""{query}"" -ErrorAction Stop | Select-Object -First 10
                        $results = @()
                        foreach ($m in $mbxs) {{
                            $stats = Get-EXOMailboxStatistics -Identity $m.UserPrincipalName -ErrorAction SilentlyContinue
                            $results += [PSCustomObject]@{{
                                DisplayName = $m.DisplayName
                                PrimarySmtpAddress = $m.PrimarySmtpAddress
                                IssueWarningQuota = $m.IssueWarningQuota
                                TotalItemSize = $stats.TotalItemSize
                                ItemCount = $stats.ItemCount
                            }}
                        }}
                        $results | ConvertTo-Json -Compress
                    }} else {{
                        Write-Error ""ExchangeOnlineManagement not loaded or connected.""
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
                        mailboxes.Add(ParseMailbox(elem));
                    }
                }
                else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    mailboxes.Add(ParseMailbox(doc.RootElement));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search EXO mailboxes");
        }

        return mailboxes;
    }

    private M365UserModel ParseUser(JsonElement elem)
    {
        return new M365UserModel
        {
            DisplayName = GetString(elem, "DisplayName"),
            UserPrincipalName = GetString(elem, "UserPrincipalName"),
            IsLicensed = elem.TryGetProperty("IsLicensed", out var lic) && lic.ValueKind == JsonValueKind.True,
            BlockCredential = elem.TryGetProperty("BlockCredential", out var block) ? (block.ValueKind == JsonValueKind.True ? "Blocked" : "Allowed") : "Unknown",
            Licenses = "Assigned"
        };
    }

    private M365MailboxModel ParseMailbox(JsonElement elem)
    {
        return new M365MailboxModel
        {
            DisplayName = GetString(elem, "DisplayName"),
            PrimarySmtpAddress = GetString(elem, "PrimarySmtpAddress"),
            IssueWarningQuota = GetString(elem, "IssueWarningQuota"),
            TotalItemSize = GetString(elem, "TotalItemSize"),
            ItemCount = GetString(elem, "ItemCount")
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

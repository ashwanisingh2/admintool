// -----------------------------------------------------------------------
// <copyright file="SecurityCenterViewModel.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Models;
using SysAdminX.SecurityCenter.Services;

namespace SysAdminX.SecurityCenter.ViewModels;

/// <summary>
/// ViewModel for the Security Center module.
/// </summary>
public partial class SecurityCenterViewModel : ObservableObject
{
    private readonly ILogger<SecurityCenterViewModel> _logger;
    private readonly ISecurityService _securityService;

    [ObservableProperty]
    private DefenderStatusModel _defenderStatus = new();

    [ObservableProperty]
    private ObservableCollection<BitLockerStatusModel> _bitLockerVolumes = new();

    [ObservableProperty]
    private ObservableCollection<FirewallProfileModel> _firewallProfiles = new();

    [ObservableProperty]
    private bool _isLoading;

    public SecurityCenterViewModel(ILogger<SecurityCenterViewModel> logger, ISecurityService securityService)
    {
        _logger = logger;
        _securityService = securityService;
        
        LoadDataCommand.Execute(null);
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            DefenderStatus = await _securityService.GetDefenderStatusAsync();
            
            var volumes = await _securityService.GetBitLockerStatusAsync();
            BitLockerVolumes.Clear();
            foreach (var vol in volumes)
            {
                BitLockerVolumes.Add(vol);
            }
            
            var profiles = await _securityService.GetFirewallProfilesAsync();
            FirewallProfiles.Clear();
            foreach (var p in profiles)
            {
                FirewallProfiles.Add(p);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }
}

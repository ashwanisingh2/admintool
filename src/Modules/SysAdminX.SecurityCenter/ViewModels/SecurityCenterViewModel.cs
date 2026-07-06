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
    private ObservableCollection<AntivirusProductModel> _antivirusProducts = new();

    [ObservableProperty]
    private UacStatusModel _uacStatus = new();

    [ObservableProperty]
    private WindowsUpdateStatusModel _windowsUpdateStatus = new();

    [ObservableProperty]
    private SecureBootStatusModel _secureBootStatus = new();

    [ObservableProperty]
    private bool _isLoading;

    public SecurityCenterViewModel(ILogger<SecurityCenterViewModel> logger, ISecurityService securityService)
    {
        _logger = logger;
        _securityService = securityService;

    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        try
        {
            _logger.LogInformation("Loading Security Center data (parallel queries)...");

            // Run all 7 independent queries in parallel — they each launch a
            // separate PowerShell host, so sequential awaiting previously took
            // 5-10s on a cold cache. Parallel cuts that to ~1-2s.
            var defenderTask     = _securityService.GetDefenderStatusAsync();
            var bitLockerTask    = _securityService.GetBitLockerStatusAsync();
            var firewallTask     = _securityService.GetFirewallProfilesAsync();
            var avProductsTask   = _securityService.GetAntivirusProductsAsync();
            var uacTask          = _securityService.GetUacStatusAsync();
            var wuTask           = _securityService.GetWindowsUpdateStatusAsync();
            var secureBootTask   = _securityService.GetSecureBootStatusAsync();

            await Task.WhenAll(defenderTask, bitLockerTask, firewallTask,
                               avProductsTask, uacTask, wuTask, secureBootTask);

            // Marshal all observable-collection updates onto the UI thread
            // in a single Dispatcher.Invoke so we only pay one cross-thread
            // round-trip instead of seven.
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                DefenderStatus = defenderTask.Result;

                BitLockerVolumes.Clear();
                foreach (var vol in bitLockerTask.Result) BitLockerVolumes.Add(vol);

                FirewallProfiles.Clear();
                foreach (var p in firewallTask.Result) FirewallProfiles.Add(p);

                AntivirusProducts.Clear();
                foreach (var av in avProductsTask.Result) AntivirusProducts.Add(av);

                UacStatus = uacTask.Result;
                WindowsUpdateStatus = wuTask.Result;
                SecureBootStatus = secureBootTask.Result;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Security Center data");
        }
        finally
        {
            IsLoading = false;
        }
    }
}

// -----------------------------------------------------------------------
// <copyright file="RemoteSupportViewModel.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SysAdminX.RemoteSupport.Services;

namespace SysAdminX.RemoteSupport.ViewModels;

/// <summary>
/// ViewModel for the Remote Support module.
/// </summary>
public partial class RemoteSupportViewModel : ObservableObject
{
    private readonly IRemoteSupportService _remoteService;

    [ObservableProperty]
    private string _targetHostname = string.Empty;

    public RemoteSupportViewModel(IRemoteSupportService remoteService)
    {
        _remoteService = remoteService;
    }

    [RelayCommand]
    private async Task LaunchRdpAsync()
    {
        await _remoteService.LaunchRdpAsync(TargetHostname);
    }

    [RelayCommand]
    private async Task LaunchComputerManagementAsync()
    {
        await _remoteService.LaunchComputerManagementAsync(TargetHostname);
    }
    
    [RelayCommand]
    private async Task LaunchPsExecAsync()
    {
        await _remoteService.LaunchPsExecAsync(TargetHostname);
    }
}

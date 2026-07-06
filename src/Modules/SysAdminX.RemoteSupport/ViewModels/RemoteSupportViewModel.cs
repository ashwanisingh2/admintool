// -----------------------------------------------------------------------
// <copyright file="RemoteSupportViewModel.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.RemoteSupport.Services;

namespace SysAdminX.RemoteSupport.ViewModels;

/// <summary>
/// ViewModel for the Remote Support module.
///
/// Improvements applied:
///   - All launch commands now wrapped in try/catch so a thrown exception
///     (e.g. mstsc.exe missing, RPC unavailable) is surfaced as a toast
///     instead of crashing via DispatcherUnhandledException.
///   - IToastNotificationService injected for success / failure feedback.
/// </summary>
public partial class RemoteSupportViewModel : ObservableObject
{
    private readonly IRemoteSupportService _remoteService;
    private readonly IToastNotificationService _toastService;
    private readonly ILogger<RemoteSupportViewModel> _logger;

    [ObservableProperty]
    private string _targetHostname = string.Empty;

    public RemoteSupportViewModel(
        IRemoteSupportService remoteService,
        IToastNotificationService toastService,
        ILogger<RemoteSupportViewModel> logger)
    {
        _remoteService = remoteService ?? throw new ArgumentNullException(nameof(remoteService));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private async Task LaunchAsync(Func<string, Task> launcher, string toolName)
    {
        if (string.IsNullOrWhiteSpace(TargetHostname))
        {
            _toastService.ShowWarning($"Cannot launch {toolName}", "Enter a target hostname or IP address first.");
            return;
        }

        try
        {
            await launcher(TargetHostname);
            _toastService.ShowSuccess($"{toolName} launched", $"Connecting to {TargetHostname}...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Tool} launch failed for {Host}", toolName, TargetHostname);
            _toastService.ShowError($"{toolName} failed to launch", ex.Message);
        }
    }

    [RelayCommand]
    private Task LaunchRdpAsync()
        => LaunchAsync(_remoteService.LaunchRdpAsync, "Remote Desktop");

    [RelayCommand]
    private Task LaunchComputerManagementAsync()
        => LaunchAsync(_remoteService.LaunchComputerManagementAsync, "Computer Management");

    [RelayCommand]
    private Task LaunchRemoteCommandPromptAsync()
        => LaunchAsync(_remoteService.LaunchRemoteCommandPromptAsync, "Remote Cmd");

    [RelayCommand]
    private Task LaunchRemotePowerShellAsync()
        => LaunchAsync(_remoteService.LaunchRemotePowerShellAsync, "Remote PowerShell");
}

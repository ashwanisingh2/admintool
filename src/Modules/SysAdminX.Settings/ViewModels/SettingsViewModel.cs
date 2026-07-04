// -----------------------------------------------------------------------
// <copyright file="SettingsViewModel.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Models;
using SysAdminX.Settings.Services;
using Wpf.Ui.Appearance;

namespace SysAdminX.Settings.ViewModels;

/// <summary>
/// ViewModel for the Settings module.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ILogger<SettingsViewModel> _logger;
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private AppConfigModel _config = new();

    [ObservableProperty]
    private bool _isSaved;

    public SettingsViewModel(ILogger<SettingsViewModel> logger, ISettingsService settingsService)
    {
        _logger = logger;
        _settingsService = settingsService;

    }

    [RelayCommand]
    public async Task LoadSettingsAsync()
    {
        Config = await _settingsService.LoadSettingsAsync();
    }

    [RelayCommand]
    public async Task SaveSettingsAsync()
    {
        IsSaved = false;
        await _settingsService.SaveSettingsAsync(Config);
        IsSaved = true;

        // Apply theme immediately
        if (Config.Theme.Equals("Dark", StringComparison.OrdinalIgnoreCase))
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Dark);
        }
        else
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Light);
        }
        
        // Let the 'Saved' indicator show for a few seconds
        await Task.Delay(3000);
        IsSaved = false;
    }
}

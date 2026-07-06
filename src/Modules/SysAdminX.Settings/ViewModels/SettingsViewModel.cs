// -----------------------------------------------------------------------
// <copyright file="SettingsViewModel.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;
using SysAdminX.Settings.Services;
using Wpf.Ui.Appearance;

namespace SysAdminX.Settings.ViewModels;

/// <summary>
/// ViewModel for the Settings module.
///
/// Improvements applied:
///   - LoadSettingsAsync and SaveSettingsAsync wrapped in try/catch/finally
///     so an exception can no longer leave IsSaved stuck on.
///   - Real cancellation token propagation.
///   - Toast notifications on save outcome.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ILogger<SettingsViewModel> _logger;
    private readonly ISettingsService _settingsService;
    private readonly IToastNotificationService _toastService;

    [ObservableProperty]
    private AppConfigModel _config = new();

    [ObservableProperty]
    private bool _isSaved;

    [ObservableProperty]
    private bool _isSaving;

    public SettingsViewModel(
        ILogger<SettingsViewModel> logger,
        ISettingsService settingsService,
        IToastNotificationService toastService)
    {
        _logger = logger;
        _settingsService = settingsService;
        _toastService = toastService;
    }

    [RelayCommand]
    public async Task LoadSettingsAsync(CancellationToken ct = default)
    {
        try
        {
            Config = await _settingsService.LoadSettingsAsync(ct);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Load settings cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings.");
            _toastService.ShowError("Failed to load settings", ex.Message);
        }
    }

    [RelayCommand]
    public async Task SaveSettingsAsync(CancellationToken ct = default)
    {
        if (IsSaving) return;
        IsSaving = true;
        IsSaved = false;

        try
        {
            await _settingsService.SaveSettingsAsync(Config, ct);
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

            _toastService.ShowSuccess("Settings saved", "Your preferences have been saved.");

            // Let the 'Saved' indicator show for a few seconds
            await Task.Delay(3000, ct);
            IsSaved = false;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Save settings cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings.");
            _toastService.ShowError("Failed to save settings", ex.Message);
        }
        finally
        {
            IsSaving = false;
        }
    }
}

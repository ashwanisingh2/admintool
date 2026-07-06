using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.PerformanceMode.Models;
using SysAdminX.PerformanceMode.Services;

namespace SysAdminX.PerformanceMode.ViewModels;

/// <summary>
/// ViewModel for the Performance Mode module.
///
/// Improvements applied:
///   - All async commands wrapped in try/finally with toast feedback.
///   - Constructor no longer fires off a fire-and-forget load — the view's
///     Loaded handler now triggers the initial refresh.
/// </summary>
public partial class PerformanceModeViewModel : ObservableObject
{
    private readonly IPerformanceModeService _service;
    private readonly IToastNotificationService _toastService;
    private readonly ILogger<PerformanceModeViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<PerformanceProfile> _profiles = new();

    [ObservableProperty]
    private bool _isApplying;

    public PerformanceModeViewModel(
        IPerformanceModeService service,
        IToastNotificationService toastService,
        ILogger<PerformanceModeViewModel> logger)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        InitializeProfiles();
    }

    private void InitializeProfiles()
    {
        Profiles = new ObservableCollection<PerformanceProfile>
        {
            new PerformanceProfile
            {
                Id = "gaming",
                Name = "Gaming Mode",
                Description = "Ultimate Performance power plan, Game Mode ON, HAGS ON, background apps disabled.",
                IconName = "XboxController24"
            },
            new PerformanceProfile
            {
                Id = "work",
                Name = "Work Mode",
                Description = "Balanced power plan, Game Mode OFF, background apps enabled.",
                IconName = "Briefcase24"
            },
            new PerformanceProfile
            {
                Id = "powersaver",
                Name = "Power Saver",
                Description = "Power Saver plan, Game Mode OFF, background apps disabled.",
                IconName = "Battery24"
            }
        };
    }

    [RelayCommand]
    public async Task LoadCurrentProfileAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _service.GetCurrentProfileAsync(ct);
            if (result.IsSuccess)
            {
                foreach (var profile in Profiles)
                {
                    profile.IsActive = profile.Id == result.Value;
                }
            }
            else
            {
                _logger.LogWarning("Failed to load current performance profile: {Error}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LoadCurrentProfile threw an exception.");
        }
    }

    [RelayCommand]
    public async Task ApplyProfileAsync(string profileId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(profileId) || IsApplying) return;

        IsApplying = true;
        try
        {
            var result = await _service.ApplyProfileAsync(profileId, ct);
            if (result.IsSuccess)
            {
                _toastService.ShowSuccess("Performance profile applied",
                    $"Profile '{profileId}' was applied.");
                await LoadCurrentProfileAsync(ct);
            }
            else
            {
                _toastService.ShowError("Failed to apply profile", result.ErrorMessage ?? "Unknown error.");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Apply profile cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Apply profile threw an exception.");
            _toastService.ShowError("Failed to apply profile", ex.Message);
        }
        finally
        {
            IsApplying = false;
        }
    }
}

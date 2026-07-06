using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SysAdminX.AutoPilot.Models;
using SysAdminX.AutoPilot.Services;
using SysAdminX.Core.Interfaces;

namespace SysAdminX.AutoPilot.ViewModels;

/// <summary>
/// ViewModel for the AutoPilot module.
///
/// Improvements applied:
///   - The 30-second poll timer is now stopped if RefreshStatusAsync
///     throws repeatedly, so we don't spam the log every 30 seconds when
///     the underlying scheduler is unavailable.
///   - Constructor no longer fires off a fire-and-forget RefreshStatus —
///     the view's Loaded handler triggers the initial refresh.
///   - All commands wrapped in try/catch/finally with toast feedback.
///   - Schedule / Unschedule now surface the result to the user.
/// </summary>
public partial class AutoPilotViewModel : ObservableObject
{
    private readonly IAutoPilotService _service;
    private readonly IToastNotificationService _toastService;
    private readonly ILogger<AutoPilotViewModel> _logger;
    private readonly DispatcherTimer _pollTimer;
    private int _consecutivePollFailures;

    [ObservableProperty]
    private AutoPilotTaskInfo? _taskInfo;

    [ObservableProperty]
    private string _countdownText = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public AutoPilotViewModel(
        IAutoPilotService service,
        IToastNotificationService toastService,
        ILogger<AutoPilotViewModel> logger)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _pollTimer.Tick += async (s, e) => await RefreshStatusAsync();
        _pollTimer.Start();
    }

    [RelayCommand]
    public async Task RefreshStatusAsync()
    {
        try
        {
            var result = await _service.GetStatusAsync(CancellationToken.None);
            if (result.IsSuccess)
            {
                TaskInfo = result.Value;
                UpdateCountdown();
                _consecutivePollFailures = 0;
            }
            else
            {
                _consecutivePollFailures++;
                _logger.LogWarning("AutoPilot status poll failed: {Error}", result.ErrorMessage);

                // If we've failed 3 times in a row, stop the timer so we don't
                // spam the log every 30 seconds when the scheduler is broken.
                if (_consecutivePollFailures >= 3)
                {
                    _pollTimer.Stop();
                    _logger.LogWarning("AutoPilot poll timer stopped after {Count} consecutive failures.", _consecutivePollFailures);
                }
            }
        }
        catch (Exception ex)
        {
            _consecutivePollFailures++;
            _logger.LogError(ex, "AutoPilot status poll threw an exception.");
            if (_consecutivePollFailures >= 3)
            {
                _pollTimer.Stop();
            }
        }
    }

    [RelayCommand]
    public async Task ScheduleAsync(CancellationToken ct = default)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var result = await _service.ScheduleAsync("Sunday", "02:00", new AutoPilotActions(), ct);
            if (result.IsSuccess)
            {
                _toastService.ShowSuccess("AutoPilot scheduled",
                    "Weekly care scheduled for Sundays at 02:00.");
            }
            else
            {
                _toastService.ShowError("Failed to schedule AutoPilot", result.ErrorMessage ?? "Unknown error.");
            }
            await RefreshStatusAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AutoPilot Schedule threw an exception.");
            _toastService.ShowError("Failed to schedule AutoPilot", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task UnscheduleAsync(CancellationToken ct = default)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var result = await _service.UnscheduleAsync(ct);
            if (result.IsSuccess)
            {
                _toastService.ShowSuccess("AutoPilot unscheduled",
                    "The weekly care schedule was removed.");
            }
            else
            {
                _toastService.ShowError("Failed to unschedule AutoPilot", result.ErrorMessage ?? "Unknown error.");
            }
            await RefreshStatusAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AutoPilot Unschedule threw an exception.");
            _toastService.ShowError("Failed to unschedule AutoPilot", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateCountdown()
    {
        if (TaskInfo?.NextRunTime.HasValue == true)
        {
            var diff = TaskInfo.NextRunTime.Value - DateTime.Now;
            if (diff.TotalSeconds > 0)
            {
                CountdownText = $"{diff.Days}d {diff.Hours}h {diff.Minutes}m {diff.Seconds}s";
            }
            else
            {
                CountdownText = "Running soon...";
            }
        }
        else
        {
            CountdownText = "Not scheduled";
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SysAdminX.AutoPilot.Models;
using SysAdminX.AutoPilot.Services;

namespace SysAdminX.AutoPilot.ViewModels;

public partial class AutoPilotViewModel : ObservableObject
{
    private readonly IAutoPilotService _service;
    private readonly DispatcherTimer _pollTimer;

    [ObservableProperty]
    private AutoPilotTaskInfo? _taskInfo;

    [ObservableProperty]
    private string _countdownText = string.Empty;

    public AutoPilotViewModel(IAutoPilotService service)
    {
        _service = service;
        _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _pollTimer.Tick += async (s, e) => await RefreshStatusAsync();
        _pollTimer.Start();
        
        _ = RefreshStatusAsync();
    }

    [RelayCommand]
    public async Task RefreshStatusAsync()
    {
        var result = await _service.GetStatusAsync(CancellationToken.None);
        if (result.IsSuccess)
        {
            TaskInfo = result.Value;
            UpdateCountdown();
        }
    }

    [RelayCommand]
    public async Task ScheduleAsync()
    {
        await _service.ScheduleAsync("Sunday", "02:00", new AutoPilotActions());
        await RefreshStatusAsync();
    }

    [RelayCommand]
    public async Task UnscheduleAsync()
    {
        await _service.UnscheduleAsync();
        await RefreshStatusAsync();
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

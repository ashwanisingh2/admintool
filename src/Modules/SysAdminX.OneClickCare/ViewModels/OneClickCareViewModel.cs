using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.OneClickCare.ViewModels;

public partial class StepViewModel : ObservableObject
{
    public string Name { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _isSelected = true;

    [ObservableProperty]
    private int _progress;

    [ObservableProperty]
    private string _statusIcon = "Circle24";

    [ObservableProperty]
    private string _estimatedTimeRemaining = string.Empty;

    [ObservableProperty]
    private string _output = string.Empty;
}

/// <summary>
/// ViewModel for the One-Click Care module.
/// </summary>
public partial class OneClickCareViewModel : ObservableObject
{
    private readonly IOneClickCareService _service;
    private readonly IToastNotificationService _toastService;
    private readonly ILogger<OneClickCareViewModel>? _logger;
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private bool _isComplete;

    public ObservableCollection<StepViewModel> Steps { get; } = new();

    public OneClickCareViewModel(
        IOneClickCareService service,
        IToastNotificationService toastService,
        ILogger<OneClickCareViewModel>? logger = null)
    {
        _service = service;
        _toastService = toastService;
        _logger = logger;
        _service.StepProgressChanged += OnStepProgressChanged;

        StartCareCommand = new AsyncRelayCommand(StartCareAsync, () => !IsRunning);
        CancelCareCommand = new RelayCommand(CancelCare, () => IsRunning);

        InitializeSteps();
    }

    public IAsyncRelayCommand StartCareCommand { get; }
    public IRelayCommand CancelCareCommand { get; }

    private void InitializeSteps()
    {
        Steps.Clear();
        Steps.Add(new StepViewModel { Name = "System Restore Point", Action = "Restore" });
        Steps.Add(new StepViewModel { Name = "Junk Cleanup Scan", Action = "JunkScan" });
        Steps.Add(new StepViewModel { Name = "Junk Cleanup Run", Action = "JunkClean" });
        Steps.Add(new StepViewModel { Name = "Network Optimization", Action = "Network" });
        Steps.Add(new StepViewModel { Name = "SFC Scan", Action = "Sfc" });
        Steps.Add(new StepViewModel { Name = "SSD TRIM", Action = "Trim" });
        Steps.Add(new StepViewModel { Name = "Security Audit", Action = "Security" });
    }

    private void OnStepProgressChanged(object? sender, StepProgressEventArgs e)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            if (e == null) return;

            if (e.StepName == "All steps")
            {
                IsRunning = false;
                IsComplete = true;
                StartCareCommand.NotifyCanExecuteChanged();
                CancelCareCommand.NotifyCanExecuteChanged();
                _toastService.ShowSuccess("One-Click Care complete",
                    "All care steps finished successfully.");
                return;
            }

            StepViewModel? step = null;
            foreach (var s in Steps)
            {
                if (s.Name == e.StepName)
                {
                    step = s;
                    break;
                }
            }

            if (step == null)
            {
                _logger?.LogWarning("StepProgressChanged for unknown step '{Step}'", e.StepName);
                return;
            }

            step.Progress = e.Progress;
            if (e.Status == "started" || e.Status == "running")
            {
                step.StatusIcon = "PlayCircle24";
                if (!string.IsNullOrEmpty(e.OutputLine)) step.Output = e.OutputLine;
            }
            else if (e.Status == "success")
            {
                step.StatusIcon = "CheckmarkCircle24";
                step.Progress = 100;
                if (!string.IsNullOrEmpty(e.OutputLine)) step.Output = e.OutputLine;
            }
            else if (e.Status == "failed")
            {
                step.StatusIcon = "ErrorCircle24";
                step.Output = e.ErrorMessage;
                IsRunning = false;
                StartCareCommand.NotifyCanExecuteChanged();
                CancelCareCommand.NotifyCanExecuteChanged();
                _toastService.ShowError($"Step failed: {step.Name}", e.ErrorMessage);
            }
            else if (e.Status == "cancelled")
            {
                step.StatusIcon = "StopCircle24";
                _toastService.ShowWarning("Care cancelled", $"Step '{step.Name}' was cancelled.");
            }

            if (e.StepName == "SFC Scan" && e.Status == "running" && e.Progress > 0)
            {
                step.EstimatedTimeRemaining = $"~ {100 - e.Progress} seconds left";
            }
        });
    }

    private async Task StartCareAsync()
    {
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        // Reset progress and status instead of re-initializing to keep user selection
        foreach (var step in Steps)
        {
            step.Progress = 0;
            step.StatusIcon = "Circle24";
            step.Output = string.Empty;
            step.EstimatedTimeRemaining = string.Empty;
        }

        IsRunning = true;
        IsComplete = false;
        StartCareCommand.NotifyCanExecuteChanged();
        CancelCareCommand.NotifyCanExecuteChanged();

        try
        {
            var careSteps = new System.Collections.Generic.List<CareStepModel>();
            foreach (var s in Steps)
            {
                if (s.IsSelected)
                {
                    careSteps.Add(new CareStepModel { Name = s.Name, Action = s.Action });
                }
            }

            if (careSteps.Count == 0)
            {
                _toastService.ShowWarning("No steps selected", "Please select at least one step to run.");
                return;
            }

            await Task.Run(() => _service.RunCareSequenceAsync(careSteps, _cts.Token), _cts.Token);
        }
        catch (OperationCanceledException)
        {
            _logger?.LogInformation("One-Click Care was cancelled by the user.");
            _toastService.ShowWarning("Care cancelled", "The care sequence was cancelled.");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "One-Click Care threw an unexpected exception.");
            _toastService.ShowError("Care failed", ex.Message);
        }
        finally
        {
            IsRunning = false;
            StartCareCommand.NotifyCanExecuteChanged();
            CancelCareCommand.NotifyCanExecuteChanged();
        }
    }

    private void CancelCare()
    {
        try
        {
            _cts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }
}

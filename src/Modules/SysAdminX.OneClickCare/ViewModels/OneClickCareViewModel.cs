using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.OneClickCare.ViewModels;

public partial class StepViewModel : ObservableObject
{
    public string Name { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;

    [ObservableProperty]
    private int _progress;

    [ObservableProperty]
    private string _statusIcon = "Circle24";

    [ObservableProperty]
    private string _estimatedTimeRemaining = string.Empty;
    
    [ObservableProperty]
    private string _output = string.Empty;
}

public partial class OneClickCareViewModel : ObservableObject
{
    private readonly IOneClickCareService _service;
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private bool _isComplete;

    public ObservableCollection<StepViewModel> Steps { get; } = new();

    public OneClickCareViewModel(IOneClickCareService service)
    {
        _service = service;
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
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (e.StepName == "All steps")
            {
                IsRunning = false;
                IsComplete = true;
                StartCareCommand.NotifyCanExecuteChanged();
                CancelCareCommand.NotifyCanExecuteChanged();
                MessageBox.Show("One-Click Care completed successfully!", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var step = default(StepViewModel);
            foreach (var s in Steps)
            {
                if (s.Name == e.StepName)
                {
                    step = s;
                    break;
                }
            }

            if (step != null)
            {
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
                }
                else if (e.Status == "cancelled")
                {
                    step.StatusIcon = "StopCircle24";
                }
                
                if (e.StepName == "SFC Scan" && e.Status == "running" && e.Progress > 0)
                {
                    step.EstimatedTimeRemaining = $"~ {100 - e.Progress} seconds left";
                }
            }
        });
    }

    private async Task StartCareAsync()
    {
        InitializeSteps();
        IsRunning = true;
        IsComplete = false;
        StartCareCommand.NotifyCanExecuteChanged();
        CancelCareCommand.NotifyCanExecuteChanged();

        _cts = new CancellationTokenSource();

        var careSteps = new System.Collections.Generic.List<CareStepModel>();
        foreach (var s in Steps)
        {
            careSteps.Add(new CareStepModel { Name = s.Name, Action = s.Action });
        }

        await Task.Run(() => _service.RunCareSequenceAsync(careSteps, _cts.Token));
    }

    private void CancelCare()
    {
        _cts?.Cancel();
    }
}

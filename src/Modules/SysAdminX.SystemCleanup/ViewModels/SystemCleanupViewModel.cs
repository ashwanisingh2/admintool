using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.SystemCleanup.ViewModels;

public partial class SystemCleanupViewModel : ObservableObject
{
    private readonly ILogger<SystemCleanupViewModel> _logger;
    private readonly ISystemCleanupService _cleanupService;
    private readonly IPowerShellService _powerShellService;
    private readonly DispatcherTimer _undoTimer;
    private CleanupResultModel? _lastCleanupResult;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public ObservableCollection<CleanupItemModel> CleanupItems { get; } = new();

    // --- Component Store Properties ---
    [ObservableProperty]
    private string _componentStoreSize = "Unknown";

    [ObservableProperty]
    private string _reclaimableSpace = "Unknown";

    [ObservableProperty]
    private bool _isAnalyzeComplete;

    [ObservableProperty]
    private double _dismProgress;

    [ObservableProperty]
    private bool _isDismRunning;

    // --- Undo Properties ---
    [ObservableProperty]
    private bool _isUndoVisible;

    [ObservableProperty]
    private int _undoCountdown;

    public SystemCleanupViewModel(
        ILogger<SystemCleanupViewModel> logger,
        ISystemCleanupService cleanupService,
        IPowerShellService powerShellService)
    {
        _logger = logger;
        _cleanupService = cleanupService;
        _powerShellService = powerShellService;

        _undoTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _undoTimer.Tick += UndoTimer_Tick;
    }

    [RelayCommand]
    private async Task LoadItemsAsync(CancellationToken ct)
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        CleanupItems.Clear();

        try
        {
            var result = await _cleanupService.GetCleanupItemsAsync(ct);
            if (result.IsSuccess && result.Value != null)
            {
                foreach (var item in result.Value)
                {
                    CleanupItems.Add(item);
                }
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Failed to load cleanup items.";
            }
        }
        catch (OperationCanceledException)
        {
            // ignore — user navigated away
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load cleanup items.");
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task PerformCleanupAsync(CancellationToken ct)
    {
        var selectedItems = CleanupItems.Where(i => i.IsSelected).ToList();
        if (!selectedItems.Any())
        {
            ErrorMessage = "Select at least one category to clean.";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var result = await _cleanupService.CleanAsync(selectedItems, ct);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.ErrorMessage ?? "Cleanup failed.";
            }
            else
            {
                _lastCleanupResult = result.Value;
                IsUndoVisible = true;
                UndoCountdown = 30;
                _undoTimer.Start();

                var freedBytes = result.Value?.TotalBytesFreed ?? 0;
                _logger.LogInformation("Cleanup freed {Bytes} bytes.", freedBytes);
            }

            await LoadItemsAsync(ct);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Cleanup cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cleanup threw an exception.");
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task UndoAsync(CancellationToken ct)
    {
        if (_lastCleanupResult == null) return;

        _undoTimer.Stop();
        IsUndoVisible = false;
        IsLoading = true;

        try
        {
            var result = await _cleanupService.UndoAsync(
                _lastCleanupResult.BackupDirectory,
                _lastCleanupResult.MovedFiles, ct);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.ErrorMessage ?? "Undo failed.";
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Undo cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Undo threw an exception.");
            ErrorMessage = ex.Message;
        }
        finally
        {
            _lastCleanupResult = null;
            await LoadItemsAsync(ct);
            IsLoading = false;
        }
    }

    private async void UndoTimer_Tick(object? sender, EventArgs e)
    {
        UndoCountdown--;
        if (UndoCountdown <= 0)
        {
            _undoTimer.Stop();
            IsUndoVisible = false;

            if (_lastCleanupResult != null)
            {
                try
                {
                    await _cleanupService.FinalizeAsync(_lastCleanupResult.BackupDirectory);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to finalize (auto-delete) cleanup backup directory.");
                }
                _lastCleanupResult = null;
            }
        }
    }

    private bool CanAnalyze() => !IsDismRunning;
    private bool CanCleanup() => IsAnalyzeComplete && !IsDismRunning;

    [RelayCommand(CanExecute = nameof(CanAnalyze))]
    private async Task AnalyzeComponentStoreAsync()
    {
        IsDismRunning = true;
        AnalyzeComponentStoreCommand.NotifyCanExecuteChanged();
        CleanupComponentStoreCommand.NotifyCanExecuteChanged();
        
        DismProgress = 0;
        ErrorMessage = string.Empty;

        var scriptContent = await _powerShellService.ExtractEmbeddedScriptAsync("component_cleanup.ps1");
        if (string.IsNullOrEmpty(scriptContent))
        {
            ErrorMessage = "Failed to load script.";
            IsDismRunning = false;
            AnalyzeComponentStoreCommand.NotifyCanExecuteChanged();
            CleanupComponentStoreCommand.NotifyCanExecuteChanged();
            return;
        }

        var parameters = new Dictionary<string, object> { { "Action", "Analyze" } };

        var outputStr = "";
        Action<string> onOutput = (line) =>
        {
            outputStr += line + "\n";
            var match = Regex.Match(line, @"\[=+([\d\.]+)%=+\]");
            if (match.Success && double.TryParse(match.Groups[1].Value, out var percent))
            {
                DismProgress = percent;
            }
        };

        var result = await _powerShellService.ExecuteStreamingAsync(scriptContent, parameters, onOutput);
        
        if (result.IsSuccess)
        {
            var sizeMatch = Regex.Match(outputStr, @"(?i)(Component Store Size|Actual Size of Component Store)\s*:\s*([\d\.]+)\s*(GB|MB)");
            if (sizeMatch.Success) ComponentStoreSize = sizeMatch.Groups[2].Value + " " + sizeMatch.Groups[3].Value;

            var recMatch = Regex.Match(outputStr, @"(?i)Reclaimable\s*Space\s*:\s*([\d\.]+)\s*(GB|MB)");
            if (recMatch.Success) ReclaimableSpace = recMatch.Groups[1].Value + " " + recMatch.Groups[2].Value;

            IsAnalyzeComplete = true;
        }
        else
        {
            ErrorMessage = result.ErrorMessage ?? "Analyze failed.";
        }

        IsDismRunning = false;
        AnalyzeComponentStoreCommand.NotifyCanExecuteChanged();
        CleanupComponentStoreCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanCleanup))]
    private async Task CleanupComponentStoreAsync()
    {
        IsDismRunning = true;
        AnalyzeComponentStoreCommand.NotifyCanExecuteChanged();
        CleanupComponentStoreCommand.NotifyCanExecuteChanged();
        
        DismProgress = 0;
        ErrorMessage = string.Empty;

        var scriptContent = await _powerShellService.ExtractEmbeddedScriptAsync("component_cleanup.ps1");
        if (string.IsNullOrEmpty(scriptContent))
        {
            ErrorMessage = "Failed to load script.";
            IsDismRunning = false;
            AnalyzeComponentStoreCommand.NotifyCanExecuteChanged();
            CleanupComponentStoreCommand.NotifyCanExecuteChanged();
            return;
        }

        var parameters = new Dictionary<string, object> { { "Action", "Cleanup" } };

        Action<string> onOutput = (line) =>
        {
            var match = Regex.Match(line, @"\[=+([\d\.]+)%=+\]");
            if (match.Success && double.TryParse(match.Groups[1].Value, out var percent))
            {
                DismProgress = percent;
            }
        };

        var result = await _powerShellService.ExecuteStreamingAsync(scriptContent, parameters, onOutput);
        
        if (!result.IsSuccess)
        {
            ErrorMessage = result.ErrorMessage ?? "Cleanup failed.";
        }
        else
        {
            await AnalyzeComponentStoreAsync();
        }

        IsDismRunning = false;
        AnalyzeComponentStoreCommand.NotifyCanExecuteChanged();
        CleanupComponentStoreCommand.NotifyCanExecuteChanged();
    }
}

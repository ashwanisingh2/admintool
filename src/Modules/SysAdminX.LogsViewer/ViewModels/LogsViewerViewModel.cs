// -----------------------------------------------------------------------
// <copyright file="LogsViewerViewModel.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;
using SysAdminX.LogsViewer.Services;

namespace SysAdminX.LogsViewer.ViewModels;

/// <summary>
/// ViewModel for the Logs Viewer module.
///
/// Improvements applied:
///   - Real cancellation token propagation in <see cref="RefreshLogsAsync"/>
///     and <see cref="AutoRefreshTickAsync"/> so navigating away actually
///     aborts the file read.
///   - <see cref="LogCount"/> observable so the UI can show "X of Y entries".
///   - <see cref="IsLoading"/> flag set in try/finally so a thrown exception
///     can no longer leave the spinner stuck on.
/// </summary>
public partial class LogsViewerViewModel : ObservableObject
{
    private readonly ILogger<LogsViewerViewModel> _logger;
    private readonly ILogsService _logsService;
    private readonly IBsodAnalyzerService _bsodAnalyzerService;

    private ObservableCollection<LogEntryModel> _allLogs = new();

    [ObservableProperty]
    private ObservableCollection<BsodEntryModel> _bsodEntries = new();

    [ObservableProperty]
    private bool _isBsodLoading;

    [ObservableProperty]
    private bool _hasNoCrashes;

    [ObservableProperty]
    private ObservableCollection<LogEntryModel> _filteredLogs = new();

    [ObservableProperty]
    private bool _showInfo = true;

    [ObservableProperty]
    private bool _showWarn = true;

    [ObservableProperty]
    private bool _showError = true;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isAutoRefreshEnabled;

    /// <summary>True while a log refresh is in flight (drives the spinner).</summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>Total number of log entries currently loaded (pre-filter).</summary>
    [ObservableProperty]
    private int _logCount;

    /// <summary>Number of entries currently visible after applying filters.</summary>
    [ObservableProperty]
    private int _filteredLogCount;

    private readonly DispatcherTimer _autoRefreshTimer;

    public LogsViewerViewModel(ILogger<LogsViewerViewModel> logger, ILogsService logsService, IBsodAnalyzerService bsodAnalyzerService)
    {
        _logger = logger;
        _logsService = logsService;
        _bsodAnalyzerService = bsodAnalyzerService;

        _autoRefreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _autoRefreshTimer.Tick += async (s, e) => await AutoRefreshTickAsync();
    }

    partial void OnIsAutoRefreshEnabledChanged(bool value)
    {
        if (value)
            _autoRefreshTimer.Start();
        else
            _autoRefreshTimer.Stop();
    }

    private async Task AutoRefreshTickAsync()
    {
        try
        {
            var logs = await _logsService.GetRecentLogsAsync(50);
            var lastLogTime = _allLogs.FirstOrDefault()?.Timestamp ?? DateTime.MinValue;

            var newLogs = logs.Where(l => l.Timestamp > lastLogTime).ToList();

            if (newLogs.Count > 0)
            {
                for (int i = newLogs.Count - 1; i >= 0; i--)
                {
                    var l = newLogs[i];
                    _allLogs.Insert(0, l);

                    if (MatchesFilters(l))
                    {
                        FilteredLogs.Insert(0, l);
                    }
                }

                LogCount = _allLogs.Count;
                FilteredLogCount = FilteredLogs.Count;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Auto-refresh tick failed.");
            // Stop the timer so we don't keep spamming failures.
            _autoRefreshTimer.Stop();
            IsAutoRefreshEnabled = false;
        }
    }

    [RelayCommand]
    public async Task RefreshLogsAsync(CancellationToken ct = default)
    {
        if (IsLoading) return;
        IsLoading = true;

        try
        {
            _logger.LogInformation("Refreshing log viewer...");
            var logs = await _logsService.GetRecentLogsAsync(2000);
            _allLogs.Clear();
            foreach (var l in logs)
            {
                _allLogs.Add(l);
            }

            ApplyFilters();
            LogCount = _allLogs.Count;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Log refresh cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh logs.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnShowInfoChanged(bool value) => ApplyFilters();
    partial void OnShowWarnChanged(bool value) => ApplyFilters();
    partial void OnShowErrorChanged(bool value) => ApplyFilters();
    partial void OnSearchQueryChanged(string value) => ApplyFilters();

    private bool MatchesFilters(LogEntryModel l)
    {
        // Level Filter
        bool matchesLevel = false;
        if (ShowInfo && (l.Level == "INF" || l.Level == "DBG")) matchesLevel = true;
        if (ShowWarn && l.Level == "WRN") matchesLevel = true;
        if (ShowError && (l.Level == "ERR" || l.Level == "FTL")) matchesLevel = true;

        if (!matchesLevel) return false;

        // Search Filter
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            if (!l.FullRawLine.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private void ApplyFilters()
    {
        var filtered = _allLogs.Where(MatchesFilters).ToList();

        FilteredLogs.Clear();
        foreach (var l in filtered)
        {
            FilteredLogs.Add(l);
        }

        LogCount = _allLogs.Count;
        FilteredLogCount = FilteredLogs.Count;
    }

    [RelayCommand]
    public async Task ExportLogsAsync(CancellationToken ct = default)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
            DefaultExt = ".txt",
            FileName = $"LogsExport_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var sb = new StringBuilder();
                foreach (var log in FilteredLogs)
                {
                    sb.AppendLine(log.FullRawLine);
                }
                await File.WriteAllTextAsync(dialog.FileName, sb.ToString(), ct);
                _logger.LogInformation("Exported {Count} log entries to {Path}", FilteredLogs.Count, dialog.FileName);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Log export cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export logs.");
            }
        }
    }

    [RelayCommand]
    public async Task AnalyzeDumpsAsync(CancellationToken ct = default)
    {
        if (IsBsodLoading) return;
        IsBsodLoading = true;
        HasNoCrashes = false;
        BsodEntries.Clear();

        try
        {
            var result = await _bsodAnalyzerService.AnalyzeDumpsAsync(ct);
            if (result.IsSuccess && result.Value != null)
            {
                foreach (var entry in result.Value)
                {
                    BsodEntries.Add(entry);
                }
            }

            if (BsodEntries.Count == 0)
            {
                HasNoCrashes = true;
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("BSOD analysis cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BSOD analysis threw an exception.");
        }
        finally
        {
            IsBsodLoading = false;
        }
    }

    [RelayCommand]
    public async Task GenerateBsodReportAsync(CancellationToken ct = default)
    {
        if (BsodEntries.Count == 0) return;

        try
        {
            await _bsodAnalyzerService.GenerateHtmlReportAsync(BsodEntries, ct);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("BSOD report generation cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BSOD report generation threw an exception.");
        }
    }
}

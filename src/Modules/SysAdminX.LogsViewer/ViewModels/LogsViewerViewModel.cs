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
        }
    }

    [RelayCommand]
    public async Task RefreshLogsAsync()
    {
        _logger.LogInformation("Refreshing log viewer...");
        
        var logs = await _logsService.GetRecentLogsAsync(2000);
        _allLogs.Clear();
        foreach (var l in logs)
        {
            _allLogs.Add(l);
        }
        
        ApplyFilters();
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
    }

    [RelayCommand]
    public async Task ExportLogsAsync()
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
                await File.WriteAllTextAsync(dialog.FileName, sb.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export logs.");
            }
        }
    }

    [RelayCommand]
    public async Task AnalyzeDumpsAsync(CancellationToken ct)
    {
        IsBsodLoading = true;
        HasNoCrashes = false;
        BsodEntries.Clear();

        try
        {
            var result = await _bsodAnalyzerService.AnalyzeDumpsAsync(ct);
            if (result.IsSuccess && result.Data != null)
            {
                foreach (var entry in result.Data)
                {
                    BsodEntries.Add(entry);
                }
            }

            if (BsodEntries.Count == 0)
            {
                HasNoCrashes = true;
            }
        }
        finally
        {
            IsBsodLoading = false;
        }
    }

    [RelayCommand]
    public async Task GenerateBsodReportAsync(CancellationToken ct)
    {
        if (BsodEntries.Count > 0)
        {
            await _bsodAnalyzerService.GenerateHtmlReportAsync(BsodEntries, ct);
        }
    }
}

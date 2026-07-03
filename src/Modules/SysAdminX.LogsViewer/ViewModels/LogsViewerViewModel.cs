// -----------------------------------------------------------------------
// <copyright file="LogsViewerViewModel.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
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

    private ObservableCollection<LogEntryModel> _allLogs = new();

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

    public LogsViewerViewModel(ILogger<LogsViewerViewModel> logger, ILogsService logsService)
    {
        _logger = logger;
        _logsService = logsService;
        
        RefreshLogsCommand.Execute(null);
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

    private void ApplyFilters()
    {
        var filtered = _allLogs.Where(l =>
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
        }).ToList();

        FilteredLogs.Clear();
        foreach (var l in filtered)
        {
            FilteredLogs.Add(l);
        }
    }
}

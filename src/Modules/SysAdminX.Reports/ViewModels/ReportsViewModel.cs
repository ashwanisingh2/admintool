// -----------------------------------------------------------------------
// <copyright file="ReportsViewModel.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Models;
using SysAdminX.Reports.Services;

namespace SysAdminX.Reports.ViewModels;

/// <summary>
/// ViewModel for the Reports module.
/// </summary>
public partial class ReportsViewModel : ObservableObject
{
    private readonly ILogger<ReportsViewModel> _logger;
    private readonly IReportService _reportService;

    [ObservableProperty]
    private ObservableCollection<ReportModel> _reports = new();

    [ObservableProperty]
    private bool _isGenerating;

    public ReportsViewModel(ILogger<ReportsViewModel> logger, IReportService reportService)
    {
        _logger = logger;
        _reportService = reportService;

    }

    [RelayCommand]
    public async Task LoadHistoryAsync()
    {
        var history = await _reportService.GetReportHistoryAsync();
        Reports.Clear();
        foreach (var r in history)
        {
            Reports.Add(r);
        }
    }

    [RelayCommand]
    public async Task GeneratePdfAsync(CancellationToken ct)
    {
        string filename = $"SystemReport_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        var result = await _reportService.GeneratePdfReportAsync(filename, ct);
        
        if (result.IsSuccess && result.Value != null)
        {
            Reports.Insert(0, result.Value);
        }
    }

    [RelayCommand]
    public async Task GenerateJsonAsync(CancellationToken ct)
    {
        string filename = $"SystemState_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        var result = await _reportService.GenerateJsonReportAsync(filename, ct);
        
        if (result.IsSuccess && result.Value != null)
        {
            Reports.Insert(0, result.Value);
        }
    }

    [RelayCommand]
    public async Task GenerateHtmlAuditAsync(CancellationToken ct)
    {
        IsGenerating = true;
        try
        {
            string filename = $"AuditReport_{DateTime.Now:yyyyMMdd_HHmmss}.html";
            var result = await _reportService.GenerateHtmlAuditReportAsync(filename, ct);
            
            if (result.IsSuccess && result.Value != null)
            {
                Reports.Insert(0, result.Value);
                OpenReport(result.Value);
            }
        }
        finally
        {
            IsGenerating = false;
        }
    }

    [RelayCommand]
    public void OpenReport(ReportModel report)
    {
        if (report != null && !string.IsNullOrEmpty(report.FilePath))
        {
            try
            {
                var p = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = report.FilePath,
                        UseShellExecute = true
                    }
                };
                p.Start();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open report");
            }
        }
    }
}

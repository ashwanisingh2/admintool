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
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;
using SysAdminX.Reports.Services;

namespace SysAdminX.Reports.ViewModels;

/// <summary>
/// ViewModel for the Reports module.
///
/// Improvements applied:
///   - All async command bodies wrapped in try/finally so an exception can
///     no longer leave <see cref="IsGenerating"/> stuck on.
///   - PDF / JSON / HTML generation now run with the command's cancellation
///     token so navigating away actually aborts the work.
///   - Toast notifications on success / failure so the user gets feedback
///     even if they switched tabs.
/// </summary>
public partial class ReportsViewModel : ObservableObject
{
    private readonly ILogger<ReportsViewModel> _logger;
    private readonly IReportService _reportService;
    private readonly IToastNotificationService _toastService;

    [ObservableProperty]
    private ObservableCollection<ReportModel> _reports = new();

    [ObservableProperty]
    private bool _isGenerating;

    public ReportsViewModel(
        ILogger<ReportsViewModel> logger,
        IReportService reportService,
        IToastNotificationService toastService)
    {
        _logger = logger;
        _reportService = reportService;
        _toastService = toastService;
    }

    [RelayCommand]
    public async Task LoadHistoryAsync(CancellationToken ct = default)
    {
        try
        {
            var history = await _reportService.GetReportHistoryAsync();
            Reports.Clear();
            foreach (var r in history)
            {
                Reports.Add(r);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load report history.");
            _toastService.ShowError("Failed to load report history", ex.Message);
        }
    }

    [RelayCommand]
    public async Task GeneratePdfAsync(CancellationToken ct = default)
    {
        IsGenerating = true;
        try
        {
            string filename = $"SystemReport_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            var result = await _reportService.GeneratePdfReportAsync(filename, ct);

            if (result.IsSuccess && result.Value != null)
            {
                Reports.Insert(0, result.Value);
                _toastService.ShowSuccess("PDF report generated", filename);
            }
            else
            {
                _toastService.ShowError("PDF report failed", result.ErrorMessage ?? "Unknown error.");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("PDF report generation cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate PDF report.");
            _toastService.ShowError("PDF report failed", ex.Message);
        }
        finally
        {
            IsGenerating = false;
        }
    }

    [RelayCommand]
    public async Task GenerateJsonAsync(CancellationToken ct = default)
    {
        IsGenerating = true;
        try
        {
            string filename = $"SystemState_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            var result = await _reportService.GenerateJsonReportAsync(filename, ct);

            if (result.IsSuccess && result.Value != null)
            {
                Reports.Insert(0, result.Value);
                _toastService.ShowSuccess("JSON report generated", filename);
            }
            else
            {
                _toastService.ShowError("JSON report failed", result.ErrorMessage ?? "Unknown error.");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("JSON report generation cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate JSON report.");
            _toastService.ShowError("JSON report failed", ex.Message);
        }
        finally
        {
            IsGenerating = false;
        }
    }

    [RelayCommand]
    public async Task GenerateHtmlAuditAsync(CancellationToken ct = default)
    {
        IsGenerating = true;
        try
        {
            string filename = $"AuditReport_{DateTime.Now:yyyyMMdd_HHmmss}.html";
            var result = await _reportService.GenerateHtmlAuditReportAsync(filename, ct);

            if (result.IsSuccess && result.Value != null)
            {
                Reports.Insert(0, result.Value);
                _toastService.ShowSuccess("HTML audit report generated", filename);
                OpenReport(result.Value);
            }
            else
            {
                _toastService.ShowError("HTML audit report failed", result.ErrorMessage ?? "Unknown error.");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("HTML audit report generation cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate HTML audit report.");
            _toastService.ShowError("HTML audit report failed", ex.Message);
        }
        finally
        {
            IsGenerating = false;
        }
    }

    [RelayCommand]
    public void OpenReport(ReportModel? report)
    {
        if (report == null || string.IsNullOrEmpty(report.FilePath))
        {
            _toastService.ShowWarning("Cannot open report", "Report file path is empty.");
            return;
        }

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
            _logger.LogError(ex, "Failed to open report {Path}", report.FilePath);
            _toastService.ShowError("Cannot open report", ex.Message);
        }
    }
}

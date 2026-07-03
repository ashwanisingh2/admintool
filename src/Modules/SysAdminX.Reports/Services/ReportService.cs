// -----------------------------------------------------------------------
// <copyright file="ReportService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SysAdminX.Core.Models;

namespace SysAdminX.Reports.Services;

/// <summary>
/// Implementation of <see cref="IReportService"/> for generating system state reports.
/// </summary>
public class ReportService : IReportService
{
    private readonly ILogger<ReportService> _logger;
    private readonly string _reportsDir;

    public ReportService(ILogger<ReportService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        QuestPDF.Settings.License = LicenseType.Community;

        // Ensure reports directory exists
        _reportsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SysAdminX_Reports");
        if (!Directory.Exists(_reportsDir))
        {
            Directory.CreateDirectory(_reportsDir);
        }
    }

    public Task<List<ReportModel>> GetReportHistoryAsync()
    {
        var reports = new List<ReportModel>();
        try
        {
            var files = Directory.GetFiles(_reportsDir);
            foreach (var file in files)
            {
                var info = new FileInfo(file);
                var type = info.Extension.ToLower() == ".json" ? ReportType.JSON : ReportType.PDF;
                
                reports.Add(new ReportModel
                {
                    Title = info.Name,
                    Description = $"{type} System State Export",
                    FilePath = file,
                    GeneratedAt = info.CreationTime,
                    Type = type,
                    FileSizeBytes = info.Length
                });
            }
            
            reports.Sort((a, b) => b.GeneratedAt.CompareTo(a.GeneratedAt)); // Newest first
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load report history");
        }

        return Task.FromResult(reports);
    }

    public async Task<Result<ReportModel>> GeneratePdfReportAsync(string filename, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating PDF Report...");
            string path = Path.Combine(_reportsDir, filename);

            await Task.Run(() =>
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(11));
                        
                        page.Header().Text("SysAdminX System Report").SemiBold().FontSize(24).FontColor(Colors.Blue.Darken2);
                        
                        page.Content().PaddingVertical(1, Unit.Centimetre).Column(x =>
                        {
                            x.Spacing(10);
                            
                            x.Item().Text("Device Overview").FontSize(16).SemiBold();
                            x.Item().Text($"Machine Name: {Environment.MachineName}");
                            x.Item().Text($"OS Version: {Environment.OSVersion.VersionString}");
                            x.Item().Text($"64-bit OS: {Environment.Is64BitOperatingSystem}");
                            x.Item().Text($"Processors: {Environment.ProcessorCount}");
                            x.Item().Text($"System Directory: {Environment.SystemDirectory}");
                            
                            x.Item().PaddingTop(20).Text("Report generated successfully using SysAdminX Toolkit.").Italic();
                        });
                        
                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                    });
                })
                .GeneratePdf(path);
            }, ct);

            var info = new FileInfo(path);
            var report = new ReportModel
            {
                Title = filename,
                Description = "System State Export (PDF)",
                FilePath = path,
                Type = ReportType.PDF,
                FileSizeBytes = info.Length
            };

            return Result<ReportModel>.Success(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PDF Generation failed");
            return Result<ReportModel>.Failure(ex.Message, ex);
        }
    }

    public async Task<Result<ReportModel>> GenerateJsonReportAsync(string filename, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating JSON Report...");
            string path = Path.Combine(_reportsDir, filename);

            var systemData = new
            {
                MachineName = Environment.MachineName,
                OSVersion = Environment.OSVersion.VersionString,
                Is64BitOS = Environment.Is64BitOperatingSystem,
                ProcessorCount = Environment.ProcessorCount,
                SystemDirectory = Environment.SystemDirectory,
                LogicalDrives = Environment.GetLogicalDrives(),
                UserDomainName = Environment.UserDomainName,
                UserName = Environment.UserName,
                GeneratedAt = DateTime.Now
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(systemData, options);

            await File.WriteAllTextAsync(path, json, ct);

            var info = new FileInfo(path);
            var report = new ReportModel
            {
                Title = filename,
                Description = "System State Export (JSON)",
                FilePath = path,
                Type = ReportType.JSON,
                FileSizeBytes = info.Length
            };

            return Result<ReportModel>.Success(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JSON Generation failed");
            return Result<ReportModel>.Failure(ex.Message, ex);
        }
    }
}

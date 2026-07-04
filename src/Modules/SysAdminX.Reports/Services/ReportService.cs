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
using System.Linq;
using System.Management;
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
                ReportType type = ReportType.PDF;
                string ext = info.Extension.ToLower();
                if (ext == ".json") type = ReportType.JSON;
                else if (ext == ".html" || ext == ".htm") type = ReportType.HTML;
                
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

    public async Task<Result<ReportModel>> GenerateHtmlAuditReportAsync(string filename, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating HTML Audit Report...");
            string path = Path.Combine(_reportsDir, filename);

            string computerName = Environment.MachineName;
            string osVersion = Environment.OSVersion.VersionString;
            
            string cpu = "Unknown";
            string ram = "Unknown";
            string antivirus = "Unknown";
            
            try
            {
#pragma warning disable CA1416 // Validate platform compatibility
                using var cpuSearcher = new ManagementObjectSearcher("select Name from Win32_Processor");
                foreach (var obj in cpuSearcher.Get())
                {
                    cpu = obj["Name"]?.ToString() ?? "Unknown";
                    break;
                }

                using var ramSearcher = new ManagementObjectSearcher("select TotalPhysicalMemory from Win32_ComputerSystem");
                foreach (var obj in ramSearcher.Get())
                {
                    if (ulong.TryParse(obj["TotalPhysicalMemory"]?.ToString(), out ulong bytes))
                    {
                        ram = $"{bytes / (1024 * 1024 * 1024.0):F2} GB";
                    }
                    break;
                }
                
                using var avSearcher = new ManagementObjectSearcher(@"root\SecurityCenter2", "SELECT * FROM AntiVirusProduct");
                var avList = new List<string>();
                foreach (var obj in avSearcher.Get())
                {
                    avList.Add(obj["displayName"]?.ToString() ?? "Unknown");
                }
                antivirus = avList.Any() ? string.Join(", ", avList) : "Not Found / Windows Defender";
#pragma warning restore CA1416
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve some WMI info.");
            }

            var ipAddress = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
                .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                .Where(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                .Select(a => a.Address.ToString())
                .FirstOrDefault() ?? "Unknown";

            var drives = DriveInfo.GetDrives().Where(d => d.IsReady).Select(d => 
                $"<li>{d.Name} - {d.TotalFreeSpace / (1024 * 1024 * 1024.0):F2} GB Free / {d.TotalSize / (1024 * 1024 * 1024.0):F2} GB Total</li>"
            );
            string drivesHtml = string.Join("\n", drives);

            string html = $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>System Audit Report - {computerName}</title>
    <style>
        :root {{
            --bg-color: #0f172a;
            --card-bg: #1e293b;
            --text-main: #f8fafc;
            --text-muted: #94a3b8;
            --accent: #3b82f6;
            --border: #334155;
        }}
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background-color: var(--bg-color);
            color: var(--text-main);
            margin: 0;
            padding: 40px 20px;
        }}
        .container {{
            max-width: 800px;
            margin: 0 auto;
        }}
        .header {{
            text-align: center;
            margin-bottom: 40px;
            animation: fadeIn 1s ease-out;
        }}
        .header h1 {{
            margin: 0;
            font-size: 2.5rem;
            color: var(--text-main);
            background: -webkit-linear-gradient(45deg, #60a5fa, #3b82f6);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
        }}
        .header p {{
            color: var(--text-muted);
            margin-top: 10px;
        }}
        .card {{
            background-color: var(--card-bg);
            border: 1px solid var(--border);
            border-radius: 12px;
            padding: 24px;
            margin-bottom: 24px;
            box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06);
            transition: transform 0.2s ease, box-shadow 0.2s ease;
        }}
        .card:hover {{
            transform: translateY(-2px);
            box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -2px rgba(0, 0, 0, 0.05);
        }}
        .card h2 {{
            margin-top: 0;
            color: var(--accent);
            font-size: 1.5rem;
            border-bottom: 1px solid var(--border);
            padding-bottom: 12px;
            margin-bottom: 20px;
        }}
        .grid-list {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
            gap: 20px;
        }}
        .data-item {{
            display: flex;
            flex-direction: column;
        }}
        .data-label {{
            font-size: 0.875rem;
            color: var(--text-muted);
            margin-bottom: 4px;
            text-transform: uppercase;
            letter-spacing: 0.05em;
        }}
        .data-value {{
            font-size: 1.125rem;
            font-weight: 500;
        }}
        ul.drives-list {{
            list-style-type: none;
            padding: 0;
            margin: 0;
        }}
        ul.drives-list li {{
            padding: 10px 0;
            border-bottom: 1px solid var(--border);
        }}
        ul.drives-list li:last-child {{
            border-bottom: none;
        }}
        @keyframes fadeIn {{
            from {{ opacity: 0; transform: translateY(-10px); }}
            to {{ opacity: 1; transform: translateY(0); }}
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>System Audit Report</h1>
            <p>Generated on {DateTime.Now:F}</p>
        </div>
        
        <div class=""card"">
            <h2>System Information</h2>
            <div class=""grid-list"">
                <div class=""data-item"">
                    <span class=""data-label"">Computer Name</span>
                    <span class=""data-value"">{computerName}</span>
                </div>
                <div class=""data-item"">
                    <span class=""data-label"">OS Version</span>
                    <span class=""data-value"">{osVersion}</span>
                </div>
                <div class=""data-item"">
                    <span class=""data-label"">IP Address</span>
                    <span class=""data-value"">{ipAddress}</span>
                </div>
                <div class=""data-item"">
                    <span class=""data-label"">Antivirus Status</span>
                    <span class=""data-value"">{antivirus}</span>
                </div>
            </div>
        </div>

        <div class=""card"">
            <h2>Hardware Specs</h2>
            <div class=""grid-list"">
                <div class=""data-item"">
                    <span class=""data-label"">CPU</span>
                    <span class=""data-value"">{cpu}</span>
                </div>
                <div class=""data-item"">
                    <span class=""data-label"">RAM</span>
                    <span class=""data-value"">{ram}</span>
                </div>
            </div>
        </div>

        <div class=""card"">
            <h2>Disk Space</h2>
            <ul class=""drives-list"">
                {drivesHtml}
            </ul>
        </div>
    </div>
</body>
</html>";

            await File.WriteAllTextAsync(path, html, ct);

            var info = new FileInfo(path);
            var report = new ReportModel
            {
                Title = filename,
                Description = "Full System Audit Report (HTML)",
                FilePath = path,
                Type = ReportType.HTML,
                FileSizeBytes = info.Length
            };

            return Result<ReportModel>.Success(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HTML Generation failed");
            return Result<ReportModel>.Failure(ex.Message, ex);
        }
    }
}

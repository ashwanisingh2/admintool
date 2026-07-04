using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.Infrastructure.Services;

public class BsodAnalyzerService : IBsodAnalyzerService
{
    private readonly ILogger<BsodAnalyzerService> _logger;
    private readonly IPowerShellService _powerShellService;

    // Hardcoded hashtable mapping ~50 common codes to human-readable causes
    private static readonly Dictionary<string, string> BugCheckMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "0x0000000A", "IRQL_NOT_LESS_OR_EQUAL" },
        { "0x0000001A", "MEMORY_MANAGEMENT" },
        { "0x0000003B", "SYSTEM_SERVICE_EXCEPTION" },
        { "0x00000050", "PAGE_FAULT_IN_NONPAGED_AREA" },
        { "0x0000007E", "SYSTEM_THREAD_EXCEPTION_NOT_HANDLED" },
        { "0x0000009F", "DRIVER_POWER_STATE_FAILURE" },
        { "0x000000D1", "DRIVER_IRQL_NOT_LESS_OR_EQUAL" },
        { "0x00000116", "VIDEO_TDR_ERROR" },
        { "0x00000133", "DPC_WATCHDOG_VIOLATION" },
        { "0x00000139", "KERNEL_SECURITY_CHECK_FAILURE" },
        { "0x000001E", "KMODE_EXCEPTION_NOT_HANDLED" },
        { "0x0000007A", "KERNEL_DATA_INPAGE_ERROR" },
        { "0x00000024", "NTFS_FILE_SYSTEM" },
        { "0x000000C2", "BAD_POOL_CALLER" },
        { "0x00000019", "BAD_POOL_HEADER" },
        { "0x000000FC", "ATTEMPTED_EXECUTE_OF_NOEXECUTE_MEMORY" },
        { "0x000000EF", "CRITICAL_PROCESS_DIED" },
        { "0x00000109", "CRITICAL_STRUCTURE_CORRUPTION" },
        { "0x00000119", "VIDEO_SCHEDULER_INTERNAL_ERROR" }
        // We only add common ones, others will stay empty or fallback
    };

    public BsodAnalyzerService(ILogger<BsodAnalyzerService> logger, IPowerShellService powerShellService)
    {
        _logger = logger;
        _powerShellService = powerShellService;
    }

    public async Task<Result<List<BsodEntryModel>>> AnalyzeDumpsAsync(CancellationToken ct)
    {
        try
        {
            // Read embedded script
            var scriptContent = await _powerShellService.ExtractEmbeddedScriptAsync("SysAdminX.Infrastructure.Scripts.analyze_bsod.ps1", ct);
            var result = await _powerShellService.ExecuteScriptContentAsync(scriptContent, null, ct);
            if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.Data))
            {
                // Fallback to empty list
                return Result<List<BsodEntryModel>>.Success(new List<BsodEntryModel>());
            }

            var json = result.Data;
            var entries = JsonSerializer.Deserialize<List<BsodEntryModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (entries == null)
            {
                entries = new List<BsodEntryModel>();
            }

            // Map bugcheck names
            foreach (var entry in entries)
            {
                if (!string.IsNullOrEmpty(entry.BugCheckCode))
                {
                    // Convert integer to hex if needed or just use string lookup
                    string codeKey = entry.BugCheckCode.Trim().ToUpperInvariant();
                    // if codeKey is just "10" convert to "0x0000000A", etc. Assuming script returns hex strings.
                    // Normalize length to 0x000000xx
                    if (!codeKey.StartsWith("0X") && int.TryParse(codeKey, out int decValue))
                    {
                        codeKey = $"0x{decValue:X8}";
                    }
                    if (codeKey.StartsWith("0X") && codeKey.Length < 10)
                    {
                        codeKey = "0x" + codeKey.Substring(2).PadLeft(8, '0');
                    }
                    
                    if (BugCheckMap.TryGetValue(codeKey, out var name))
                    {
                        entry.BugCheckName = name;
                    }
                }
            }

            return Result<List<BsodEntryModel>>.Success(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze BSOD dumps");
            return Result<List<BsodEntryModel>>.Failure("Error analyzing BSOD dumps: " + ex.Message);
        }
    }

    public async Task<Result<string>> GenerateHtmlReportAsync(IEnumerable<BsodEntryModel> entries, CancellationToken ct)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html><head><title>SysAdminX BSOD Report</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: Segoe UI, Tahoma, sans-serif; margin: 20px; background: #f5f5f5; }");
            sb.AppendLine("table { border-collapse: collapse; width: 100%; background: #fff; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }");
            sb.AppendLine("th, td { padding: 12px; text-align: left; border-bottom: 1px solid #ddd; }");
            sb.AppendLine("th { background: #0078D7; color: white; }");
            sb.AppendLine("</style></head><body>");
            
            sb.AppendLine("<h1>SysAdminX - Crash Dump Report</h1>");
            
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>Time</th><th>Dump File</th><th>BugCheck Code</th><th>BugCheck Name</th><th>Likely Cause</th></tr>");
            foreach (var entry in entries)
            {
                sb.AppendLine($"<tr><td>{entry.Timestamp:G}</td><td>{entry.DumpFile}</td><td>{entry.BugCheckCode}</td><td>{entry.BugCheckName}</td><td>{entry.LikelyCause}</td></tr>");
            }
            sb.AppendLine("</table>");
            sb.AppendLine("</body></html>");

            var reportPath = Path.Combine(Path.GetTempPath(), "sysadminx_bsod_report.html");
            await File.WriteAllTextAsync(reportPath, sb.ToString(), ct);
            
            // Open it
            Process.Start(new ProcessStartInfo
            {
                FileName = reportPath,
                UseShellExecute = true
            });

            return Result<string>.Success(reportPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate HTML report");
            return Result<string>.Failure("Error generating report: " + ex.Message);
        }
    }
}

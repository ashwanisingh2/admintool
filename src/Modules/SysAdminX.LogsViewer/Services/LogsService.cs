// -----------------------------------------------------------------------
// <copyright file="LogsService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Models;

namespace SysAdminX.LogsViewer.Services;

/// <summary>
/// Implementation of <see cref="ILogsService"/> for parsing Serilog files.
/// </summary>
public class LogsService : ILogsService
{
    private readonly ILogger<LogsService> _logger;
    private readonly string _logsDir;
    
    // Pattern matches: [2026-07-03 15:30:00.000 +05:30] [INF] [Source] Message
    private readonly Regex _logRegex = new Regex(@"^\[(.*?)\] \[(.*?)\] \[(.*?)\] (.*)", RegexOptions.Compiled);

    public LogsService(ILogger<LogsService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SysAdminX", "Logs");
    }

    public async Task<List<LogEntryModel>> GetRecentLogsAsync(int maxLines = 1000)
    {
        var entries = new List<LogEntryModel>();
        
        try
        {
            if (!Directory.Exists(_logsDir))
                return entries;

            var logFiles = Directory.GetFiles(_logsDir, "sysadminx-*.log")
                                    .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                                    .ToList();

            if (logFiles.Count == 0)
                return entries;

            var logFilesToRead = logFiles.Take(5).Reverse().ToList();
            var linesList = new List<string>();
            
            foreach (var logFile in logFilesToRead)
            {
                using var fs = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fs);
                linesList.AddRange((await reader.ReadToEndAsync()).Split(new[] { Environment.NewLine }, StringSplitOptions.None));
            }
            
            var allLines = linesList.ToArray();
            
            // Start from the end, read up to maxLines
            int startIdx = Math.Max(0, allLines.Length - maxLines);
            
            LogEntryModel? currentEntry = null;

            for (int i = startIdx; i < allLines.Length; i++)
            {
                var line = allLines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                var match = _logRegex.Match(line);
                if (match.Success)
                {
                    if (currentEntry != null)
                        entries.Add(currentEntry);

                    currentEntry = new LogEntryModel
                    {
                        Timestamp = DateTime.TryParse(match.Groups[1].Value, out var ts) ? ts : DateTime.Now,
                        Level = match.Groups[2].Value.Trim(),
                        Source = match.Groups[3].Value.Trim(),
                        Message = match.Groups[4].Value,
                        FullRawLine = line
                    };
                }
                else if (currentEntry != null)
                {
                    // This is probably an exception trace appended to the previous line
                    currentEntry.Exception += line + Environment.NewLine;
                    currentEntry.FullRawLine += Environment.NewLine + line;
                }
            }
            
            if (currentEntry != null)
                entries.Add(currentEntry);

            entries.Reverse(); // Newest first
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse log files");
        }

        return entries;
    }
}

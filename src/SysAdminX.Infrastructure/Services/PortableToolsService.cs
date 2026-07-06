// -----------------------------------------------------------------------
// <copyright file="PortableToolsService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

using System.Net.Http;

namespace SysAdminX.Infrastructure.Services;

public class PortableToolsService : IPortableToolsService
{
    private readonly ILogger<PortableToolsService> _logger;
    private static readonly List<PortableToolModel> _tools = new()
    {
        new() { Id = "procexp", Name = "Process Explorer", Description = "Advanced process management.", ExecutableName = "procexp.exe", Category = "Sysinternals", DownloadUrl = "https://live.sysinternals.com/procexp.exe" },
        new() { Id = "autoruns", Name = "Autoruns", Description = "Startup program viewer.", ExecutableName = "autoruns.exe", Category = "Sysinternals", DownloadUrl = "https://live.sysinternals.com/autoruns.exe" },
        new() { Id = "tcpview", Name = "TCPView", Description = "Active socket command line viewer.", ExecutableName = "tcpview.exe", Category = "Sysinternals", DownloadUrl = "https://live.sysinternals.com/tcpview.exe" },
        new() { Id = "psexec", Name = "PsExec", Description = "Execute processes remotely.", ExecutableName = "PsExec.exe", Category = "Sysinternals", DownloadUrl = "https://live.sysinternals.com/PsExec.exe" },
        new() { Id = "rammap", Name = "RAMMap", Description = "Physical memory usage analysis.", ExecutableName = "RAMMap.exe", Category = "Sysinternals", DownloadUrl = "https://live.sysinternals.com/RAMMap.exe" },
        new() { Id = "putty", Name = "PuTTY", Description = "SSH and telnet client.", ExecutableName = "putty.exe", Category = "Network", DownloadUrl = "https://the.earth.li/~sgtatham/putty/latest/w64/putty.exe" },
        new() { Id = "rufus", Name = "Rufus", Description = "Create bootable USB drives.", ExecutableName = "rufus.exe", Category = "Utilities", DownloadUrl = "https://github.com/pbatard/rufus/releases/download/v4.5/rufus-4.5p.exe" }
    };

    public PortableToolsService(ILogger<PortableToolsService> logger)
    {
        _logger = logger;
    }

    public async Task<Result<IEnumerable<PortableToolModel>>> GetAvailableToolsAsync(CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SysAdminX", "PortableTools");
                if (!Directory.Exists(appData)) Directory.CreateDirectory(appData);

                foreach (var tool in _tools)
                {
                    var filePath = Path.Combine(appData, tool.ExecutableName);
                    tool.IsDownloaded = File.Exists(filePath);
                }

                return Result<IEnumerable<PortableToolModel>>.Success(_tools);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get portable tools.");
                return Result<IEnumerable<PortableToolModel>>.Failure("Failed to get portable tools: " + ex.Message);
            }
        });
    }

    public async Task<Result<bool>> RunToolAsync(string toolId, CancellationToken ct = default)
    {
        try
        {
            var tool = _tools.Find(t => t.Id == toolId);
            if (tool == null) return Result<bool>.Failure("Tool not found.");

            var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SysAdminX", "PortableTools");
            if (!Directory.Exists(appData)) Directory.CreateDirectory(appData);

            var exePath = Path.Combine(appData, tool.ExecutableName);

            if (!File.Exists(exePath))
            {
                if (string.IsNullOrEmpty(tool.DownloadUrl))
                {
                    return Result<bool>.Failure("No download URL provided for this tool.");
                }

                using var client = new HttpClient();
                var response = await client.GetAsync(tool.DownloadUrl, ct);
                if (!response.IsSuccessStatusCode)
                {
                    return Result<bool>.Failure($"Failed to download tool. Status code: {response.StatusCode}");
                }

                var bytes = await response.Content.ReadAsByteArrayAsync(ct);
                await File.WriteAllBytesAsync(exePath, bytes, ct);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true
            };
            
            Process.Start(startInfo);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run portable tool.");
            return Result<bool>.Failure("Failed to run tool: " + ex.Message);
        }
    }
}

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
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.Infrastructure.Services;

public class PortableToolsService : IPortableToolsService
{
    private readonly ILogger<PortableToolsService> _logger;

    public PortableToolsService(ILogger<PortableToolsService> logger)
    {
        _logger = logger;
    }

    public async Task<Result<IEnumerable<PortableToolModel>>> GetAvailableToolsAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var tools = new List<PortableToolModel>
                {
                    new() { Id = "procexp", Name = "Process Explorer", Description = "Advanced process management.", ExecutableName = "procexp.exe", Category = "Sysinternals" },
                    new() { Id = "autoruns", Name = "Autoruns", Description = "Startup program viewer.", ExecutableName = "autoruns.exe", Category = "Sysinternals" },
                    new() { Id = "tcpview", Name = "TCPView", Description = "Active socket command line viewer.", ExecutableName = "tcpview.exe", Category = "Sysinternals" }
                };
                return Result<IEnumerable<PortableToolModel>>.Success(tools);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get portable tools.");
                return Result<IEnumerable<PortableToolModel>>.Failure("Failed to get portable tools: " + ex.Message);
            }
        });
    }

    public async Task<Result<bool>> RunToolAsync(string toolId)
    {
        return await Task.Run(() =>
        {
            try
            {
                // In a real app, this would download and extract to a temp/tools dir, then run.
                // For now, we simulate success or launch if found in path.
                return Result<bool>.Failure($"Tool {toolId} not downloaded locally yet.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run portable tool.");
                return Result<bool>.Failure("Failed to run tool: " + ex.Message);
            }
        });
    }
}

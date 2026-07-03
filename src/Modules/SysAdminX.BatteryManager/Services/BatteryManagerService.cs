// -----------------------------------------------------------------------
// <copyright file="BatteryManagerService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.BatteryManager.Services;

public class BatteryManagerService : IBatteryManagerService
{
    private readonly ILogger<BatteryManagerService> _logger;
    private readonly IWmiService _wmiService;
    private readonly IProcessExecutorService _processService;

    public BatteryManagerService(ILogger<BatteryManagerService> logger, IWmiService wmiService, IProcessExecutorService processService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _wmiService = wmiService ?? throw new ArgumentNullException(nameof(wmiService));
        _processService = processService ?? throw new ArgumentNullException(nameof(processService));
    }

    public async Task<Result<BatteryInfoModel>> GetBatteryInfoAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Querying Battery Information");
            var model = new BatteryInfoModel();

            // Win32_Battery for basic info
            var batteryResult = await _wmiService.QueryAsync("SELECT * FROM Win32_Battery", ct);
            if (batteryResult.IsSuccess && batteryResult.Value?.Count > 0)
            {
                var dict = batteryResult.Value[0];
                model.Name = dict.TryGetValue("Name", out var n) ? n?.ToString() ?? "Unknown" : "Unknown";
                model.Status = dict.TryGetValue("BatteryStatus", out var s) ? ParseStatus(s?.ToString()) : "Unknown";
            }
            else
            {
                // If there's no battery, WMI might return no results.
                return Result<BatteryInfoModel>.Failure("No battery found on this system.");
            }

            // You can query MSBatteryClass_FullChargeCapacity to get real capacities in mWh
            // Note: MSBatteryClass_* classes are in root\wmi namespace, not root\cimv2
            var fullCapResult = await _wmiService.QueryAsync(@"root\wmi", "SELECT * FROM MSBatteryClass_FullChargeCapacity", ct);
            if (fullCapResult.IsSuccess && fullCapResult.Value?.Count > 0)
            {
                var dict = fullCapResult.Value[0];
                if (dict.TryGetValue("FullChargeCapacity", out var val) && uint.TryParse(val?.ToString(), out uint cap))
                {
                    model.FullChargeCapacity = cap;
                }
            }

            var designedCapResult = await _wmiService.QueryAsync(@"root\wmi", "SELECT * FROM MSBatteryClass_StaticData", ct);
            if (designedCapResult.IsSuccess && designedCapResult.Value?.Count > 0)
            {
                var dict = designedCapResult.Value[0];
                if (dict.TryGetValue("DesignedCapacity", out var val) && uint.TryParse(val?.ToString(), out uint cap))
                {
                    model.DesignedCapacity = cap;
                }
                model.Manufacturer = dict.TryGetValue("OEMInformation", out var oem) ? oem?.ToString() ?? "Unknown" : "Unknown";
            }

            var currentCapResult = await _wmiService.QueryAsync(@"root\wmi", "SELECT * FROM MSBatteryClass_SystemBatteryState", ct);
            if (currentCapResult.IsSuccess && currentCapResult.Value?.Count > 0)
            {
                var dict = currentCapResult.Value[0];
                if (dict.TryGetValue("RemainingCapacity", out var val) && uint.TryParse(val?.ToString(), out uint cap))
                {
                    model.CurrentCapacity = cap;
                }
            }

            return Result<BatteryInfoModel>.Success(model);
        }
        catch (OperationCanceledException)
        {
            return Result<BatteryInfoModel>.Cancelled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get battery info");
            return Result<BatteryInfoModel>.Failure(ex.Message, ex);
        }
    }

    public async Task<Result<string>> GenerateBatteryReportAsync(string destinationFolder, CancellationToken ct = default)
    {
        try
        {
            if (!Directory.Exists(destinationFolder))
                Directory.CreateDirectory(destinationFolder);

            string reportPath = Path.Combine(destinationFolder, $"BatteryReport_{DateTime.Now:yyyyMMdd_HHmmss}.html");
            
            var result = await _processService.ExecuteAsync("powercfg", $"/batteryreport /output \"{reportPath}\"", false, ct);
            if (result.IsSuccess && File.Exists(reportPath))
            {
                return Result<string>.Success(reportPath);
            }
            
            return Result<string>.Failure(result.ErrorMessage ?? "Failed to generate report");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate battery report");
            return Result<string>.Failure(ex.Message, ex);
        }
    }

    private string ParseStatus(string? statusVal)
    {
        return statusVal switch
        {
            "1" => "Discharging",
            "2" => "AC Power",
            "3" => "Fully Charged",
            "4" => "Low",
            "5" => "Critical",
            "6" => "Charging",
            "7" => "Charging and High",
            "8" => "Charging and Low",
            "9" => "Charging and Critical",
            "10" => "Undefined",
            "11" => "Partially Charged",
            _ => "Unknown"
        };
    }
}

// -----------------------------------------------------------------------
// <copyright file="DeviceDetailsService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.DeviceDetails.Services;

/// <summary>
/// Implementation of <see cref="IDeviceDetailsService"/> using WMI and Registry.
/// </summary>
public class DeviceDetailsService : IDeviceDetailsService
{
    private readonly ILogger<DeviceDetailsService> _logger;
    private readonly IWmiService _wmiService;
    private readonly IRegistryService _registryService;

    public DeviceDetailsService(
        ILogger<DeviceDetailsService> logger,
        IWmiService wmiService,
        IRegistryService registryService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _wmiService = wmiService ?? throw new ArgumentNullException(nameof(wmiService));
        _registryService = registryService ?? throw new ArgumentNullException(nameof(registryService));
    }

    public async Task<Result<DeviceDetailsModel>> GetDeviceDetailsAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Starting {Operation}", nameof(GetDeviceDetailsAsync));

            var computer = await GetComputerInfoAsync(ct);
            var cpu = await GetCpuInfoAsync(ct);
            var ram = await GetRamInfoAsync(ct);
            var gpus = await GetGpuInfoAsync(ct);
            var mobo = await GetMotherboardInfoAsync(ct);
            var bios = await GetBiosInfoAsync(ct);
            var windows = await GetWindowsInfoAsync(ct);
            var drives = await GetStorageInfoAsync(ct);
            var activation = await GetActivationStatusAsync(ct);

            var result = new DeviceDetailsModel
            {
                Computer = computer,
                Cpu = cpu,
                Ram = ram,
                Gpus = gpus,
                Motherboard = mobo,
                Bios = bios,
                Windows = windows,
                Drives = drives,
                Activation = activation
            };

            return Result<DeviceDetailsModel>.Success(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Operation cancelled: {Op}", nameof(GetDeviceDetailsAsync));
            return Result<DeviceDetailsModel>.Cancelled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed: {Op}", nameof(GetDeviceDetailsAsync));
            return Result<DeviceDetailsModel>.Failure(ex.Message);
        }
    }

    private async Task<ComputerInfoModel> GetComputerInfoAsync(CancellationToken ct)
    {
        var model = new ComputerInfoModel { Name = Environment.MachineName };

        var wmiResult = await _wmiService.QueryAsync("SELECT Domain, Workgroup, Manufacturer, Model FROM Win32_ComputerSystem", ct);
        if (wmiResult.IsSuccess && wmiResult.Value != null && wmiResult.Value.Count > 0)
        {
            var obj = wmiResult.Value[0];
            model = model with
            {
                Domain = obj["Domain"]?.ToString() ?? "",
                Workgroup = obj["Workgroup"]?.ToString() ?? "",
                Manufacturer = obj["Manufacturer"]?.ToString() ?? "",
                Model = obj["Model"]?.ToString() ?? ""
            };
        }

        var biosResult = await _wmiService.QueryAsync("SELECT SerialNumber FROM Win32_BIOS", ct);
        if (biosResult.IsSuccess && biosResult.Value != null && biosResult.Value.Count > 0)
        {
            model = model with { SerialNumber = biosResult.Value[0]["SerialNumber"]?.ToString() ?? "" };
        }

        return model;
    }

    private async Task<CpuInfoModel> GetCpuInfoAsync(CancellationToken ct)
    {
        var model = new CpuInfoModel();
        var wmiResult = await _wmiService.QueryAsync("SELECT Name, NumberOfCores, NumberOfLogicalProcessors, Architecture, L2CacheSize, L3CacheSize FROM Win32_Processor", ct);
        
        if (wmiResult.IsSuccess && wmiResult.Value != null && wmiResult.Value.Count > 0)
        {
            var obj = wmiResult.Value[0];
            
            // Architecture mapping
            int archCode = 9;
            var archVal = obj["Architecture"];
            if (archVal != null && int.TryParse(archVal.ToString(), out archCode))
            {
                model = model with { Architecture = archCode == 9 ? "x64" : (archCode == 0 ? "x86" : (archCode == 12 ? "ARM64" : archCode.ToString())) };
            }

            model = model with
            {
                Name = obj["Name"]?.ToString() ?? "",
                Cores = int.TryParse(obj["NumberOfCores"]?.ToString(), out var c) ? c : 0,
                Threads = int.TryParse(obj["NumberOfLogicalProcessors"]?.ToString(), out var t) ? t : 0,
                L2Cache = obj["L2CacheSize"] != null ? $"{obj["L2CacheSize"]} KB" : "",
                L3Cache = obj["L3CacheSize"] != null ? $"{obj["L3CacheSize"]} KB" : "",
                Architecture = model.Architecture == "" ? "x64" : model.Architecture
            };
        }
        return model;
    }

    private async Task<RamInfoModel> GetRamInfoAsync(CancellationToken ct)
    {
        var model = new RamInfoModel();
        long totalCapacity = 0;
        
        var wmiResult = await _wmiService.QueryAsync("SELECT Capacity, Speed, MemoryType, FormFactor FROM Win32_PhysicalMemory", ct);
        
        if (wmiResult.IsSuccess && wmiResult.Value != null)
        {
            foreach (var obj in wmiResult.Value)
            {
                if (long.TryParse(obj["Capacity"]?.ToString(), out var cap))
                {
                    totalCapacity += cap;
                }
                
                if (model.ConfiguredClockSpeed == 0 && int.TryParse(obj["Speed"]?.ToString(), out var speed))
                {
                    model = model with { ConfiguredClockSpeed = speed };
                }
            }
            
            model = model with { TotalCapacity = $"{(totalCapacity / (1024.0 * 1024 * 1024)):F1} GB" };
        }
        return model;
    }

    private async Task<List<GpuInfoModel>> GetGpuInfoAsync(CancellationToken ct)
    {
        var list = new List<GpuInfoModel>();
        var wmiResult = await _wmiService.QueryAsync("SELECT Name, DriverVersion, AdapterRAM, VideoModeDescription FROM Win32_VideoController", ct);
        
        if (wmiResult.IsSuccess && wmiResult.Value != null)
        {
            foreach (var obj in wmiResult.Value)
            {
                long ram = 0;
                long.TryParse(obj["AdapterRAM"]?.ToString(), out ram);
                
                list.Add(new GpuInfoModel
                {
                    Name = obj["Name"]?.ToString() ?? "",
                    DriverVersion = obj["DriverVersion"]?.ToString() ?? "",
                    AdapterRAM = ram > 0 ? $"{(ram / (1024.0 * 1024 * 1024)):F1} GB" : "Unknown",
                    Resolution = obj["VideoModeDescription"]?.ToString() ?? ""
                });
            }
        }
        return list;
    }

    private async Task<MotherboardInfoModel> GetMotherboardInfoAsync(CancellationToken ct)
    {
        var model = new MotherboardInfoModel();
        var wmiResult = await _wmiService.QueryAsync("SELECT Manufacturer, Product, Version FROM Win32_BaseBoard", ct);
        
        if (wmiResult.IsSuccess && wmiResult.Value != null && wmiResult.Value.Count > 0)
        {
            var obj = wmiResult.Value[0];
            model = model with
            {
                Manufacturer = obj["Manufacturer"]?.ToString() ?? "",
                Product = obj["Product"]?.ToString() ?? "",
                Version = obj["Version"]?.ToString() ?? ""
            };
        }
        return model;
    }

    private async Task<BiosInfoModel> GetBiosInfoAsync(CancellationToken ct)
    {
        var model = new BiosInfoModel();
        var wmiResult = await _wmiService.QueryAsync("SELECT Manufacturer, SMBIOSBIOSVersion, ReleaseDate, SMBIOSMajorVersion, SMBIOSMinorVersion FROM Win32_BIOS", ct);
        
        if (wmiResult.IsSuccess && wmiResult.Value != null && wmiResult.Value.Count > 0)
        {
            var obj = wmiResult.Value[0];
            string dateStr = obj["ReleaseDate"]?.ToString() ?? "";
            if (dateStr.Length >= 8)
            {
                dateStr = $"{dateStr.Substring(0, 4)}-{dateStr.Substring(4, 2)}-{dateStr.Substring(6, 2)}";
            }
            
            model = model with
            {
                Vendor = obj["Manufacturer"]?.ToString() ?? "",
                Version = obj["SMBIOSBIOSVersion"]?.ToString() ?? "",
                ReleaseDate = dateStr,
                SmbiosVersion = $"{obj["SMBIOSMajorVersion"]}.{obj["SMBIOSMinorVersion"]}"
            };
        }
        return model;
    }

    private async Task<WindowsBuildModel> GetWindowsInfoAsync(CancellationToken ct)
    {
        var model = new WindowsBuildModel();
        
        var editionTask = _registryService.ReadStringValueAsync("HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName", ct);
        var displayVersionTask = _registryService.ReadStringValueAsync("HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "DisplayVersion", ct);
        var buildNumberTask = _registryService.ReadStringValueAsync("HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentBuildNumber", ct);
        
        await Task.WhenAll(editionTask, displayVersionTask, buildNumberTask);
        
        model = model with
        {
            Edition = editionTask.Result.IsSuccess ? editionTask.Result.Value! : "Windows",
            Version = displayVersionTask.Result.IsSuccess ? displayVersionTask.Result.Value! : "",
            BuildNumber = buildNumberTask.Result.IsSuccess ? buildNumberTask.Result.Value! : "",
            Architecture = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit"
        };
        
        var wmiResult = await _wmiService.QueryAsync("SELECT InstallDate FROM Win32_OperatingSystem", ct);
        if (wmiResult.IsSuccess && wmiResult.Value != null && wmiResult.Value.Count > 0)
        {
            var installDateStr = wmiResult.Value[0]["InstallDate"]?.ToString() ?? "";
            if (installDateStr.Length >= 8)
            {
                model = model with { InstallDate = $"{installDateStr.Substring(0, 4)}-{installDateStr.Substring(4, 2)}-{installDateStr.Substring(6, 2)}" };
            }
        }
        
        return model;
    }

    private async Task<List<StorageDriveModel>> GetStorageInfoAsync(CancellationToken ct)
    {
        var drives = new List<StorageDriveModel>();
        var wmiResult = await _wmiService.QueryAsync("SELECT DeviceID, VolumeName, FileSystem, Size, FreeSpace, DriveType FROM Win32_LogicalDisk WHERE DriveType=3", ct);
        if (wmiResult.IsSuccess && wmiResult.Value != null)
        {
            foreach (var obj in wmiResult.Value)
            {
                long size = long.TryParse(obj["Size"]?.ToString(), out var s) ? s : 0;
                long free = long.TryParse(obj["FreeSpace"]?.ToString(), out var f) ? f : 0;
                long used = size - free;
                double pct = size > 0 ? (used * 100.0 / size) : 0;
                drives.Add(new StorageDriveModel
                {
                    DriveLetter = obj["DeviceID"]?.ToString() ?? "",
                    Label = obj["VolumeName"]?.ToString() ?? "",
                    FileSystem = obj["FileSystem"]?.ToString() ?? "",
                    TotalSize = $"{size / (1024.0 * 1024 * 1024):F1} GB",
                    FreeSpace = $"{free / (1024.0 * 1024 * 1024):F1} GB",
                    UsedSpace = $"{used / (1024.0 * 1024 * 1024):F1} GB",
                    UsagePercent = pct,
                    DriveType = "Local Disk"
                });
            }
        }
        return drives;
    }

    private async Task<ActivationStatusModel> GetActivationStatusAsync(CancellationToken ct)
    {
        var wmiResult = await _wmiService.QueryAsync("SELECT LicenseStatus FROM SoftwareLicensingProduct WHERE LicenseStatus=1 AND PartialProductKey IS NOT NULL", ct);
        
        string status = "Not Activated";
        if (wmiResult.IsSuccess && wmiResult.Value != null && wmiResult.Value.Count > 0)
        {
            var licStatus = wmiResult.Value[0]["LicenseStatus"]?.ToString();
            status = licStatus == "1" ? "Activated" : "Not Activated";
        }
        
        // Get partial product key (masked)
        var keyResult = await _registryService.ReadStringValueAsync("HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductId", ct);
        string maskedKey = keyResult.IsSuccess && !string.IsNullOrEmpty(keyResult.Value) 
            ? $"XXXXX-XXXXX-XXXXX-{keyResult.Value.Substring(Math.Max(0, keyResult.Value.Length - 5))}" 
            : "N/A";
        
        return new ActivationStatusModel
        {
            LicenseStatus = status,
            ActivationType = status == "Activated" ? "Digital License" : "N/A",
            ProductKey = maskedKey
        };
    }
}

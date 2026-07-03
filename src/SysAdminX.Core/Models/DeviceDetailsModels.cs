// -----------------------------------------------------------------------
// <copyright file="DeviceDetailsModels.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace SysAdminX.Core.Models;

public record ComputerInfoModel
{
    public string Name { get; init; } = string.Empty;
    public string Domain { get; init; } = string.Empty;
    public string Workgroup { get; init; } = string.Empty;
    public string SerialNumber { get; init; } = string.Empty;
    public string Manufacturer { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
}

public record CpuInfoModel
{
    public string Name { get; init; } = string.Empty;
    public int Cores { get; init; }
    public int Threads { get; init; }
    public string Architecture { get; init; } = string.Empty;
    public string L2Cache { get; init; } = string.Empty;
    public string L3Cache { get; init; } = string.Empty;
}

public record RamInfoModel
{
    public string TotalCapacity { get; init; } = string.Empty;
    public int ConfiguredClockSpeed { get; init; }
    public string FormFactor { get; init; } = string.Empty;
    public string MemoryType { get; init; } = string.Empty;
}

public record GpuInfoModel
{
    public string Name { get; init; } = string.Empty;
    public string DriverVersion { get; init; } = string.Empty;
    public string AdapterRAM { get; init; } = string.Empty;
    public string Resolution { get; init; } = string.Empty;
}

public record MotherboardInfoModel
{
    public string Manufacturer { get; init; } = string.Empty;
    public string Product { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
}

public record BiosInfoModel
{
    public string Vendor { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string ReleaseDate { get; init; } = string.Empty;
    public string SmbiosVersion { get; init; } = string.Empty;
}

public record WindowsBuildModel
{
    public string Edition { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string BuildNumber { get; init; } = string.Empty;
    public string Architecture { get; init; } = string.Empty;
    public string InstallDate { get; init; } = string.Empty;
}

public record DeviceDetailsModel
{
    public ComputerInfoModel Computer { get; init; } = new();
    public CpuInfoModel Cpu { get; init; } = new();
    public RamInfoModel Ram { get; init; } = new();
    public List<GpuInfoModel> Gpus { get; init; } = new();
    public MotherboardInfoModel Motherboard { get; init; } = new();
    public BiosInfoModel Bios { get; init; } = new();
    public WindowsBuildModel Windows { get; init; } = new();
}

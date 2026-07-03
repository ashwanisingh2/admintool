// -----------------------------------------------------------------------
// <copyright file="BatteryInfoModel.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace SysAdminX.Core.Models;

public class BatteryInfoModel
{
    public string Name { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Chemistry { get; set; } = string.Empty;
    public uint DesignedCapacity { get; set; }
    public uint FullChargeCapacity { get; set; }
    public uint CurrentCapacity { get; set; }
    public string Status { get; set; } = string.Empty;
    public double Voltage { get; set; }
    public double WearLevel => DesignedCapacity > 0 ? (1.0 - ((double)FullChargeCapacity / DesignedCapacity)) * 100 : 0;
    public double ChargeLevel => FullChargeCapacity > 0 ? ((double)CurrentCapacity / FullChargeCapacity) * 100 : 0;
}

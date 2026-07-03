// -----------------------------------------------------------------------
// <copyright file="IBatteryManagerService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using SysAdminX.Core.Models;

namespace SysAdminX.BatteryManager.Services;

public interface IBatteryManagerService
{
    Task<Result<BatteryInfoModel>> GetBatteryInfoAsync(CancellationToken ct = default);
    Task<Result<string>> GenerateBatteryReportAsync(string destinationFolder, CancellationToken ct = default);
}

// -----------------------------------------------------------------------
// <copyright file="ITroubleshootingService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using SysAdminX.Core.Models;

namespace SysAdminX.Troubleshooting.Services;

/// <summary>
/// Service for executing system troubleshooting commands.
/// </summary>
public interface ITroubleshootingService
{
    Task<Result<TroubleshootingActionModel>> RunSfcScanAsync(CancellationToken ct = default);
    Task<Result<TroubleshootingActionModel>> RunDismCheckHealthAsync(CancellationToken ct = default);
    Task<Result<TroubleshootingActionModel>> RunDismRestoreHealthAsync(CancellationToken ct = default);
    Task<Result<TroubleshootingActionModel>> ClearTempFilesAsync(CancellationToken ct = default);
    Task<Result<TroubleshootingActionModel>> ToggleFastStartupAsync(bool enable, CancellationToken ct = default);
}

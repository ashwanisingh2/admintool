// -----------------------------------------------------------------------
// <copyright file="ISystemCleanupService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using SysAdminX.Core.Models;

namespace SysAdminX.Core.Interfaces;

public interface ISystemCleanupService
{
    Task<Result<IEnumerable<CleanupItemModel>>> GetCleanupItemsAsync(CancellationToken ct = default);
    Task<Result<long>> CalculateSpaceAsync(IEnumerable<string> itemIds, CancellationToken ct = default);
    Task<Result<bool>> PerformCleanupAsync(IEnumerable<string> itemIds, CancellationToken ct = default);
    Task<Result<CleanupResultModel>> CleanAsync(IEnumerable<CleanupItemModel> items, CancellationToken ct = default);
    Task<Result> UndoAsync(string backupDirectory, List<(string OriginalPath, string BackupPath)> movedFiles, CancellationToken ct = default);
    Task<Result> FinalizeAsync(string backupDirectory, CancellationToken ct = default);
}

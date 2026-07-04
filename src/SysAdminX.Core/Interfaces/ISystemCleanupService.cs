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
    Task<Result<IEnumerable<CleanupItemModel>>> GetCleanupItemsAsync();
    Task<Result<long>> CalculateSpaceAsync(IEnumerable<string> itemIds);
    Task<Result<bool>> PerformCleanupAsync(IEnumerable<string> itemIds);
    Task<Result<CleanupResultModel>> CleanAsync(IEnumerable<CleanupItemModel> items, CancellationToken ct = default);
    Task<Result> UndoAsync(string backupDirectory, List<(string OriginalPath, string BackupPath)> movedFiles, CancellationToken ct = default);
    Task<Result> FinalizeAsync(string backupDirectory, CancellationToken ct = default);
}

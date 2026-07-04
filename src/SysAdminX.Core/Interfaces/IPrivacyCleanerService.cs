using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SysAdminX.Core.Models;

namespace SysAdminX.Core.Interfaces;

public interface IPrivacyCleanerService
{
    Task<Result<long>> ScanCategoryAsync(string categoryId, CancellationToken ct);
    Task<Result> CleanCategoryAsync(string categoryId, CancellationToken ct);
}

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SysAdminX.Core.Models;

namespace SysAdminX.Core.Interfaces;

public interface IBrowserRepairService
{
    Task<Result<List<BrowserRepairModel>>> GetBrowsersAsync(CancellationToken ct);
    Task<Result> ClearCacheAsync(string browserId, CancellationToken ct);
    Task<Result> ResetBrowserAsync(string browserId, CancellationToken ct);
    Task<Result> ReRegisterBrowserAsync(string browserId, CancellationToken ct);
}

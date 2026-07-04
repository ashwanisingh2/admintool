using System.Threading;
using System.Threading.Tasks;
using SysAdminX.Core.Models;

namespace SysAdminX.PerformanceMode.Services;

public interface IPerformanceModeService
{
    Task<Result> ApplyProfileAsync(string profileId, CancellationToken ct = default);
    Task<Result<string>> GetCurrentProfileAsync(CancellationToken ct = default);
}

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SysAdminX.Core.Models;
using SysAdminX.StartupManager.Models;

namespace SysAdminX.StartupManager.Services;

public interface IStartupManagerService
{
    Task<Result<List<StartupAppModel>>> GetStartupAppsAsync(CancellationToken ct = default);
    Task<Result> ToggleStartupAppAsync(StartupAppModel app, CancellationToken ct = default);
}

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SysAdminX.Core.Models;

namespace SysAdminX.Core.Interfaces;

public interface ISystemRestoreService
{
    Task<Result<bool>> IsProtectionEnabledAsync(CancellationToken ct);
    Task<Result> EnableProtectionAsync(string driveLetter, CancellationToken ct);
    Task<Result<SystemRestorePoint>> CreatePointAsync(string description, CancellationToken ct);
    Task<Result<List<SystemRestorePoint>>> ListPointsAsync(CancellationToken ct);
    Task<Result> RestoreToPointAsync(int sequenceNumber, CancellationToken ct);
}

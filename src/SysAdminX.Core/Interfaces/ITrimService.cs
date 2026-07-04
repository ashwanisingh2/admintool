using System.Threading;
using System.Threading.Tasks;
using SysAdminX.Core.Models;

namespace SysAdminX.Core.Interfaces;

public interface ITrimService
{
    Task<Result<string>> RunTrimAsync(string driveLetter, CancellationToken ct = default);
}

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SysAdminX.Core.Models;
using SysAdminX.RegistryManager.Models;

namespace SysAdminX.RegistryManager.Services;

public interface IRegistryManagerService
{
    Task<Result<RegistryBackupModel>> CreateBackupAsync(string label, CancellationToken ct = default);
    Task<Result<List<RegistryBackupModel>>> GetBackupsAsync(CancellationToken ct = default);
    Task<Result> RestoreBackupAsync(string filePath, CancellationToken ct = default);
    Task<Result> DeleteBackupAsync(RegistryBackupModel backup, CancellationToken ct = default);
    void OpenBackupFolder();
}

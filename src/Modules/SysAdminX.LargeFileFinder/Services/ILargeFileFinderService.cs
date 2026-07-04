using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SysAdminX.Core.Models;
using SysAdminX.LargeFileFinder.Models;

namespace SysAdminX.LargeFileFinder.Services;

public interface ILargeFileFinderService
{
    Task<Result<List<LargeFileModel>>> ScanFilesAsync(string drive, int minSizeMB, Action<string>? progressCallback = null, CancellationToken ct = default);
    Task<Result> DeleteFileAsync(string filePath, CancellationToken ct = default);
    Task<Result> MoveFileAsync(string sourcePath, string destinationPath, CancellationToken ct = default);
}

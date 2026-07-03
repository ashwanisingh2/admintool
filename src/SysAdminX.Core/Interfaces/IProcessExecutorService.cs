// -----------------------------------------------------------------------
// <copyright file="IProcessExecutorService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using SysAdminX.Core.Models;

namespace SysAdminX.Core.Interfaces;

/// <summary>
/// Service for executing external processes and capturing their output.
/// </summary>
public interface IProcessExecutorService
{
    /// <summary>
    /// Executes a command line process and captures standard output.
    /// </summary>
    /// <param name="fileName">The executable to run (e.g., "pnputil.exe").</param>
    /// <param name="arguments">The arguments to pass.</param>
    /// <param name="requireElevation">Whether to request admin privileges.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the standard output text.</returns>
    Task<Result<string>> ExecuteAsync(string fileName, string arguments, bool requireElevation = false, CancellationToken ct = default);
}

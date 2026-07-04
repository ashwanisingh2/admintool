// -----------------------------------------------------------------------
// <copyright file="IPowerShellService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SysAdminX.Core.Models;

namespace SysAdminX.Core.Interfaces;

/// <summary>
/// Provides an abstraction for executing PowerShell commands safely.
/// All PowerShell execution in the application must go through this service.
/// </summary>
public interface IPowerShellService
{
    /// <summary>
    /// Executes a PowerShell command and returns the output as a string.
    /// </summary>
    /// <param name="command">The PowerShell command to execute.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the command output.</returns>
    Task<Result<string>> ExecuteCommandAsync(string command, CancellationToken ct = default);

    /// <summary>
    /// Executes a PowerShell script file.
    /// </summary>
    /// <param name="scriptPath">The path to the .ps1 script file.</param>
    /// <param name="parameters">Optional parameters to pass to the script.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the script output.</returns>
    Task<Result<string>> ExecuteScriptFileAsync(string scriptPath, Dictionary<string, object>? parameters = null, CancellationToken ct = default);

    /// <summary>
    /// Executes PowerShell script content directly.
    /// </summary>
    /// <param name="scriptContent">The PowerShell script content.</param>
    /// <param name="parameters">Optional parameters to pass to the script.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the script output.</returns>
    Task<Result<string>> ExecuteScriptContentAsync(string scriptContent, Dictionary<string, object>? parameters = null, CancellationToken ct = default);

    /// <summary>
    /// Executes a PowerShell command and returns the output as structured objects.
    /// </summary>
    /// <param name="command">The PowerShell command to execute.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing a list of property dictionaries.</returns>
    Task<Result<List<Dictionary<string, object?>>>> ExecuteCommandWithObjectsAsync(string command, CancellationToken ct = default);

    /// <summary>
    /// Executes a PowerShell script and streams the output via a callback.
    /// </summary>
    Task<Result<bool>> ExecuteStreamingAsync(string scriptPath, Dictionary<string, object>? parameters, Action<string> onOutput, CancellationToken ct = default);

    /// <summary>
    /// Extracts an embedded script from the assembly.
    /// </summary>
    /// <param name="resourceName">The name of the embedded resource.</param>
    /// <returns>The contents of the embedded script.</returns>
    Task<string> ExtractEmbeddedScriptAsync(string resourceName, CancellationToken ct = default);
}

// -----------------------------------------------------------------------
// <copyright file="IPortableToolsService.cs" company="SysAdminX">
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
/// Service for enumerating and launching the bundled portable tools
/// (CPU-Z, GPU-Z, CrystalDiskInfo, etc.).
/// </summary>
public interface IPortableToolsService
{
    /// <summary>Enumerates the tools available in the portable-tools directory.</summary>
    Task<Result<IEnumerable<PortableToolModel>>> GetAvailableToolsAsync(CancellationToken ct = default);

    /// <summary>Launches the tool with the given id. Returns true on launch success.</summary>
    Task<Result<bool>> RunToolAsync(string toolId, CancellationToken ct = default);
}

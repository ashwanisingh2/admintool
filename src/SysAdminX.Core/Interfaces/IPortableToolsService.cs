// -----------------------------------------------------------------------
// <copyright file="IPortableToolsService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using SysAdminX.Core.Models;

namespace SysAdminX.Core.Interfaces;

public interface IPortableToolsService
{
    Task<Result<IEnumerable<PortableToolModel>>> GetAvailableToolsAsync();
    Task<Result<bool>> RunToolAsync(string toolId);
}

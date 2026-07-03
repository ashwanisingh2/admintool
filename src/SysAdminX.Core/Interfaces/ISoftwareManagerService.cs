// -----------------------------------------------------------------------
// <copyright file="ISoftwareManagerService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using SysAdminX.Core.Models;

namespace SysAdminX.Core.Interfaces;

public interface ISoftwareManagerService
{
    Task<Result<IEnumerable<SoftwareItemModel>>> GetInstalledSoftwareAsync();
    Task<Result<bool>> UninstallSoftwareAsync(string uninstallString);
}

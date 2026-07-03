// -----------------------------------------------------------------------
// <copyright file="IDeviceDetailsService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using SysAdminX.Core.Models;

namespace SysAdminX.DeviceDetails.Services;

/// <summary>
/// Service for fetching comprehensive device details.
/// </summary>
public interface IDeviceDetailsService
{
    /// <summary>
    /// Gets all hardware and OS details for the current device.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the device details.</returns>
    Task<Result<DeviceDetailsModel>> GetDeviceDetailsAsync(CancellationToken ct = default);
}

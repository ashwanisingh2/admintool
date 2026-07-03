// -----------------------------------------------------------------------
// <copyright file="IServiceManagerService.cs" company="SysAdminX">
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
/// Defines the contract for managing Windows Services.
/// </summary>
public interface IServiceManagerService
{
    /// <summary>
    /// Gets a list of all Windows services.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the list of services.</returns>
    Task<Result<IEnumerable<WindowsServiceModel>>> GetServicesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts the specified service.
    /// </summary>
    /// <param name="serviceName">The name of the service.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result<bool>> StartServiceAsync(string serviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the specified service.
    /// </summary>
    /// <param name="serviceName">The name of the service.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result<bool>> StopServiceAsync(string serviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restarts the specified service.
    /// </summary>
    /// <param name="serviceName">The name of the service.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result<bool>> RestartServiceAsync(string serviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the startup type of the specified service.
    /// </summary>
    /// <param name="serviceName">The name of the service.</param>
    /// <param name="startMode">The new startup mode (Automatic, Manual, Disabled).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result<bool>> ChangeStartupTypeAsync(string serviceName, string startMode, CancellationToken cancellationToken = default);
}

// -----------------------------------------------------------------------
// <copyright file="WindowsServiceModel.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace SysAdminX.Core.Models;

/// <summary>
/// Represents a Windows Service on the system.
/// </summary>
public class WindowsServiceModel
{
    /// <summary>
    /// Gets or sets the service name (internal ID).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the friendly display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the service description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current status (e.g., Running, Stopped).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the startup type (e.g., Automatic, Manual, Disabled).
    /// </summary>
    public string StartType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the account the service runs as.
    /// </summary>
    public string StartName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the service can be stopped.
    /// </summary>
    public bool CanStop { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the service can be paused.
    /// </summary>
    public bool CanPauseAndContinue { get; set; }

    /// <summary>
    /// Gets a value indicating whether the service is currently running.
    /// </summary>
    public bool IsRunning => Status?.Equals("Running", System.StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>
    /// Gets a value indicating whether the service is stopped.
    /// </summary>
    public bool IsStopped => Status?.Equals("Stopped", System.StringComparison.OrdinalIgnoreCase) == true;
}

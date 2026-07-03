// -----------------------------------------------------------------------
// <copyright file="AppTheme.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace SysAdminX.Core.Models;

/// <summary>
/// Represents the application theme options.
/// </summary>
public enum AppTheme
{
    /// <summary>
    /// Follow the system theme (Windows personalization setting).
    /// </summary>
    System = 0,

    /// <summary>
    /// Dark theme.
    /// </summary>
    Dark = 1,

    /// <summary>
    /// Light theme.
    /// </summary>
    Light = 2
}

// -----------------------------------------------------------------------
// <copyright file="INavigationService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace SysAdminX.Core.Interfaces;

/// <summary>
/// Provides navigation services for the application shell.
/// Manages page transitions within the main content frame.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Gets the current page type being displayed.
    /// </summary>
    Type? CurrentPage { get; }

    /// <summary>
    /// Navigates to the specified page type.
    /// </summary>
    /// <param name="pageType">The type of the page/view to navigate to.</param>
    /// <returns>True if navigation was successful.</returns>
    bool NavigateTo(Type pageType);

    /// <summary>
    /// Navigates to the specified page type with a parameter.
    /// </summary>
    /// <param name="pageType">The type of the page/view to navigate to.</param>
    /// <param name="parameter">The parameter to pass to the page.</param>
    /// <returns>True if navigation was successful.</returns>
    bool NavigateTo(Type pageType, object? parameter);

    /// <summary>
    /// Navigates back to the previous page.
    /// </summary>
    /// <returns>True if navigation was successful.</returns>
    bool GoBack();

    /// <summary>
    /// Gets whether the navigation service can go back.
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// Event raised when navigation occurs.
    /// </summary>
    event EventHandler<Type>? Navigated;
}

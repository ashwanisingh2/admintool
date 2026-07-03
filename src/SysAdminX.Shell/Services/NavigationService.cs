// -----------------------------------------------------------------------
// <copyright file="NavigationService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using Wpf.Ui.Controls;

namespace SysAdminX.Shell.Services;

/// <summary>
/// Concrete implementation of <see cref="INavigationService"/>.
/// Manages page navigation within the WPF-UI NavigationView.
/// </summary>
public class NavigationService : INavigationService
{
    private readonly ILogger<NavigationService> _logger;
    private INavigationView? _navigationView;

    /// <inheritdoc />
    public Type? CurrentPage { get; private set; }

    /// <inheritdoc />
    public bool CanGoBack => _navigationView?.CanGoBack ?? false;

    /// <inheritdoc />
    public event EventHandler<Type>? Navigated;

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public NavigationService(ILogger<NavigationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sets the NavigationView control used for navigation.
    /// Must be called during shell initialization.
    /// </summary>
    /// <param name="navigationView">The WPF-UI NavigationView control.</param>
    public void SetNavigationView(INavigationView navigationView)
    {
        _navigationView = navigationView ?? throw new ArgumentNullException(nameof(navigationView));
    }

    /// <inheritdoc />
    public bool NavigateTo(Type pageType)
    {
        return NavigateTo(pageType, null);
    }

    /// <inheritdoc />
    public bool NavigateTo(Type pageType, object? parameter)
    {
        try
        {
            if (_navigationView == null)
            {
                _logger.LogError("NavigationView is not set. Call SetNavigationView() first.");
                return false;
            }

            if (CurrentPage == pageType)
            {
                _logger.LogDebug("Already on page {Page}, skipping navigation", pageType.Name);
                return true;
            }

            _logger.LogInformation("Navigating to {Page}", pageType.Name);

            var result = _navigationView.Navigate(pageType);
            if (result)
            {
                CurrentPage = pageType;
                Navigated?.Invoke(this, pageType);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Navigation failed for page {Page}", pageType.Name);
            return false;
        }
    }

    /// <inheritdoc />
    public bool GoBack()
    {
        try
        {
            if (_navigationView == null || !_navigationView.CanGoBack)
            {
                return false;
            }

            return _navigationView.GoBack();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GoBack navigation failed");
            return false;
        }
    }
}

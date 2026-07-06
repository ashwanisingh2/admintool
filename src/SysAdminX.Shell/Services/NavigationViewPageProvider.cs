// -----------------------------------------------------------------------
// <copyright file="NavigationViewPageProvider.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Windows;
using Microsoft.Extensions.Logging;
using Wpf.Ui;

namespace SysAdminX.Shell.Services;

/// <summary>
/// Provides page instances to the WPF-UI NavigationView from the DI container.
/// </summary>
public class NavigationViewPageProvider : IPageService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NavigationViewPageProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationViewPageProvider"/> class.
    /// </summary>
    /// <param name="serviceProvider">The DI service provider.</param>
    /// <param name="logger">The logger instance.</param>
    public NavigationViewPageProvider(IServiceProvider serviceProvider, ILogger<NavigationViewPageProvider> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public T? GetPage<T>() where T : class
    {
        var page = _serviceProvider.GetService(typeof(T)) as T;
        if (page is null)
        {
            _logger.LogError("Page of type {PageType} is not registered in the DI container.", typeof(T).FullName);
        }

        return page;
    }

    /// <inheritdoc />
    public FrameworkElement? GetPage(Type pageType)
    {
        var page = _serviceProvider.GetService(pageType) as FrameworkElement;
        if (page is null)
        {
            _logger.LogError("Navigation target page is not registered in DI: {PageType}", pageType.FullName);
            throw new InvalidOperationException($"Page '{pageType.FullName}' is not registered in the DI container. Add it in App.xaml.cs ConfigureServices().");
        }

        return page;
    }
}

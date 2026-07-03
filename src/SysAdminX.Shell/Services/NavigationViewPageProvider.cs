// -----------------------------------------------------------------------
// <copyright file="NavigationViewPageProvider.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Windows;
using Wpf.Ui;

namespace SysAdminX.Shell.Services;

/// <summary>
/// Provides page instances to the WPF-UI NavigationView from the DI container.
/// </summary>
public class NavigationViewPageProvider : IPageService
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationViewPageProvider"/> class.
    /// </summary>
    /// <param name="serviceProvider">The DI service provider.</param>
    public NavigationViewPageProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc />
    public T? GetPage<T>() where T : class
    {
        return (T?)_serviceProvider.GetService(typeof(T));
    }

    /// <inheritdoc />
    public FrameworkElement? GetPage(Type pageType)
    {
        return _serviceProvider.GetService(pageType) as FrameworkElement;
    }
}

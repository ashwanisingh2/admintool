// -----------------------------------------------------------------------
// <copyright file="MainWindowViewModel.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;

namespace SysAdminX.Shell.ViewModels;

/// <summary>
/// ViewModel for the main application window (shell).
/// Manages navigation menu items and the active page state.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly INavigationService _navigationService;

    /// <summary>
    /// Gets the application title.
    /// </summary>
    public string AppTitle => "SysAdminX";

    /// <summary>
    /// Gets the application version.
    /// </summary>
    public string AppVersion => "v0.1.0-alpha";

    /// <summary>
    /// Gets the collection of navigation menu items.
    /// </summary>
    public ObservableCollection<NavigationMenuItem> MenuItems { get; } = new();

    /// <summary>
    /// Gets the collection of footer navigation items.
    /// </summary>
    public ObservableCollection<NavigationMenuItem> FooterItems { get; } = new();

    [ObservableProperty]
    private NavigationMenuItem? _selectedMenuItem;

    [ObservableProperty]
    private string _currentPageTitle = "Dashboard";

    [ObservableProperty]
    private bool _isNavigating;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="navigationService">The navigation service.</param>
    public MainWindowViewModel(ILogger<MainWindowViewModel> logger, INavigationService navigationService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

        InitializeMenuItems();
    }

    /// <summary>
    /// Initializes the navigation menu items for the sidebar.
    /// </summary>
    private void InitializeMenuItems()
    {
        MenuItems.Add(new NavigationMenuItem
        {
            Title = "Dashboard",
            IconGlyph = "\uE80F",  // Home icon
            PageTag = "Dashboard",
            IsSelected = true
        });

        MenuItems.Add(new NavigationMenuItem
        {
            Title = "Device Details",
            IconGlyph = "\uE7F7",  // PC icon
            PageTag = "DeviceDetails",
            IsEnabled = false       // Phase 1 - pending
        });

        MenuItems.Add(new NavigationMenuItem
        {
            Title = "Driver Manager",
            IconGlyph = "\uE964",  // Processing icon
            PageTag = "DriverManager",
            IsEnabled = false
        });

        MenuItems.Add(new NavigationMenuItem
        {
            Title = "Patch Manager",
            IconGlyph = "\uE777",  // Update icon
            PageTag = "PatchManager",
            IsEnabled = false
        });

        MenuItems.Add(new NavigationMenuItem
        {
            Title = "Network Toolkit",
            IconGlyph = "\uE968",  // Network icon
            PageTag = "NetworkToolkit",
            IsEnabled = false
        });

        MenuItems.Add(new NavigationMenuItem
        {
            Title = "Troubleshooting",
            IconGlyph = "\uE90F",  // Wrench icon
            PageTag = "Troubleshooting",
            IsEnabled = false
        });

        MenuItems.Add(new NavigationMenuItem
        {
            Title = "AI Assistant",
            IconGlyph = "\uE99A",  // Robot icon
            PageTag = "AIAssistant",
            IsEnabled = false
        });

        MenuItems.Add(new NavigationMenuItem
        {
            Title = "Reports",
            IconGlyph = "\uE9F9",  // Document icon
            PageTag = "Reports",
            IsEnabled = false
        });

        // Footer items
        FooterItems.Add(new NavigationMenuItem
        {
            Title = "Logs",
            IconGlyph = "\uE7BA",  // List icon
            PageTag = "Logs",
            IsEnabled = false
        });

        FooterItems.Add(new NavigationMenuItem
        {
            Title = "Settings",
            IconGlyph = "\uE713",  // Settings gear icon
            PageTag = "Settings",
            IsEnabled = false
        });
    }

    /// <summary>
    /// Navigates to the specified page.
    /// </summary>
    /// <param name="pageTag">The tag identifying the page.</param>
    [RelayCommand]
    private void NavigateToPage(string pageTag)
    {
        try
        {
            _logger.LogInformation("Navigation requested: {Page}", pageTag);
            CurrentPageTitle = pageTag switch
            {
                "Dashboard" => "Dashboard",
                "DeviceDetails" => "Device Details",
                "DriverManager" => "Driver Manager",
                "PatchManager" => "Patch Manager",
                "NetworkToolkit" => "Network Toolkit",
                "Troubleshooting" => "Troubleshooting",
                "AIAssistant" => "AI Assistant",
                "Reports" => "Reports",
                "Logs" => "Logs",
                "Settings" => "Settings",
                _ => pageTag
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Navigation failed for page: {Page}", pageTag);
        }
    }
}

/// <summary>
/// Represents a navigation menu item in the sidebar.
/// </summary>
public partial class NavigationMenuItem : ObservableObject
{
    /// <summary>
    /// Gets or sets the display title.
    /// </summary>
    [ObservableProperty]
    private string _title = string.Empty;

    /// <summary>
    /// Gets or sets the Segoe Fluent Icons glyph.
    /// </summary>
    [ObservableProperty]
    private string _iconGlyph = string.Empty;

    /// <summary>
    /// Gets or sets the page identifier tag.
    /// </summary>
    public string PageTag { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this item is currently selected.
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// Gets or sets whether this menu item is enabled (implemented).
    /// </summary>
    [ObservableProperty]
    private bool _isEnabled = true;
}

// -----------------------------------------------------------------------
// <copyright file="AppConfigModel.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace SysAdminX.Core.Models;

/// <summary>
/// Represents the global application configuration settings.
/// </summary>
public class AppConfigModel
{
    public string Theme { get; set; } = "Dark"; // Dark, Light
    public bool StartOnBoot { get; set; } = false;
    public string DefaultExportDirectory { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\SysAdminX_Reports";
    public string SysinternalsPath { get; set; } = string.Empty;

    /// <summary>
    /// When true, the sidebar collapses to icons-only mode to save horizontal
    /// space. Bound to the NavigationView PaneDisplayMode in MainWindow.xaml.
    /// </summary>
    public bool CompactSidebar { get; set; } = false;

    /// <summary>
    /// When true, the Logs Viewer auto-tails the latest log file in real time
    /// (uses a FileSystemWatcher).
    /// </summary>
    public bool LogAutoTail { get; set; } = true;

    /// <summary>
    /// When true, the app checks GitHub releases on startup and shows a toast
    /// when a newer version is available.
    /// </summary>
    public bool CheckForUpdatesOnStartup { get; set; } = true;

    /// <summary>
    /// Module keys that the user has hidden from the sidebar. Keys match the
    /// TargetPageType.Name of each NavigationViewItem (e.g. "DashboardView").
    /// Modules in this set are skipped when building the sidebar at startup.
    /// </summary>
    public HashSet<string> HiddenModules { get; set; } = new();

    /// <summary>
    /// GitHub repository to check for updates (format: "owner/repo").
    /// Defaults to the public SysAdminX repository.
    /// </summary>
    public string UpdateRepository { get; set; } = "ashwanisingh2/admintool";

    /// <summary>
    /// Optional: pin to a specific version. If set, the update checker will
    /// not prompt for versions older than this. Empty string = always prompt.
    /// </summary>
    public string MinimumVersion { get; set; } = string.Empty;
}

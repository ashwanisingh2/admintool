// -----------------------------------------------------------------------
// <copyright file="AppConfigModel.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;

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
}

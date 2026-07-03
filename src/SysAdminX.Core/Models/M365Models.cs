// -----------------------------------------------------------------------
// <copyright file="M365Models.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace SysAdminX.Core.Models;

/// <summary>
/// Represents a Microsoft 365 / Entra ID User.
/// </summary>
public class M365UserModel
{
    public string DisplayName { get; set; } = string.Empty;
    public string UserPrincipalName { get; set; } = string.Empty;
    public bool IsLicensed { get; set; }
    public string Licenses { get; set; } = string.Empty;
    public string BlockCredential { get; set; } = string.Empty; // Using string to handle true/false visually
}

/// <summary>
/// Represents an Exchange Online Mailbox.
/// </summary>
public class M365MailboxModel
{
    public string DisplayName { get; set; } = string.Empty;
    public string PrimarySmtpAddress { get; set; } = string.Empty;
    public string IssueWarningQuota { get; set; } = string.Empty;
    public string TotalItemSize { get; set; } = string.Empty;
    public string ItemCount { get; set; } = string.Empty;
}

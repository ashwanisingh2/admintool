// -----------------------------------------------------------------------
// <copyright file="ActiveDirectoryModels.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace SysAdminX.Core.Models;

/// <summary>
/// Represents an Active Directory User.
/// </summary>
public class AdUserModel
{
    public string Name { get; set; } = string.Empty;
    public string SamAccountName { get; set; } = string.Empty;
    public string UserPrincipalName { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string GivenName { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
}

/// <summary>
/// Represents an Active Directory Group.
/// </summary>
public class AdGroupModel
{
    public string Name { get; set; } = string.Empty;
    public string SamAccountName { get; set; } = string.Empty;
    public string GroupCategory { get; set; } = string.Empty;
    public string GroupScope { get; set; } = string.Empty;
}

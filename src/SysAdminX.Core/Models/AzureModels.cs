// -----------------------------------------------------------------------
// <copyright file="AzureModels.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace SysAdminX.Core.Models;

/// <summary>
/// Represents an Azure Resource Group.
/// </summary>
public class AzureResourceGroupModel
{
    public string ResourceGroupName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string ProvisioningState { get; set; } = string.Empty;
}

/// <summary>
/// Represents an Azure Virtual Machine.
/// </summary>
public class AzureVmModel
{
    public string Name { get; set; } = string.Empty;
    public string ResourceGroupName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string VmSize { get; set; } = string.Empty;
    public string OsType { get; set; } = string.Empty;
    public string ProvisioningState { get; set; } = string.Empty;
}

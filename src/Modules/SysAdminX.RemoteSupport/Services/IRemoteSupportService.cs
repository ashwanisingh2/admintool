// -----------------------------------------------------------------------
// <copyright file="IRemoteSupportService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Threading.Tasks;

namespace SysAdminX.RemoteSupport.Services;

/// <summary>
/// Service for launching remote support tools.
/// </summary>
public interface IRemoteSupportService
{
    Task LaunchRdpAsync(string hostname);
    Task LaunchComputerManagementAsync(string hostname);
    Task LaunchRemoteCommandPromptAsync(string hostname);
    Task LaunchRemotePowerShellAsync(string hostname);
}

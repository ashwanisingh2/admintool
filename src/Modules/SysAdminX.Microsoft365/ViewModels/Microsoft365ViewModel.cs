// -----------------------------------------------------------------------
// <copyright file="Microsoft365ViewModel.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Models;
using SysAdminX.Microsoft365.Services;

namespace SysAdminX.Microsoft365.ViewModels;

/// <summary>
/// ViewModel for the Microsoft 365 module.
/// </summary>
public partial class Microsoft365ViewModel : ObservableObject
{
    private readonly ILogger<Microsoft365ViewModel> _logger;
    private readonly IMicrosoft365Service _m365Service;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private ObservableCollection<M365UserModel> _users = new();

    [ObservableProperty]
    private ObservableCollection<M365MailboxModel> _mailboxes = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isMissingModules;

    public Microsoft365ViewModel(ILogger<Microsoft365ViewModel> logger, IMicrosoft365Service m365Service)
    {
        _logger = logger;
        _m365Service = m365Service;
    }

    [RelayCommand]
    public async Task SearchAsync()
    {
        IsLoading = true;
        IsMissingModules = false;
        
        try
        {
            var isConnected = await _m365Service.IsConnectedAsync();
            if (!isConnected)
            {
                IsMissingModules = true;
                return;
            }
            
            var usersResult = await _m365Service.GetUsersAsync(SearchQuery);
            var mailboxesResult = await _m365Service.GetMailboxesAsync(SearchQuery);
            
            Users.Clear();
            foreach (var u in usersResult) Users.Add(u);
            
            Mailboxes.Clear();
            foreach (var m in mailboxesResult) Mailboxes.Add(m);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "M365 Search failed");
            IsMissingModules = true;
        }
        finally
        {
            IsLoading = false;
        }
    }
}

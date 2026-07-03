// -----------------------------------------------------------------------
// <copyright file="ActiveDirectoryViewModel.cs" company="SysAdminX">
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
using SysAdminX.ActiveDirectory.Services;

namespace SysAdminX.ActiveDirectory.ViewModels;

/// <summary>
/// ViewModel for the Active Directory module.
/// </summary>
public partial class ActiveDirectoryViewModel : ObservableObject
{
    private readonly ILogger<ActiveDirectoryViewModel> _logger;
    private readonly IActiveDirectoryService _adService;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private ObservableCollection<AdUserModel> _users = new();

    [ObservableProperty]
    private ObservableCollection<AdGroupModel> _groups = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isMissingRsat;

    public ActiveDirectoryViewModel(ILogger<ActiveDirectoryViewModel> logger, IActiveDirectoryService adService)
    {
        _logger = logger;
        _adService = adService;
    }

    [RelayCommand]
    public async Task SearchAsync()
    {
        IsLoading = true;
        IsMissingRsat = false;
        
        try
        {
            var usersResult = await _adService.SearchUsersAsync(SearchQuery);
            var groupsResult = await _adService.SearchGroupsAsync(SearchQuery);
            
            Users.Clear();
            foreach (var u in usersResult) Users.Add(u);
            
            Groups.Clear();
            foreach (var g in groupsResult) Groups.Add(g);
            
            // If lists are completely empty and query is empty, assume RSAT might be missing. 
            // In a real app we'd expose CheckModuleAvailabilityAsync() directly, but this is a heuristic.
            if (usersResult.Count == 0 && groupsResult.Count == 0 && string.IsNullOrWhiteSpace(SearchQuery))
            {
                IsMissingRsat = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search failed");
            IsMissingRsat = true;
        }
        finally
        {
            IsLoading = false;
        }
    }
}

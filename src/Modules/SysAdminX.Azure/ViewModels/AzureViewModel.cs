// -----------------------------------------------------------------------
// <copyright file="AzureViewModel.cs" company="SysAdminX">
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
using SysAdminX.Azure.Services;

namespace SysAdminX.Azure.ViewModels;

/// <summary>
/// ViewModel for the Azure module.
/// </summary>
public partial class AzureViewModel : ObservableObject
{
    private readonly ILogger<AzureViewModel> _logger;
    private readonly IAzureService _azureService;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private ObservableCollection<AzureResourceGroupModel> _resourceGroups = new();

    [ObservableProperty]
    private ObservableCollection<AzureVmModel> _virtualMachines = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isMissingModules;

    public AzureViewModel(ILogger<AzureViewModel> logger, IAzureService azureService)
    {
        _logger = logger;
        _azureService = azureService;
    }

    [RelayCommand]
    public async Task SearchAsync()
    {
        IsLoading = true;
        IsMissingModules = false;
        
        try
        {
            var isConnected = await _azureService.IsConnectedAsync();
            if (!isConnected)
            {
                IsMissingModules = true;
                return;
            }
            
            var rgResult = await _azureService.GetResourceGroupsAsync(SearchQuery);
            var vmResult = await _azureService.GetVirtualMachinesAsync(SearchQuery);
            
            ResourceGroups.Clear();
            foreach (var r in rgResult) ResourceGroups.Add(r);
            
            VirtualMachines.Clear();
            foreach (var v in vmResult) VirtualMachines.Add(v);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure Search failed");
            IsMissingModules = true;
        }
        finally
        {
            IsLoading = false;
        }
    }
}

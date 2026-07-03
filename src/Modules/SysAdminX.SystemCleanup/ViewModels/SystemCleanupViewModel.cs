// -----------------------------------------------------------------------
// <copyright file="SystemCleanupViewModel.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.SystemCleanup.ViewModels;

public partial class SystemCleanupViewModel : ObservableObject
{
    private readonly ILogger<SystemCleanupViewModel> _logger;
    private readonly ISystemCleanupService _cleanupService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public ObservableCollection<CleanupItemModel> CleanupItems { get; } = new();

    public SystemCleanupViewModel(
        ILogger<SystemCleanupViewModel> logger,
        ISystemCleanupService cleanupService)
    {
        _logger = logger;
        _cleanupService = cleanupService;
    }

    [RelayCommand]
    private async Task LoadItemsAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        CleanupItems.Clear();

        var result = await _cleanupService.GetCleanupItemsAsync();
        if (result.IsSuccess && result.Value != null)
        {
            foreach (var item in result.Value)
            {
                CleanupItems.Add(item);
            }
        }
        else
        {
            ErrorMessage = result.ErrorMessage ?? "Failed to load cleanup items.";
        }

        IsLoading = false;
    }

    [RelayCommand]
    private async Task PerformCleanupAsync()
    {
        var selectedIds = CleanupItems.Where(i => i.IsSelected).Select(i => i.Id).ToList();
        if (!selectedIds.Any()) return;

        IsLoading = true;
        ErrorMessage = string.Empty;

        var result = await _cleanupService.PerformCleanupAsync(selectedIds);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.ErrorMessage ?? "Cleanup failed.";
        }

        await LoadItemsAsync();
    }
}

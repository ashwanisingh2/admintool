// -----------------------------------------------------------------------
// <copyright file="PortableToolsViewModel.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.PortableTools.ViewModels;

public partial class PortableToolsViewModel : ObservableObject
{
    private readonly ILogger<PortableToolsViewModel> _logger;
    private readonly IPortableToolsService _toolsService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public ObservableCollection<PortableToolModel> Tools { get; } = new();

    public PortableToolsViewModel(
        ILogger<PortableToolsViewModel> logger,
        IPortableToolsService toolsService)
    {
        _logger = logger;
        _toolsService = toolsService;
    }

    [RelayCommand]
    private async Task LoadToolsAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        Tools.Clear();

        var result = await _toolsService.GetAvailableToolsAsync();
        if (result.IsSuccess && result.Value != null)
        {
            foreach (var item in result.Value)
            {
                Tools.Add(item);
            }
        }
        else
        {
            ErrorMessage = result.ErrorMessage ?? "Failed to load tools.";
        }

        IsLoading = false;
    }

    [RelayCommand]
    private async Task RunToolAsync(string toolId)
    {
        if (string.IsNullOrEmpty(toolId)) return;

        IsLoading = true;
        ErrorMessage = string.Empty;

        var result = await _toolsService.RunToolAsync(toolId);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.ErrorMessage ?? "Failed to launch tool.";
        }

        IsLoading = false;
    }
}

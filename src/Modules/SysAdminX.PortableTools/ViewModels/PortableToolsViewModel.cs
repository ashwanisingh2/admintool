// -----------------------------------------------------------------------
// <copyright file="PortableToolsViewModel.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.PortableTools.ViewModels;

/// <summary>
/// ViewModel for the Portable Tools module.
///
/// Improvements applied:
///   - All async commands wrapped in try/finally so an exception can no
///     longer leave IsLoading stuck on.
///   - Real cancellation token propagation.
///   - Toast notifications on RunTool outcome.
/// </summary>
public partial class PortableToolsViewModel : ObservableObject
{
    private readonly ILogger<PortableToolsViewModel> _logger;
    private readonly IPortableToolsService _toolsService;
    private readonly IToastNotificationService _toastService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _errorMessage = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public ObservableCollection<PortableToolModel> Tools { get; } = new();

    public PortableToolsViewModel(
        ILogger<PortableToolsViewModel> logger,
        IPortableToolsService toolsService,
        IToastNotificationService toastService)
    {
        _logger = logger;
        _toolsService = toolsService;
        _toastService = toastService;
    }

    [RelayCommand]
    private async Task LoadToolsAsync(CancellationToken ct = default)
    {
        if (IsLoading) return;
        IsLoading = true;
        ErrorMessage = string.Empty;
        Tools.Clear();

        try
        {
            var result = await _toolsService.GetAvailableToolsAsync(ct);
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
                _toastService.ShowError("Failed to load portable tools", ErrorMessage);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Load tools cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Load tools threw an exception.");
            ErrorMessage = ex.Message;
            _toastService.ShowError("Failed to load portable tools", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RunToolAsync(string toolId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(toolId)) return;

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var result = await _toolsService.RunToolAsync(toolId, ct);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.ErrorMessage ?? "Failed to launch tool.";
                _toastService.ShowError("Failed to launch tool", ErrorMessage);
            }
            else
            {
                _toastService.ShowSuccess("Tool launched", toolId);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Run tool cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Run tool threw an exception.");
            ErrorMessage = ex.Message;
            _toastService.ShowError("Failed to launch tool", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }
}

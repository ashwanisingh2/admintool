using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.BrowserRepair.ViewModels;

/// <summary>
/// Per-browser view model used by the Browser Repair module.
///
/// Improvements applied:
///   - All three async operations (ClearCache, Reset, ReRegister) wrapped in
///     try/finally so an exception can no longer leave IsProcessing stuck on.
///   - Modal MessageBox calls replaced with toast notifications.
///   - The Reset confirmation dialog stays as a MessageBox because it's a
///     destructive confirmation — that genuinely needs to block.
/// </summary>
public partial class BrowserViewModel : ObservableObject
{
    private readonly IBrowserRepairService _service;
    private readonly IToastNotificationService _toastService;
    private readonly ILogger<BrowserViewModel> _logger;

    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public bool IsInstalled { get; set; }
    public string InstallPath { get; set; } = string.Empty;

    [ObservableProperty]
    private long _cacheSize;

    [ObservableProperty]
    private bool _isProcessing;

    public string CacheSizeString => CacheSize > 0 ? $"{CacheSize / 1024.0 / 1024.0:F2} MB" : "0 MB";

    public IAsyncRelayCommand ClearCacheCommand { get; }
    public IAsyncRelayCommand ResetCommand { get; }
    public IAsyncRelayCommand ReRegisterCommand { get; }

    public BrowserViewModel(
        BrowserRepairModel model,
        IBrowserRepairService service,
        IToastNotificationService toastService,
        ILogger<BrowserViewModel> logger)
    {
        _service = service;
        _toastService = toastService;
        _logger = logger;
        Id = model.Id;
        Name = model.Name;
        Icon = model.Icon;
        IsInstalled = model.IsInstalled;
        InstallPath = model.InstallPath;
        CacheSize = model.CacheSize;

        ClearCacheCommand = new AsyncRelayCommand(ClearCacheAsync, () => IsInstalled && !IsProcessing);
        ResetCommand = new AsyncRelayCommand(ResetAsync, () => IsInstalled && !IsProcessing);
        ReRegisterCommand = new AsyncRelayCommand(ReRegisterAsync, () => IsInstalled && !IsProcessing);
    }

    partial void OnCacheSizeChanged(long value)
    {
        OnPropertyChanged(nameof(CacheSizeString));
    }

    private void NotifyCommands()
    {
        ClearCacheCommand.NotifyCanExecuteChanged();
        ResetCommand.NotifyCanExecuteChanged();
        ReRegisterCommand.NotifyCanExecuteChanged();
    }

    private async Task ClearCacheAsync()
    {
        IsProcessing = true;
        NotifyCommands();
        try
        {
            var result = await _service.ClearCacheAsync(Id, CancellationToken.None);
            if (result.IsSuccess)
            {
                CacheSize = 0;
                _toastService.ShowSuccess($"{Name} cache cleared", "Cache was cleared successfully.");
            }
            else
            {
                _toastService.ShowError($"Failed to clear {Name} cache", result.ErrorMessage ?? "Unknown error.");
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "ClearCache threw an exception for {Browser}", Name);
            _toastService.ShowError($"Failed to clear {Name} cache", ex.Message);
        }
        finally
        {
            IsProcessing = false;
            NotifyCommands();
        }
    }

    private async Task ResetAsync()
    {
        // Destructive confirmation — this stays as a modal MessageBox because
        // we genuinely need the user's yes/no answer before proceeding.
        if (MessageBox.Show(
            $"Are you sure you want to reset {Name}? This will delete preferences and local state.",
            "Confirm Reset",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        IsProcessing = true;
        NotifyCommands();
        try
        {
            var result = await _service.ResetBrowserAsync(Id, CancellationToken.None);
            if (result.IsSuccess)
            {
                _toastService.ShowSuccess($"{Name} reset", "Browser was reset successfully.");
            }
            else
            {
                _toastService.ShowError($"Failed to reset {Name}", result.ErrorMessage ?? "Unknown error.");
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "ResetBrowser threw an exception for {Browser}", Name);
            _toastService.ShowError($"Failed to reset {Name}", ex.Message);
        }
        finally
        {
            IsProcessing = false;
            NotifyCommands();
        }
    }

    private async Task ReRegisterAsync()
    {
        IsProcessing = true;
        NotifyCommands();
        try
        {
            var result = await _service.ReRegisterBrowserAsync(Id, CancellationToken.None);
            if (result.IsSuccess)
            {
                _toastService.ShowSuccess($"{Name} re-registered", "Browser was re-registered successfully.");
            }
            else
            {
                _toastService.ShowError($"Failed to re-register {Name}", result.ErrorMessage ?? "Unknown error.");
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "ReRegisterBrowser threw an exception for {Browser}", Name);
            _toastService.ShowError($"Failed to re-register {Name}", ex.Message);
        }
        finally
        {
            IsProcessing = false;
            NotifyCommands();
        }
    }
}

/// <summary>
/// Top-level view model for the Browser Repair module.
///
/// Improvements applied:
///   - LoadBrowsersAsync wrapped in try/finally so an exception can no longer
///     leave IsLoading stuck on.
///   - IToastNotificationService injected.
/// </summary>
public partial class BrowserRepairViewModel : ObservableObject
{
    private readonly IBrowserRepairService _service;
    private readonly IToastNotificationService _toastService;
    private readonly ILogger<BrowserRepairViewModel> _logger;

    public ObservableCollection<BrowserViewModel> Browsers { get; } = new();

    [ObservableProperty]
    private bool _isLoading;

    public BrowserRepairViewModel(
        IBrowserRepairService service,
        IToastNotificationService toastService,
        ILogger<BrowserRepairViewModel> logger)
    {
        _service = service;
        _toastService = toastService;
        _logger = logger;
        LoadBrowsersCommand = new AsyncRelayCommand(LoadBrowsersAsync);
    }

    public IAsyncRelayCommand LoadBrowsersCommand { get; }

    private async Task LoadBrowsersAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        try
        {
            var result = await _service.GetBrowsersAsync(CancellationToken.None);
            if (result.IsSuccess && result.Value != null)
            {
                Browsers.Clear();
                foreach (var b in result.Value)
                {
                    Browsers.Add(new BrowserViewModel(b, _service, _toastService, _logger));
                }
            }
            else
            {
                _toastService.ShowError("Failed to load browsers", result.ErrorMessage ?? "Unknown error.");
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "LoadBrowsers threw an exception.");
            _toastService.ShowError("Failed to load browsers", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }
}

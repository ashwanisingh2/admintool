using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.BrowserRepair.ViewModels;

public partial class BrowserViewModel : ObservableObject
{
    private readonly IBrowserRepairService _service;
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

    public BrowserViewModel(BrowserRepairModel model, IBrowserRepairService service)
    {
        _service = service;
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
        var result = await _service.ClearCacheAsync(Id, CancellationToken.None);
        if (result.IsSuccess)
        {
            CacheSize = 0;
            MessageBox.Show($"{Name} cache cleared successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show($"Failed to clear cache: {result.ErrorMessage}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        IsProcessing = false;
        NotifyCommands();
    }

    private async Task ResetAsync()
    {
        if (MessageBox.Show($"Are you sure you want to reset {Name}? This will delete preferences and local state.", "Confirm Reset", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        IsProcessing = true;
        NotifyCommands();
        var result = await _service.ResetBrowserAsync(Id, CancellationToken.None);
        if (result.IsSuccess)
        {
            MessageBox.Show($"{Name} reset successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show($"Failed to reset browser: {result.ErrorMessage}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        IsProcessing = false;
        NotifyCommands();
    }

    private async Task ReRegisterAsync()
    {
        IsProcessing = true;
        NotifyCommands();
        var result = await _service.ReRegisterBrowserAsync(Id, CancellationToken.None);
        if (result.IsSuccess)
        {
            MessageBox.Show($"{Name} re-registered successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show($"Failed to re-register browser: {result.ErrorMessage}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        IsProcessing = false;
        NotifyCommands();
    }
}

public partial class BrowserRepairViewModel : ObservableObject
{
    private readonly IBrowserRepairService _service;

    public ObservableCollection<BrowserViewModel> Browsers { get; } = new();

    [ObservableProperty]
    private bool _isLoading;

    public BrowserRepairViewModel(IBrowserRepairService service)
    {
        _service = service;
        LoadBrowsersCommand = new AsyncRelayCommand(LoadBrowsersAsync);
    }

    public IAsyncRelayCommand LoadBrowsersCommand { get; }

    private async Task LoadBrowsersAsync()
    {
        IsLoading = true;
        var result = await _service.GetBrowsersAsync(CancellationToken.None);
        if (result.IsSuccess)
        {
            Browsers.Clear();
            foreach (var b in result.Value)
            {
                Browsers.Add(new BrowserViewModel(b, _service));
            }
        }
        IsLoading = false;
    }
}

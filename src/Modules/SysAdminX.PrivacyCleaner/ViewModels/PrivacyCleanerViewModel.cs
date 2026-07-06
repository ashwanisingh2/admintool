using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.PrivacyCleaner.ViewModels;

public partial class PrivacyCategoryViewModel : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;

    [ObservableProperty]
    private long _estimatedSizeBytes;

    [ObservableProperty]
    private bool _isSelected = true;

    [ObservableProperty]
    private bool _isScanning;
    
    [ObservableProperty]
    private bool _isCleaning;
    
    [ObservableProperty]
    private bool _isCleaned;

    public string SizeString => IsCleaned ? "Cleaned" : (EstimatedSizeBytes > 0 ? $"{EstimatedSizeBytes / 1024.0 / 1024.0:F2} MB" : "0 MB");
    
    partial void OnEstimatedSizeBytesChanged(long value)
    {
        OnPropertyChanged(nameof(SizeString));
    }
    partial void OnIsCleanedChanged(bool value)
    {
        OnPropertyChanged(nameof(SizeString));
    }
}

public partial class PrivacyCleanerViewModel : ObservableObject
{
    private readonly IPrivacyCleanerService _service;

    public ObservableCollection<PrivacyCategoryViewModel> Categories { get; } = new();

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private bool _isCleaning;

    public PrivacyCleanerViewModel(IPrivacyCleanerService service)
    {
        _service = service;
        
        InitializeCategories();

        ScanCommand = new AsyncRelayCommand(ScanAsync, () => !IsScanning && !IsCleaning);
        CleanSelectedCommand = new AsyncRelayCommand(CleanSelectedAsync, () => !IsScanning && !IsCleaning && Categories.Any(c => c.IsSelected));
        SelectAllCommand = new RelayCommand(SelectAll);
        SelectNoneCommand = new RelayCommand(SelectNone);
        
        foreach(var c in Categories)
        {
            c.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(PrivacyCategoryViewModel.IsSelected))
                    CleanSelectedCommand.NotifyCanExecuteChanged();
            };
        }
    }

    public IAsyncRelayCommand ScanCommand { get; }
    public IAsyncRelayCommand CleanSelectedCommand { get; }
    public IRelayCommand SelectAllCommand { get; }
    public IRelayCommand SelectNoneCommand { get; }

    private void InitializeCategories()
    {
        Categories.Add(new PrivacyCategoryViewModel { Id = "browserHistory", Name = "Browser History", Description = "Chrome, Edge, Firefox history files", Icon = "History24" });
        Categories.Add(new PrivacyCategoryViewModel { Id = "cookies", Name = "Cookies & Site Data", Description = "Chrome, Edge cookies databases", Icon = "Cookies24" });
        Categories.Add(new PrivacyCategoryViewModel { Id = "dns", Name = "DNS Cache", Description = "Windows DNS Resolver cache", Icon = "Earth24" });
        Categories.Add(new PrivacyCategoryViewModel { Id = "thumbnail", Name = "Thumbnail Cache", Description = "Windows Explorer thumbnail cache", Icon = "Image24" });
        Categories.Add(new PrivacyCategoryViewModel { Id = "recentDocs", Name = "Recent Documents", Description = "Windows Recent Items", Icon = "Document24" });
        Categories.Add(new PrivacyCategoryViewModel { Id = "clipboard", Name = "Clipboard History", Description = "Windows Clipboard cache", Icon = "Clipboard24" });
        Categories.Add(new PrivacyCategoryViewModel { Id = "telemetry", Name = "Windows Telemetry", Description = "Diagnostic data", Icon = "DataUsage24" });
        Categories.Add(new PrivacyCategoryViewModel { Id = "recycleBin", Name = "Recycle Bin", Description = "Deleted files", Icon = "Delete24" });
    }

    private async Task ScanAsync()
    {
        IsScanning = true;
        ScanCommand.NotifyCanExecuteChanged();
        CleanSelectedCommand.NotifyCanExecuteChanged();

        var tasks = Categories.Select(async c =>
        {
            c.IsScanning = true;
            c.IsCleaned = false;
            var result = await _service.ScanCategoryAsync(c.Id, CancellationToken.None);
            if (result.IsSuccess)
            {
                c.EstimatedSizeBytes = result.Value;
            }
            c.IsScanning = false;
        });

        await Task.WhenAll(tasks);

        IsScanning = false;
        ScanCommand.NotifyCanExecuteChanged();
        CleanSelectedCommand.NotifyCanExecuteChanged();
    }

    private async Task CleanSelectedAsync()
    {
        var toClean = Categories.Where(c => c.IsSelected && !c.IsCleaned).ToList();
        if (!toClean.Any()) return;

        var result = MessageBox.Show($"Are you sure you want to clean {toClean.Count} categories?", "Confirm Clean", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        IsCleaning = true;
        ScanCommand.NotifyCanExecuteChanged();
        CleanSelectedCommand.NotifyCanExecuteChanged();

        var tasks = toClean.Select(async c =>
        {
            c.IsCleaning = true;
            var cleanResult = await _service.CleanCategoryAsync(c.Id, CancellationToken.None);
            if (cleanResult.IsSuccess)
            {
                c.EstimatedSizeBytes = 0;
                c.IsCleaned = true;
                c.IsSelected = false;
            }
            c.IsCleaning = false;
        });

        await Task.WhenAll(tasks);

        IsCleaning = false;
        ScanCommand.NotifyCanExecuteChanged();
        CleanSelectedCommand.NotifyCanExecuteChanged();
        
        MessageBox.Show("Selected categories have been cleaned.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void SelectAll()
    {
        foreach (var c in Categories) c.IsSelected = true;
    }

    private void SelectNone()
    {
        foreach (var c in Categories) c.IsSelected = false;
    }
}

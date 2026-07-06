using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SysAdminX.StartupManager.Models;
using SysAdminX.StartupManager.Services;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace SysAdminX.StartupManager.ViewModels;

public partial class StartupManagerViewModel : ObservableObject
{
    private readonly IStartupManagerService _startupManagerService;
    private readonly ISnackbarService _snackbarService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ObservableCollection<StartupAppModel> _startupApps = new();

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    public ICollectionView StartupAppsView { get; }

    public StartupManagerViewModel(IStartupManagerService startupManagerService, ISnackbarService snackbarService)
    {
        _startupManagerService = startupManagerService;
        _snackbarService = snackbarService;

        StartupAppsView = CollectionViewSource.GetDefaultView(StartupApps);
        StartupAppsView.Filter = FilterStartupApps;
    }

    private bool FilterStartupApps(object obj)
    {
        if (obj is not StartupAppModel app) return false;
        if (string.IsNullOrWhiteSpace(SearchQuery)) return true;
        
        return app.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
               app.Command.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
               app.Source.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase);
    }

    partial void OnSearchQueryChanged(string value)
    {
        StartupAppsView.Refresh();
    }

    [RelayCommand]
    private async Task LoadStartupAppsAsync()
    {
        IsLoading = true;
        try
        {
            var result = await _startupManagerService.GetStartupAppsAsync();
            if (result.IsSuccess && result.Value != null)
            {
                foreach (var app in StartupApps)
                {
                    app.PropertyChanged -= App_PropertyChanged;
                }
                StartupApps.Clear();
                foreach (var app in result.Value.OrderBy(a => a.Name))
                {
                    app.PropertyChanged += App_PropertyChanged;
                    StartupApps.Add(app);
                }
            }
            else
            {
                _snackbarService.Show("Error", result.ErrorMessage ?? "Failed to load startup apps", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), TimeSpan.FromSeconds(3));
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async void App_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(StartupAppModel.IsEnabled) && sender is StartupAppModel app)
        {
            var result = await _startupManagerService.ToggleStartupAppAsync(app);
            if (!result.IsSuccess)
            {
                app.PropertyChanged -= App_PropertyChanged;
                app.IsEnabled = !app.IsEnabled;
                app.PropertyChanged += App_PropertyChanged;
                
                _snackbarService.Show("Error", result.ErrorMessage ?? "Failed to toggle app status", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), TimeSpan.FromSeconds(3));
            }
        }
    }

    [RelayCommand]
    private void OpenStartupFolder()
    {
        try
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _snackbarService.Show("Error", ex.Message, ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), TimeSpan.FromSeconds(3));
        }
    }
}

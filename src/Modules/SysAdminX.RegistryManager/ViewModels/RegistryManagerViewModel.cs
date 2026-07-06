using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SysAdminX.RegistryManager.Models;
using SysAdminX.RegistryManager.Services;

namespace SysAdminX.RegistryManager.ViewModels;

public partial class RegistryManagerViewModel : ObservableObject
{
    private readonly IRegistryManagerService _registryManagerService;

    [ObservableProperty]
    private ObservableCollection<RegistryBackupModel> _backups = new();

    [ObservableProperty]
    private string _newBackupLabel = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public RegistryManagerViewModel(IRegistryManagerService registryManagerService)
    {
        _registryManagerService = registryManagerService;
        _ = LoadBackupsAsync();
    }

    [RelayCommand]
    private async Task LoadBackupsAsync()
    {
        IsBusy = true;
        StatusMessage = "Loading backups...";
        
        var result = await _registryManagerService.GetBackupsAsync();
        if (result.IsSuccess)
        {
            Backups = new ObservableCollection<RegistryBackupModel>(result.Value);
            StatusMessage = "";
        }
        else
        {
            StatusMessage = $"Failed to load backups: {result.ErrorMessage}";
        }
        
        IsBusy = false;
    }

    [RelayCommand]
    private async Task CreateBackupAsync()
    {
        IsBusy = true;
        StatusMessage = "Creating backup...";
        
        var result = await _registryManagerService.CreateBackupAsync(NewBackupLabel);
        if (result.IsSuccess)
        {
            StatusMessage = "Backup created successfully.";
            NewBackupLabel = string.Empty;
            await LoadBackupsAsync();
        }
        else
        {
            StatusMessage = $"Backup failed: {result.ErrorMessage}";
        }
        
        IsBusy = false;
    }

    [RelayCommand]
    private async Task RestoreBackupAsync(RegistryBackupModel backup)
    {
        if (backup == null) return;
        
        IsBusy = true;
        StatusMessage = "Restoring HKLM...";
        var resultHklm = await _registryManagerService.RestoreBackupAsync(backup.HklmFilePath);
        
        StatusMessage = "Restoring HKCU...";
        var resultHkcu = await _registryManagerService.RestoreBackupAsync(backup.HkcuFilePath);
        
        if (resultHklm.IsSuccess && resultHkcu.IsSuccess)
        {
            StatusMessage = "Restore completed successfully.";
        }
        else
        {
            StatusMessage = "Restore encountered errors. Check logs.";
        }
        
        IsBusy = false;
    }

    [RelayCommand]
    private async Task DeleteBackupAsync(RegistryBackupModel backup)
    {
        if (backup == null) return;
        
        IsBusy = true;
        await _registryManagerService.DeleteBackupAsync(backup);
        await LoadBackupsAsync();
    }

    [RelayCommand]
    private void OpenBackupFolder()
    {
        _registryManagerService.OpenBackupFolder();
    }
}

using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.RegistryManager.Models;
using SysAdminX.RegistryManager.Services;

namespace SysAdminX.RegistryManager.ViewModels;

/// <summary>
/// ViewModel for the Registry Manager module.
///
/// Improvements applied:
///   - All async commands now wrapped in try/finally so an exception can no
///     longer leave <see cref="IsBusy"/> stuck on.
///   - Real cancellation token propagation.
///   - Toast notifications on every create / restore / delete outcome.
///   - Constructor no longer fires off a fire-and-forget load — the view's
///     Loaded handler should call LoadBackupsCommand instead.
/// </summary>
public partial class RegistryManagerViewModel : ObservableObject
{
    private readonly IRegistryManagerService _registryManagerService;
    private readonly IToastNotificationService _toastService;
    private readonly ILogger<RegistryManagerViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<RegistryBackupModel> _backups = new();

    [ObservableProperty]
    private string _newBackupLabel = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public RegistryManagerViewModel(
        IRegistryManagerService registryManagerService,
        IToastNotificationService toastService,
        ILogger<RegistryManagerViewModel> logger)
    {
        _registryManagerService = registryManagerService ?? throw new ArgumentNullException(nameof(registryManagerService));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [RelayCommand]
    private async Task LoadBackupsAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        StatusMessage = "Loading backups...";

        try
        {
            var result = await _registryManagerService.GetBackupsAsync(ct);
            if (result.IsSuccess && result.Value != null)
            {
                Backups = new ObservableCollection<RegistryBackupModel>(result.Value);
                StatusMessage = "";
            }
            else
            {
                StatusMessage = $"Failed to load backups: {result.ErrorMessage}";
                _toastService.ShowError("Failed to load backups", result.ErrorMessage ?? "Unknown error.");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Load backups cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Load backups threw an exception.");
            StatusMessage = ex.Message;
            _toastService.ShowError("Failed to load backups", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CreateBackupAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(NewBackupLabel))
        {
            _toastService.ShowWarning("Cannot create backup", "Please enter a label first.");
            return;
        }

        IsBusy = true;
        StatusMessage = "Creating backup...";

        try
        {
            var result = await _registryManagerService.CreateBackupAsync(NewBackupLabel, ct);
            if (result.IsSuccess)
            {
                StatusMessage = "Backup created successfully.";
                _toastService.ShowSuccess("Registry backup created", NewBackupLabel);
                NewBackupLabel = string.Empty;
                await LoadBackupsAsync(ct);
            }
            else
            {
                StatusMessage = $"Backup failed: {result.ErrorMessage}";
                _toastService.ShowError("Registry backup failed", result.ErrorMessage ?? "Unknown error.");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Create backup cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create backup threw an exception.");
            StatusMessage = ex.Message;
            _toastService.ShowError("Registry backup failed", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RestoreBackupAsync(RegistryBackupModel? backup, CancellationToken ct = default)
    {
        if (backup == null) return;

        IsBusy = true;
        StatusMessage = "Restoring HKLM...";

        try
        {
            var resultHklm = await _registryManagerService.RestoreBackupAsync(backup.HklmFilePath, ct);

            StatusMessage = "Restoring HKCU...";
            var resultHkcu = await _registryManagerService.RestoreBackupAsync(backup.HkcuFilePath, ct);

            if (resultHklm.IsSuccess && resultHkcu.IsSuccess)
            {
                StatusMessage = "Restore completed successfully.";
                _toastService.ShowSuccess("Registry restored", $"Backup '{backup.Label}' was restored.");
            }
            else
            {
                var err = resultHklm.ErrorMessage ?? resultHkcu.ErrorMessage ?? "Unknown error.";
                StatusMessage = "Restore encountered errors. Check logs.";
                _toastService.ShowError("Registry restore failed", err);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Restore backup cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Restore backup threw an exception.");
            StatusMessage = ex.Message;
            _toastService.ShowError("Registry restore failed", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteBackupAsync(RegistryBackupModel? backup, CancellationToken ct = default)
    {
        if (backup == null) return;

        IsBusy = true;
        try
        {
            await _registryManagerService.DeleteBackupAsync(backup, ct);
            _toastService.ShowSuccess("Backup deleted", $"Backup '{backup.Label}' was deleted.");
            await LoadBackupsAsync(ct);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Delete backup cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete backup threw an exception.");
            _toastService.ShowError("Delete failed", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void OpenBackupFolder()
    {
        try
        {
            _registryManagerService.OpenBackupFolder();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open backup folder.");
            _toastService.ShowError("Cannot open folder", ex.Message);
        }
    }
}

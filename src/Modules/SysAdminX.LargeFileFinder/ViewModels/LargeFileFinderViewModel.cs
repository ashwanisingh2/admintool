using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using SysAdminX.Core.Interfaces;
using SysAdminX.LargeFileFinder.Models;
using SysAdminX.LargeFileFinder.Services;

namespace SysAdminX.LargeFileFinder.ViewModels;

/// <summary>
/// ViewModel for the Large File Finder module.
///
/// Improvements applied:
///   - All async commands wrapped in try/finally so an exception can no
///     longer leave IsScanning stuck on.
///   - DeleteSelectedAsync now shows a destructive confirmation dialog
///     (was previously deleting with no confirmation).
///   - Per-file failures during delete / move are logged instead of
///     silently swallowed.
///   - IToastNotificationService injected for outcome feedback.
/// </summary>
public partial class LargeFileFinderViewModel : ObservableObject
{
    private readonly ILargeFileFinderService _largeFileFinderService;
    private readonly IToastNotificationService _toastService;
    private readonly ILogger<LargeFileFinderViewModel> _logger;
    private CancellationTokenSource? _scanCts;

    [ObservableProperty]
    private ObservableCollection<LargeFileModel> _files = new();

    [ObservableProperty]
    private ObservableCollection<string> _drives = new();

    [ObservableProperty]
    private string _selectedDrive = "C:\\";

    [ObservableProperty]
    private int _minSizeMB = 100;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private string _statusText = "Ready";

    public LargeFileFinderViewModel(
        ILargeFileFinderService largeFileFinderService,
        IToastNotificationService toastService,
        ILogger<LargeFileFinderViewModel> logger)
    {
        _largeFileFinderService = largeFileFinderService ?? throw new ArgumentNullException(nameof(largeFileFinderService));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        LoadDrives();
    }

    private void LoadDrives()
    {
        Drives.Clear();
        try
        {
            foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
            {
                Drives.Add(drive.Name);
            }
            if (Drives.Any())
            {
                SelectedDrive = Drives.First();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enumerate drives.");
            _toastService.ShowError("Failed to enumerate drives", ex.Message);
        }
    }

    [RelayCommand]
    private async Task ScanAsync(CancellationToken ct)
    {
        if (IsScanning) return;

        // Cancel any previous scan that might still be running.
        _scanCts?.Cancel();
        _scanCts?.Dispose();
        _scanCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        IsScanning = true;
        StatusText = "Scanning...";
        Files.Clear();

        try
        {
            var result = await _largeFileFinderService.ScanFilesAsync(
                SelectedDrive,
                MinSizeMB,
                status => StatusText = status,
                _scanCts.Token);

            if (result.IsSuccess && result.Value != null)
            {
                foreach (var file in result.Value)
                {
                    Files.Add(file);
                }
                StatusText = $"Found {Files.Count} files.";
                _toastService.ShowSuccess("Scan complete", $"Found {Files.Count} large files on {SelectedDrive}.");
            }
            else
            {
                StatusText = $"Scan failed: {result.ErrorMessage}";
                _toastService.ShowError("Scan failed", result.ErrorMessage ?? "Unknown error.");
            }
        }
        catch (OperationCanceledException)
        {
            StatusText = "Scan cancelled.";
            _logger.LogInformation("Large file scan cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scan threw an exception.");
            StatusText = $"Scan threw an exception: {ex.Message}";
            _toastService.ShowError("Scan failed", ex.Message);
        }
        finally
        {
            IsScanning = false;
        }
    }

    /// <summary>Cancel any in-flight scan.</summary>
    [RelayCommand]
    private void CancelScan()
    {
        try { _scanCts?.Cancel(); } catch (ObjectDisposedException) { /* ignore */ }
    }

    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        var selectedFiles = Files.Where(f => f.IsSelected).ToList();
        if (!selectedFiles.Any())
        {
            _toastService.ShowWarning("No files selected", "Select one or more files to delete first.");
            return;
        }

        // Destructive confirmation — kept as a MessageBox because we genuinely
        // need a yes/no answer before deleting potentially gigabytes of data.
        var totalBytes = selectedFiles.Sum(f => (long)f.SizeInBytes);
        var totalMb = totalBytes / 1024.0 / 1024.0;
        if (MessageBox.Show(
            $"Are you sure you want to permanently delete {selectedFiles.Count} files ({totalMb:F1} MB)?\n\nThis cannot be undone.",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        StatusText = $"Deleting {selectedFiles.Count} files...";

        int deletedCount = 0;
        foreach (var file in selectedFiles)
        {
            try
            {
                var result = await _largeFileFinderService.DeleteFileAsync(file.FilePath);
                if (result.IsSuccess)
                {
                    Files.Remove(file);
                    deletedCount++;
                }
                else
                {
                    _logger.LogWarning("Failed to delete {Path}: {Error}", file.FilePath, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Delete threw an exception for {Path}", file.FilePath);
            }
        }

        StatusText = $"Deleted {deletedCount} of {selectedFiles.Count} files.";
        _toastService.ShowSuccess("Delete complete",
            $"Deleted {deletedCount} of {selectedFiles.Count} files.");
    }

    [RelayCommand]
    private async Task MoveSelectedAsync()
    {
        var selectedFiles = Files.Where(f => f.IsSelected).ToList();
        if (!selectedFiles.Any())
        {
            _toastService.ShowWarning("No files selected", "Select one or more files to move first.");
            return;
        }

        var dialog = new OpenFolderDialog
        {
            Title = "Select Backup Folder"
        };

        if (dialog.ShowDialog() != true) return;

        string backupFolder = dialog.FolderName;
        StatusText = $"Moving {selectedFiles.Count} files to {backupFolder}...";

        int movedCount = 0;
        foreach (var file in selectedFiles)
        {
            try
            {
                string destFile = Path.Combine(backupFolder, Path.GetFileName(file.FilePath));
                var result = await _largeFileFinderService.MoveFileAsync(file.FilePath, destFile);
                if (result.IsSuccess)
                {
                    Files.Remove(file);
                    movedCount++;
                }
                else
                {
                    _logger.LogWarning("Failed to move {Path}: {Error}", file.FilePath, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Move threw an exception for {Path}", file.FilePath);
            }
        }

        StatusText = $"Moved {movedCount} of {selectedFiles.Count} files.";
        _toastService.ShowSuccess("Move complete",
            $"Moved {movedCount} of {selectedFiles.Count} files to {backupFolder}.");
    }
}

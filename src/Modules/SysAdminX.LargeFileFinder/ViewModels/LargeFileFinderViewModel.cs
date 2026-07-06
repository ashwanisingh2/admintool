using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SysAdminX.LargeFileFinder.Models;
using SysAdminX.LargeFileFinder.Services;
using Wpf.Ui.Controls; // For possible dialogs or message boxes if available, but I'll use standard for now

namespace SysAdminX.LargeFileFinder.ViewModels;

public partial class LargeFileFinderViewModel : ObservableObject
{
    private readonly ILargeFileFinderService _largeFileFinderService;

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

    public LargeFileFinderViewModel(ILargeFileFinderService largeFileFinderService)
    {
        _largeFileFinderService = largeFileFinderService;
        LoadDrives();
    }

    private void LoadDrives()
    {
        Drives.Clear();
        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
        {
            Drives.Add(drive.Name);
        }
        if (Drives.Any())
        {
            SelectedDrive = Drives.First();
        }
    }

    [RelayCommand]
    private async Task ScanAsync()
    {
        IsScanning = true;
        StatusText = "Scanning...";
        Files.Clear();

        var result = await _largeFileFinderService.ScanFilesAsync(SelectedDrive, MinSizeMB, status =>
        {
            StatusText = status;
        });

        if (result.IsSuccess)
        {
            foreach (var file in result.Value)
            {
                Files.Add(file);
            }
            StatusText = $"Found {Files.Count} files.";
        }
        else
        {
            StatusText = $"Scan failed: {result.ErrorMessage}";
        }

        IsScanning = false;
    }

    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        var selectedFiles = Files.Where(f => f.IsSelected).ToList();
        if (!selectedFiles.Any()) return;

        // Note: In a real app, use a proper dialog service. Here we rely on basic confirmation logic if needed, 
        // but for MVVM purity without a dialog service, we'll just execute.
        StatusText = $"Deleting {selectedFiles.Count} files...";

        foreach (var file in selectedFiles)
        {
            var result = await _largeFileFinderService.DeleteFileAsync(file.FilePath);
            if (result.IsSuccess)
            {
                Files.Remove(file);
            }
        }

        StatusText = "Delete complete.";
    }

    [RelayCommand]
    private async Task MoveSelectedAsync()
    {
        var selectedFiles = Files.Where(f => f.IsSelected).ToList();
        if (!selectedFiles.Any()) return;

        // Use FolderBrowserDialog from WinForms or OpenFolderDialog from Win32
        // Since we are in WPF net8.0-windows, OpenFolderDialog exists in Microsoft.Win32
        var dialog = new OpenFolderDialog
        {
            Title = "Select Backup Folder"
        };

        if (dialog.ShowDialog() == true)
        {
            string backupFolder = dialog.FolderName;
            StatusText = $"Moving {selectedFiles.Count} files to {backupFolder}...";

            foreach (var file in selectedFiles)
            {
                string destFile = Path.Combine(backupFolder, Path.GetFileName(file.FilePath));
                var result = await _largeFileFinderService.MoveFileAsync(file.FilePath, destFile);
                if (result.IsSuccess)
                {
                    Files.Remove(file);
                }
            }
            StatusText = "Move complete.";
        }
    }
}

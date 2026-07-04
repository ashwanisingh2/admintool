using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SysAdminX.LargeFileFinder.Models;

public partial class LargeFileModel : ObservableObject
{
    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private long _sizeInBytes;

    [ObservableProperty]
    private DateTime _lastModified;

    [ObservableProperty]
    private string _extension = string.Empty;

    [ObservableProperty]
    private bool _isSelected;

    public string SizeDisplay => SizeInBytes < 1024 * 1024 ? $"{SizeInBytes / 1024.0:F2} KB" : 
                                 SizeInBytes < 1024 * 1024 * 1024 ? $"{SizeInBytes / (1024.0 * 1024.0):F2} MB" : 
                                 $"{SizeInBytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
}

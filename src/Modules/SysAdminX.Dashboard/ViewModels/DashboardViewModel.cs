// -----------------------------------------------------------------------
// <copyright file="DashboardViewModel.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.Dashboard.ViewModels;

/// <summary>
/// ViewModel for the Dashboard page.
/// Manages real-time system health monitoring with live charts.
/// </summary>
public partial class DashboardViewModel : ObservableObject, IDisposable
{
    private readonly ILogger<DashboardViewModel> _logger;
    private readonly ISystemHealthService _healthService;
    private CancellationTokenSource? _monitoringCts;
    private bool _disposed;

    private DispatcherTimer? _liveTimer;
    private PerformanceCounter? _cpuCounter;
    private PerformanceCounter? _ramCounter;
    private ulong _totalRamMb;

    private const int MAX_CHART_POINTS = 60;
    private const int MONITORING_INTERVAL_MS = 1500;

    #region Observable Properties

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    // CPU
    [ObservableProperty]
    private double _cpuUsage;

    [ObservableProperty]
    private string _cpuName = "Loading...";

    [ObservableProperty]
    private string _cpuCoresInfo = "";

    [ObservableProperty]
    private string _cpuSpeedInfo = "";

    // RAM
    [ObservableProperty]
    private double _ramUsage;

    [ObservableProperty]
    private string _ramUsedText = "";

    [ObservableProperty]
    private string _ramTotalText = "";

    // Disk
    [ObservableProperty]
    private double _diskUsage;

    [ObservableProperty]
    private string _diskUsedText = "";

    [ObservableProperty]
    private string _diskTotalText = "";

    [ObservableProperty]
    private string _diskDriveLetter = "C:";

    // Network
    [ObservableProperty]
    private string _networkSentText = "0 B/s";

    [ObservableProperty]
    private string _networkReceivedText = "0 B/s";

    // System Info
    [ObservableProperty]
    private string _windowsEdition = "Loading...";

    [ObservableProperty]
    private string _windowsVersion = "";

    [ObservableProperty]
    private string _windowsBuild = "";

    [ObservableProperty]
    private string _computerName = "";

    [ObservableProperty]
    private string _domainWorkgroup = "";

    [ObservableProperty]
    private string _currentUser = "";

    [ObservableProperty]
    private string _uptimeText = "Calculating...";

    [ObservableProperty]
    private string _architectureText = "";

    // Disk drives list
    public ObservableCollection<DiskInfoModel> DiskDrives { get; } = new();

    #endregion

    #region Chart Data

    private readonly ObservableCollection<ObservableValue> _cpuValues = new();
    private readonly ObservableCollection<ObservableValue> _ramValues = new();

    /// <summary>
    /// Gets the CPU usage chart series.
    /// </summary>
    public ISeries[] CpuSeries { get; }

    /// <summary>
    /// Gets the RAM usage chart series.
    /// </summary>
    public ISeries[] RamSeries { get; }

    /// <summary>
    /// Gets the X-axis configuration for charts.
    /// </summary>
    public Axis[] XAxes { get; } =
    {
        new Axis
        {
            IsVisible = false,
            ShowSeparatorLines = false
        }
    };

    /// <summary>
    /// Gets the Y-axis configuration for charts (0-100%).
    /// </summary>
    public Axis[] YAxes { get; } =
    {
        new Axis
        {
            MinLimit = 0,
            MaxLimit = 100,
            IsVisible = false,
            ShowSeparatorLines = false
        }
    };

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardViewModel"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="healthService">The system health monitoring service.</param>
    public DashboardViewModel(ILogger<DashboardViewModel> logger, ISystemHealthService healthService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _healthService = healthService ?? throw new ArgumentNullException(nameof(healthService));

        // Initialize chart data points
        for (int i = 0; i < MAX_CHART_POINTS; i++)
        {
            _cpuValues.Add(new ObservableValue(0));
            _ramValues.Add(new ObservableValue(0));
        }

        CpuSeries = new ISeries[]
        {
            new LineSeries<ObservableValue>
            {
                Values = _cpuValues,
                Fill = new LinearGradientPaint(new[] { new SKColor(0, 120, 212, 100), new SKColor(0, 120, 212, 5) }, new SKPoint(0.5f, 0f), new SKPoint(0.5f, 1f)),
                Stroke = new SolidColorPaint(new SKColor(0, 120, 212), 3),
                GeometrySize = 0,
                GeometryStroke = null,
                LineSmoothness = 0.65,
                AnimationsSpeed = TimeSpan.FromMilliseconds(500),
                IsHoverable = false
            }
        };

        RamSeries = new ISeries[]
        {
            new LineSeries<ObservableValue>
            {
                Values = _ramValues,
                Fill = new LinearGradientPaint(new[] { new SKColor(139, 92, 246, 100), new SKColor(139, 92, 246, 5) }, new SKPoint(0.5f, 0f), new SKPoint(0.5f, 1f)),
                Stroke = new SolidColorPaint(new SKColor(139, 92, 246), 3),
                GeometrySize = 0,
                GeometryStroke = null,
                LineSmoothness = 0.65,
                AnimationsSpeed = TimeSpan.FromMilliseconds(500),
                IsHoverable = false
            }
        };
    }

    /// <summary>
    /// Initializes the dashboard and starts monitoring.
    /// Called when the page is loaded.
    /// </summary>
    [RelayCommand]
    public async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            HasError = false;
            _logger.LogInformation("Initializing Dashboard");

            // Load Windows info (one-time)
            await LoadWindowsInfoAsync();

            // Load disk info
            await LoadDiskInfoAsync();

            // Start real-time monitoring
            await StartMonitoringAsync();

            IsLoading = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Dashboard");
            HasError = true;
            ErrorMessage = $"Failed to load dashboard: {ex.Message}";
            IsLoading = false;
        }
    }

    /// <summary>
    /// Refreshes all dashboard data.
    /// </summary>
    [RelayCommand]
    public async Task RefreshAsync()
    {
        _logger.LogInformation("Refreshing Dashboard data");
        StopMonitoring();
        await InitializeAsync();
    }

    private async Task LoadWindowsInfoAsync()
    {
        var result = await _healthService.GetWindowsInfoAsync();
        if (result.IsSuccess && result.Value != null)
        {
            var info = result.Value;
            WindowsEdition = info.Edition;
            WindowsVersion = info.Version;
            WindowsBuild = info.BuildNumber;
            ComputerName = info.ComputerName;
            DomainWorkgroup = info.DomainOrWorkgroup;
            CurrentUser = info.CurrentUser;
            ArchitectureText = info.Architecture;
        }
    }

    private async Task LoadDiskInfoAsync()
    {
        var result = await _healthService.GetDiskInfoAsync();
        if (result.IsSuccess && result.Value != null)
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                DiskDrives.Clear();
                foreach (var disk in result.Value)
                {
                    DiskDrives.Add(disk);
                }
            });
        }
    }

    private long _previousBytesSent;
    private long _previousBytesReceived;
    private bool _firstNetworkReading = true;

    private async Task StartMonitoringAsync()
    {
        _monitoringCts?.Cancel();
        _monitoringCts = new CancellationTokenSource();
        var ct = _monitoringCts.Token;

        if (_liveTimer == null)
        {
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
                _cpuCounter.NextValue(); // Prime the CPU counter
                
                _totalRamMb = (ulong)(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1048576);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize performance counters for live graphs.");
            }

            _liveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _liveTimer.Tick += LiveTimer_Tick;
            _liveTimer.Start();
        }

        await _healthService.StartMonitoringAsync(MONITORING_INTERVAL_MS, health =>
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                UpdateHealthDisplay(health);
            });
        }, ct);
    }

    private void LiveTimer_Tick(object? sender, EventArgs e)
    {
        if (_cpuCounter != null)
        {
            try
            {
                var cpuVal = Math.Round(_cpuCounter.NextValue(), 1);
                CpuUsage = cpuVal;
                _cpuValues.RemoveAt(0);
                _cpuValues.Add(new ObservableValue(cpuVal));
            }
            catch { }
        }

        if (_ramCounter != null && _totalRamMb > 0)
        {
            try
            {
                var availableMb = _ramCounter.NextValue();
                var usedMb = _totalRamMb - availableMb;
                if (usedMb < 0) usedMb = 0;
                var ramVal = Math.Round((usedMb / (double)_totalRamMb) * 100.0, 1);
                
                RamUsage = ramVal;
                RamUsedText = FormatBytes((ulong)(usedMb * 1048576));
                RamTotalText = FormatBytes((ulong)(_totalRamMb * 1048576));

                _ramValues.RemoveAt(0);
                _ramValues.Add(new ObservableValue(ramVal));
            }
            catch { }
        }
    }

    private void UpdateHealthDisplay(SystemHealthModel health)
    {
        // CPU Name (static info)
        CpuName = health.CpuName;
        CpuCoresInfo = $"{health.CpuCores} Cores / {health.CpuThreads} Threads";
        CpuSpeedInfo = $"{health.CpuSpeedMhz / 1000.0:F2} GHz";

        // Disk
        DiskUsage = health.DiskUsagePercent;
        DiskUsedText = FormatBytes(health.UsedDiskBytes);
        DiskTotalText = FormatBytes(health.TotalDiskBytes);
        DiskDriveLetter = health.SystemDrive;

        // Network (calculate delta for rate)
        if (_firstNetworkReading)
        {
            _previousBytesSent = health.NetworkBytesSentPerSec;
            _previousBytesReceived = health.NetworkBytesReceivedPerSec;
            _firstNetworkReading = false;
            NetworkSentText = "0 B/s";
            NetworkReceivedText = "0 B/s";
        }
        else
        {
            var sentDelta = health.NetworkBytesSentPerSec - _previousBytesSent;
            var receivedDelta = health.NetworkBytesReceivedPerSec - _previousBytesReceived;
            var intervalSec = MONITORING_INTERVAL_MS / 1000.0;

            NetworkSentText = FormatBytesPerSecond(sentDelta > 0 ? (long)(sentDelta / intervalSec) : 0);
            NetworkReceivedText = FormatBytesPerSecond(receivedDelta > 0 ? (long)(receivedDelta / intervalSec) : 0);

            _previousBytesSent = health.NetworkBytesSentPerSec;
            _previousBytesReceived = health.NetworkBytesReceivedPerSec;
        }

        // Uptime
        UptimeText = FormatUptime(health.Uptime);
    }

    private void StopMonitoring()
    {
        _monitoringCts?.Cancel();
        _monitoringCts?.Dispose();
        _monitoringCts = null;

        _liveTimer?.Stop();
        _liveTimer = null;

        _cpuCounter?.Dispose();
        _cpuCounter = null;

        _ramCounter?.Dispose();
        _ramCounter = null;
    }

    #region Formatting Helpers

    /// <summary>
    /// Formats bytes into a human-readable string (GB, MB, KB).
    /// </summary>
    private static string FormatBytes(ulong bytes)
    {
        if (bytes >= 1_073_741_824)
            return $"{bytes / 1_073_741_824.0:F1} GB";
        if (bytes >= 1_048_576)
            return $"{bytes / 1_048_576.0:F1} MB";
        if (bytes >= 1024)
            return $"{bytes / 1024.0:F1} KB";
        return $"{bytes} B";
    }

    /// <summary>
    /// Formats bytes per second into a human-readable throughput string.
    /// </summary>
    private static string FormatBytesPerSecond(long bytesPerSec)
    {
        if (bytesPerSec >= 1_073_741_824)
            return $"{bytesPerSec / 1_073_741_824.0:F1} GB/s";
        if (bytesPerSec >= 1_048_576)
            return $"{bytesPerSec / 1_048_576.0:F1} MB/s";
        if (bytesPerSec >= 1024)
            return $"{bytesPerSec / 1024.0:F1} KB/s";
        return $"{bytesPerSec} B/s";
    }

    /// <summary>
    /// Formats a TimeSpan uptime into a readable string.
    /// </summary>
    private static string FormatUptime(TimeSpan uptime)
    {
        if (uptime.TotalDays >= 1)
            return $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";
        if (uptime.TotalHours >= 1)
            return $"{(int)uptime.TotalHours}h {uptime.Minutes}m {uptime.Seconds}s";
        return $"{uptime.Minutes}m {uptime.Seconds}s";
    }

    #endregion

    /// <summary>
    /// Disposes resources used by the ViewModel.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            StopMonitoring();
            _disposed = true;
        }
    }
}

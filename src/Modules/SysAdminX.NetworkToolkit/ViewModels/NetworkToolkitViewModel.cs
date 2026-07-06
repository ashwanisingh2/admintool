// -----------------------------------------------------------------------
// <copyright file="NetworkToolkitViewModel.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Models;
using SysAdminX.NetworkToolkit.Services;

namespace SysAdminX.NetworkToolkit.ViewModels;

/// <summary>
/// ViewModel for the Network Toolkit module.
/// </summary>
public partial class NetworkToolkitViewModel : ObservableObject
{
    private readonly ILogger<NetworkToolkitViewModel> _logger;
    private readonly INetworkService _networkService;
    
    private List<NetworkConnectionModel> _allConnections = new();
    private List<NetworkAdapterModel> _allAdapters = new();

    [ObservableProperty]
    private ObservableCollection<NetworkAdapterModel> _adapters = new();

    [ObservableProperty]
    private ObservableCollection<NetworkConnectionModel> _connections = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    public NetworkToolkitViewModel(ILogger<NetworkToolkitViewModel> logger, INetworkService networkService)
    {
        _logger = logger;
        _networkService = networkService;
    }

    [RelayCommand]
    public async Task InitializeAsync(CancellationToken ct)
    {
        if (IsLoading) return;

        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        Adapters.Clear();
        Connections.Clear();

        try
        {
            var tasks = new[]
            {
                LoadAdaptersAsync(ct),
                LoadConnectionsAsync(ct)
            };

            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Network toolkit init cancelled.");
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Network Toolkit");
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public Task RefreshAsync(CancellationToken ct)
    {
        return InitializeAsync(ct);
    }

    partial void OnSearchQueryChanged(string value)
    {
        FilterConnections();
    }

    private async Task LoadAdaptersAsync(CancellationToken ct)
    {
        var result = await _networkService.GetNetworkAdaptersAsync(ct);
        if (result.IsSuccess && result.Value != null)
        {
            _allAdapters = result.Value;
            Adapters = new ObservableCollection<NetworkAdapterModel>(_allAdapters);
        }
        else
        {
            HasError = true;
            ErrorMessage = result.ErrorMessage ?? "Failed to load adapters.";
        }
    }

    private async Task LoadConnectionsAsync(CancellationToken ct)
    {
        var result = await _networkService.GetActiveConnectionsAsync(ct);
        if (result.IsSuccess && result.Value != null)
        {
            _allConnections = result.Value;
            FilterConnections();
        }
        else
        {
            HasError = true;
            ErrorMessage += "\n" + (result.ErrorMessage ?? "Failed to load connections.");
        }
    }

    private void FilterConnections()
    {
        IEnumerable<NetworkConnectionModel> filtered = _allConnections;

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var q = SearchQuery.ToLowerInvariant();
            filtered = filtered.Where(c => 
                c.ProcessName.ToLowerInvariant().Contains(q) || 
                c.LocalAddress.Contains(q) ||
                c.RemoteAddress.Contains(q) ||
                c.Protocol.ToLowerInvariant().Contains(q));
        }

        Connections = new ObservableCollection<NetworkConnectionModel>(filtered);
    }

    [ObservableProperty]
    private string _scannerTarget = "127.0.0.1";

    [ObservableProperty]
    private int _scannerStartPort = 1;

    [ObservableProperty]
    private int _scannerEndPort = 1000;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartPortScanCommand))]
    private bool _isScanning;

    [ObservableProperty]
    private ObservableCollection<PortScanResultModel> _scanResults = new();

    private bool CanStartScan() => !IsScanning;

    [RelayCommand(CanExecute = nameof(CanStartScan))]
    public async Task StartPortScanAsync(CancellationToken ct)
    {
        IsScanning = true;
        ScanResults.Clear();

        try
        {
            var result = await _networkService.PortScanAsync(ScannerTarget, ScannerStartPort, ScannerEndPort, ct);
            if (result.IsSuccess && result.Value != null)
            {
                foreach (var r in result.Value)
                {
                    ScanResults.Add(r);
                }
            }
            else
            {
                HasError = true;
                ErrorMessage = result.ErrorMessage ?? "Port scan failed";
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Port scan cancelled.");
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Port scan threw an exception.");
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsScanning = false;
        }
    }

    [ObservableProperty]
    private string _wolMacAddress = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendWakeOnLanCommand))]
    private bool _isSendingWol;

    private bool CanSendWol() => !IsSendingWol && !string.IsNullOrWhiteSpace(WolMacAddress);

    [RelayCommand(CanExecute = nameof(CanSendWol))]
    public async Task SendWakeOnLanAsync(CancellationToken ct)
    {
        IsSendingWol = true;
        try
        {
            var result = await _networkService.WakeOnLanAsync(WolMacAddress, ct);
            if (!result.IsSuccess)
            {
                HasError = true;
                ErrorMessage = result.ErrorMessage ?? "Failed to send WOL packet.";
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("WOL send cancelled.");
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "WOL send threw an exception.");
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsSendingWol = false;
        }
    }

    [ObservableProperty]
    private string _pingSweepBaseIP = "192.168.1.0";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartPingSweepCommand))]
    private bool _isPingSweeping;

    [ObservableProperty]
    private ObservableCollection<PingSweepResultModel> _pingSweepResults = new();

    private bool CanStartPingSweep() => !IsPingSweeping && !string.IsNullOrWhiteSpace(PingSweepBaseIP);

    [RelayCommand(CanExecute = nameof(CanStartPingSweep))]
    public async Task StartPingSweepAsync(CancellationToken ct)
    {
        IsPingSweeping = true;
        PingSweepResults.Clear();

        try
        {
            var result = await _networkService.PingSweepAsync(PingSweepBaseIP, ct);
            if (result.IsSuccess && result.Value != null)
            {
                foreach (var r in result.Value)
                {
                    PingSweepResults.Add(r);
                }
            }
            else
            {
                HasError = true;
                ErrorMessage = result.ErrorMessage ?? "Ping sweep failed";
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Ping sweep cancelled.");
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Ping sweep threw an exception.");
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsPingSweeping = false;
        }
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScanWiFiCommand))]
    private bool _isScanningWiFi;

    [ObservableProperty]
    private ObservableCollection<WiFiNetworkModel> _wiFiNetworks = new();

    private bool CanScanWiFi() => !IsScanningWiFi;

    [RelayCommand(CanExecute = nameof(CanScanWiFi))]
    public async Task ScanWiFiAsync(CancellationToken ct)
    {
        IsScanningWiFi = true;
        WiFiNetworks.Clear();

        try
        {
            // Spawn netsh on a thread-pool thread so we don't block the UI.
            // Use a child CancellationTokenSource tied to the command's CT
            // so that navigation away from the page aborts the scan.
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            linkedCts.CancelAfter(TimeSpan.FromSeconds(15));

            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = "wlan show networks mode=bssid",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.Unicode
            };

            using var p = new System.Diagnostics.Process { StartInfo = startInfo };
            p.Start();

            string output = await p.StandardOutput.ReadToEndAsync(linkedCts.Token);
            await p.WaitForExitAsync(linkedCts.Token);

            var lines = output.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            WiFiNetworkModel? currentNetwork = null;

            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("SSID "))
                {
                    if (currentNetwork != null && !string.IsNullOrEmpty(currentNetwork.Ssid))
                    {
                        WiFiNetworks.Add(currentNetwork);
                    }
                    currentNetwork = new WiFiNetworkModel();
                    var parts = trimmed.Split(new[] { ':' }, 2);
                    if (parts.Length > 1) currentNetwork.Ssid = parts[1].Trim();
                }
                else if (currentNetwork != null)
                {
                    var parts = trimmed.Split(new[] { ':' }, 2);
                    if (parts.Length > 1)
                    {
                        string key = parts[0].Trim();
                        string val = parts[1].Trim();

                        if (key.StartsWith("Network type")) currentNetwork.NetworkType = val;
                        else if (key.StartsWith("Authentication")) currentNetwork.Authentication = val;
                        else if (key.StartsWith("Encryption")) currentNetwork.Encryption = val;
                        else if (key.StartsWith("BSSID"))
                        {
                            if (string.IsNullOrEmpty(currentNetwork.Bssid))
                                currentNetwork.Bssid = val;
                            else
                            {
                                // Handle multiple BSSIDs for the same SSID by duplicating the entry
                                WiFiNetworks.Add(currentNetwork);
                                var newNetwork = new WiFiNetworkModel
                                {
                                    Ssid = currentNetwork.Ssid,
                                    NetworkType = currentNetwork.NetworkType,
                                    Authentication = currentNetwork.Authentication,
                                    Encryption = currentNetwork.Encryption,
                                    Bssid = val
                                };
                                currentNetwork = newNetwork;
                            }
                        }
                        else if (key.StartsWith("Signal"))
                        {
                            if (int.TryParse(val.Replace("%", "").Trim(), out int sig))
                                currentNetwork.SignalStrength = sig;
                        }
                        else if (key.StartsWith("Channel")) currentNetwork.Channel = val;
                    }
                }
            }
            if (currentNetwork != null && !string.IsNullOrEmpty(currentNetwork.Ssid))
            {
                WiFiNetworks.Add(currentNetwork);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("WiFi scan was cancelled or timed out.");
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to scan WiFi");
            HasError = true;
            ErrorMessage = "WiFi scan failed: " + ex.Message;
        }
        finally
        {
            IsScanningWiFi = false;
        }
    }
}

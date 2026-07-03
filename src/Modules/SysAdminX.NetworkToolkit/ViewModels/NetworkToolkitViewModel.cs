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

        var tasks = new[]
        {
            LoadAdaptersAsync(ct),
            LoadConnectionsAsync(ct)
        };

        await Task.WhenAll(tasks);

        IsLoading = false;
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
    private string _scannerPorts = "21,22,80,443,3389";

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

        var target = ScannerTarget;
        var portsStr = ScannerPorts.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
        var ports = new List<int>();

        foreach (var p in portsStr)
        {
            if (p.Contains("-"))
            {
                var parts = p.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[0], out int start) && int.TryParse(parts[1], out int end))
                {
                    for (int i = start; i <= end; i++) ports.Add(i);
                }
            }
            else if (int.TryParse(p, out int port))
            {
                ports.Add(port);
            }
        }

        ports = ports.Take(1000).Distinct().ToList();

        var tasks = ports.Select(async port =>
        {
            var result = new PortScanResultModel { Port = port, Status = "Closed", Service = GetCommonServiceName(port) };
            try
            {
                using var client = new System.Net.Sockets.TcpClient();
                var connectTask = client.ConnectAsync(target, port);
                if (await Task.WhenAny(connectTask, Task.Delay(1000, ct)) == connectTask && client.Connected)
                {
                    result.Status = "Open";
                }
            }
            catch { }
            return result;
        });

        int batchSize = 100;
        for (int i = 0; i < ports.Count; i += batchSize)
        {
            var batch = tasks.Skip(i).Take(batchSize);
            var results = await Task.WhenAll(batch);
            foreach (var r in results)
            {
                ScanResults.Add(r);
            }
        }

        IsScanning = false;
    }

    private string GetCommonServiceName(int port)
    {
        return port switch
        {
            20 => "FTP Data",
            21 => "FTP Control",
            22 => "SSH",
            23 => "Telnet",
            25 => "SMTP",
            53 => "DNS",
            80 => "HTTP",
            110 => "POP3",
            135 => "RPC",
            139 => "NetBIOS",
            143 => "IMAP",
            443 => "HTTPS",
            445 => "SMB",
            1433 => "SQL Server",
            3306 => "MySQL",
            3389 => "RDP",
            8080 => "HTTP Alternate",
            _ => "Unknown"
        };
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
            var p = new System.Diagnostics.Process();
            p.StartInfo.FileName = "netsh";
            p.StartInfo.Arguments = "wlan show networks mode=bssid";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.CreateNoWindow = true;
            p.Start();

            string output = await p.StandardOutput.ReadToEndAsync(ct);
            await p.WaitForExitAsync(ct);

            var lines = output.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            WiFiNetworkModel currentNetwork = null;

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
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to scan WiFi");
        }

        IsScanningWiFi = false;
    }
}

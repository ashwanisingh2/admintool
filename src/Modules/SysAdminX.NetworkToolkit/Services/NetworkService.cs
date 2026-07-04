// -----------------------------------------------------------------------
// <copyright file="NetworkService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.NetworkToolkit.Services;

/// <summary>
/// Implementation of <see cref="INetworkService"/> using System.Net and netstat.
/// </summary>
public class NetworkService : INetworkService
{
    private readonly ILogger<NetworkService> _logger;
    private readonly IProcessExecutorService _processService;

    public NetworkService(ILogger<NetworkService> logger, IProcessExecutorService processService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _processService = processService ?? throw new ArgumentNullException(nameof(processService));
    }

    public Task<Result<List<NetworkAdapterModel>>> GetNetworkAdaptersAsync(CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            try
            {
                var adapters = new List<NetworkAdapterModel>();
                var interfaces = NetworkInterface.GetAllNetworkInterfaces();

                foreach (var iface in interfaces)
                {
                    var props = iface.GetIPProperties();
                    var ipv4 = props.UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork);
                    var ipv6 = props.UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetworkV6);
                    var gw = props.GatewayAddresses.FirstOrDefault()?.Address.ToString() ?? "";

                    adapters.Add(new NetworkAdapterModel
                    {
                        Id = iface.Id,
                        Name = iface.Name,
                        Description = iface.Description,
                        MacAddress = string.Join(":", iface.GetPhysicalAddress().GetAddressBytes().Select(b => b.ToString("X2"))),
                        Status = iface.OperationalStatus.ToString(),
                        IPv4Address = ipv4?.Address.ToString() ?? "",
                        IPv6Address = ipv6?.Address.ToString() ?? "",
                        SubnetMask = ipv4?.IPv4Mask.ToString() ?? "",
                        DefaultGateway = gw,
                        IsDhcpEnabled = props.GetIPv4Properties()?.IsDhcpEnabled ?? false
                    });
                }

                return Result<List<NetworkAdapterModel>>.Success(adapters.OrderBy(a => a.Name).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get network adapters");
                return Result<List<NetworkAdapterModel>>.Failure(ex.Message, ex);
            }
        }, ct);
    }

    public async Task<Result<List<NetworkConnectionModel>>> GetActiveConnectionsAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Scanning active network connections via netstat");
            var result = await _processService.ExecuteAsync("netstat", "-ano", false, ct);
            
            if (!result.IsSuccess || string.IsNullOrEmpty(result.Value))
            {
                return Result<List<NetworkConnectionModel>>.Failure(result.ErrorMessage ?? "Netstat failed");
            }

            var connections = new List<NetworkConnectionModel>();
            var lines = result.Value.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                
                // TCP has 5 cols: Proto, Local, Foreign, State, PID
                // UDP has 4 cols: Proto, Local, Foreign, PID
                if (parts.Length < 4 || (!parts[0].Equals("TCP", StringComparison.OrdinalIgnoreCase) && !parts[0].Equals("UDP", StringComparison.OrdinalIgnoreCase)))
                    continue;

                string proto = parts[0];
                string local = parts[1];
                string foreign = parts[2];
                string state = parts.Length == 5 ? parts[3] : "";
                string pidStr = parts.Length == 5 ? parts[4] : parts[3];

                int.TryParse(pidStr, out int pid);

                // Parse Port from Address
                int localPort = 0;
                int remotePort = 0;
                var localParts = local.Split(':');
                if (localParts.Length > 1) int.TryParse(localParts.Last(), out localPort);
                var remoteParts = foreign.Split(':');
                if (remoteParts.Length > 1) int.TryParse(remoteParts.Last(), out remotePort);

                // For a real production app, we would look up the ProcessName using System.Diagnostics.Process.GetProcessById(pid)
                // but doing it for 500+ connections is slow. We could cache it.
                string processName = "";

                connections.Add(new NetworkConnectionModel
                {
                    Protocol = proto,
                    LocalAddress = local,
                    LocalPort = localPort,
                    RemoteAddress = foreign,
                    RemotePort = remotePort,
                    State = state,
                    ProcessId = pid,
                    ProcessName = processName
                });
            }

            // Group by PID and fetch names to avoid 1000 process lookups
            var pidGroups = connections.GroupBy(c => c.ProcessId).Select(g => g.Key).ToList();
            var processNames = new Dictionary<int, string>();
            foreach (var pid in pidGroups)
            {
                try 
                {
                    if (pid > 0) processNames[pid] = System.Diagnostics.Process.GetProcessById(pid).ProcessName;
                }
                catch { processNames[pid] = "Unknown"; }
            }

            // Apply process names
            foreach (var conn in connections)
            {
                if (processNames.TryGetValue(conn.ProcessId, out var name))
                {
                    // Using records, we must create a new one to modify
                    // But we can just use with to modify the record since it's an immutable record
                    // Wait, we can't easily replace it in the list if we use record with { }.
                    // Let's change the record definition to be a class with settable properties or just replace the item in the list.
                }
            }

            // Let's do it cleanly by modifying the list before we return it.
            var finalConnections = connections.Select(c => c with 
            { 
                ProcessName = processNames.TryGetValue(c.ProcessId, out var name) ? name : "Unknown" 
            }).ToList();

            return Result<List<NetworkConnectionModel>>.Success(finalConnections);
        }
        catch (OperationCanceledException)
        {
            return Result<List<NetworkConnectionModel>>.Cancelled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse active connections");
            return Result<List<NetworkConnectionModel>>.Failure(ex.Message, ex);
        }
    }

    public async Task<Result<bool>> WakeOnLanAsync(string macAddress, CancellationToken ct = default)
    {
        try
        {
            macAddress = macAddress.Replace(":", "").Replace("-", "");
            if (macAddress.Length != 12)
                return Result<bool>.Failure("Invalid MAC Address format");

            byte[] macBytes = new byte[6];
            for (int i = 0; i < 6; i++)
            {
                macBytes[i] = Convert.ToByte(macAddress.Substring(i * 2, 2), 16);
            }

            byte[] packet = new byte[17 * 6];
            for (int i = 0; i < 6; i++)
                packet[i] = 0xFF;

            for (int i = 1; i <= 16; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    packet[i * 6 + j] = macBytes[j];
                }
            }

            using var client = new UdpClient();
            client.EnableBroadcast = true;
            await client.SendAsync(packet, packet.Length, new System.Net.IPEndPoint(System.Net.IPAddress.Broadcast, 9));

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Wake-on-LAN packet");
            return Result<bool>.Failure(ex.Message, ex);
        }
    }

    public async Task<Result<List<PortScanResultModel>>> PortScanAsync(string ipAddress, int startPort, int endPort, CancellationToken ct = default)
    {
        try
        {
            var results = new List<PortScanResultModel>();
            var tasks = new List<Task<PortScanResultModel>>();

            for (int port = startPort; port <= endPort; port++)
            {
                int currentPort = port;
                tasks.Add(Task.Run(async () =>
                {
                    var result = new PortScanResultModel { Port = currentPort, Status = "Closed", Service = GetCommonServiceName(currentPort) };
                    try
                    {
                        using var client = new TcpClient();
                        var connectTask = client.ConnectAsync(ipAddress, currentPort);
                        if (await Task.WhenAny(connectTask, Task.Delay(1000, ct)) == connectTask && client.Connected)
                        {
                            result.Status = "Open";
                        }
                    }
                    catch { }
                    return result;
                }, ct));
            }

            int batchSize = 100;
            for (int i = 0; i < tasks.Count; i += batchSize)
            {
                var batch = tasks.Skip(i).Take(batchSize);
                var batchResults = await Task.WhenAll(batch);
                results.AddRange(batchResults);
            }

            return Result<List<PortScanResultModel>>.Success(results.OrderBy(r => r.Port).ToList());
        }
        catch (OperationCanceledException)
        {
            return Result<List<PortScanResultModel>>.Cancelled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Port scan failed");
            return Result<List<PortScanResultModel>>.Failure(ex.Message, ex);
        }
    }

    public async Task<Result<List<PingSweepResultModel>>> PingSweepAsync(string baseIP, CancellationToken ct = default)
    {
        try
        {
            var results = new List<PingSweepResultModel>();
            var tasks = new List<Task<PingSweepResultModel>>();
            
            // Extract the base IP part assuming it's like 192.168.1.
            string prefix = baseIP;
            if (prefix.Count(c => c == '.') == 3)
            {
                prefix = prefix.Substring(0, prefix.LastIndexOf('.'));
            }
            if (!prefix.EndsWith("."))
            {
                prefix += ".";
            }

            for (int i = 1; i <= 254; i++)
            {
                string ip = prefix + i;
                tasks.Add(Task.Run(async () =>
                {
                    var result = new PingSweepResultModel { IPAddress = ip, Status = "Offline" };
                    try
                    {
                        using var ping = new Ping();
                        var reply = await ping.SendPingAsync(ip, 1000);
                        if (reply.Status == IPStatus.Success)
                        {
                            result.Status = "Online";
                            result.RoundtripTime = reply.RoundtripTime;
                        }
                    }
                    catch { }
                    return result;
                }, ct));
            }

            int batchSize = 50;
            for (int i = 0; i < tasks.Count; i += batchSize)
            {
                var batch = tasks.Skip(i).Take(batchSize);
                var batchResults = await Task.WhenAll(batch);
                results.AddRange(batchResults.Where(r => r.Status == "Online"));
            }

            return Result<List<PingSweepResultModel>>.Success(results.OrderBy(r =>
            {
                var parts = r.IPAddress.Split('.');
                return int.Parse(parts.Last());
            }).ToList());
        }
        catch (OperationCanceledException)
        {
            return Result<List<PingSweepResultModel>>.Cancelled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ping sweep failed");
            return Result<List<PingSweepResultModel>>.Failure(ex.Message, ex);
        }
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
}

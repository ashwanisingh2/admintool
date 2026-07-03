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
}

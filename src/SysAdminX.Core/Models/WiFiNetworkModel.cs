namespace SysAdminX.Core.Models;

public class WiFiNetworkModel
{
    public string Ssid { get; set; } = string.Empty;
    public string Bssid { get; set; } = string.Empty;
    public int SignalStrength { get; set; }
    public string Authentication { get; set; } = string.Empty;
    public string Encryption { get; set; } = string.Empty;
    public string NetworkType { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
}

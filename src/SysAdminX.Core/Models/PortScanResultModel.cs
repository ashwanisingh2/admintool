namespace SysAdminX.Core.Models;

public class PortScanResultModel
{
    public int Port { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
}

namespace SysAdminX.Core.Models;

public class BrowserRepairModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public bool IsInstalled { get; set; }
    public string InstallPath { get; set; } = string.Empty;
    public long CacheSize { get; set; }
}

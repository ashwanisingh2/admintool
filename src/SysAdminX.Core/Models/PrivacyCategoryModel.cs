namespace SysAdminX.Core.Models;

public class PrivacyCategoryModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public long EstimatedSizeBytes { get; set; }
    public bool IsSelected { get; set; } = true;
    public string PowerShellCommand { get; set; } = string.Empty;
}

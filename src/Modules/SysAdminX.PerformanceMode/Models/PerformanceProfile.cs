using CommunityToolkit.Mvvm.ComponentModel;

namespace SysAdminX.PerformanceMode.Models;

public partial class PerformanceProfile : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconName { get; set; } = string.Empty;
    
    [ObservableProperty]
    private bool _isActive;
}

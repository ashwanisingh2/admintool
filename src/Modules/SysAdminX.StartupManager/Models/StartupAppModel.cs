using CommunityToolkit.Mvvm.ComponentModel;

namespace SysAdminX.StartupManager.Models;

public partial class StartupAppModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _command = string.Empty;

    [ObservableProperty]
    private string _source = string.Empty;

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private string _impact = "Low";
}

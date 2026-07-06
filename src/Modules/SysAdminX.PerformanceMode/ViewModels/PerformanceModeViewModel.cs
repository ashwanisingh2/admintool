using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SysAdminX.PerformanceMode.Models;
using SysAdminX.PerformanceMode.Services;

namespace SysAdminX.PerformanceMode.ViewModels;

public partial class PerformanceModeViewModel : ObservableObject
{
    private readonly IPerformanceModeService _service;

    [ObservableProperty]
    private ObservableCollection<PerformanceProfile> _profiles = new();

    public PerformanceModeViewModel(IPerformanceModeService service)
    {
        _service = service;
        InitializeProfiles();
        _ = LoadCurrentProfileAsync();
    }

    private void InitializeProfiles()
    {
        Profiles = new ObservableCollection<PerformanceProfile>
        {
            new PerformanceProfile
            {
                Id = "gaming",
                Name = "Gaming Mode",
                Description = "Ultimate Performance power plan, Game Mode ON, HAGS ON, background apps disabled.",
                IconName = "XboxController24"
            },
            new PerformanceProfile
            {
                Id = "work",
                Name = "Work Mode",
                Description = "Balanced power plan, Game Mode OFF, background apps enabled.",
                IconName = "Briefcase24"
            },
            new PerformanceProfile
            {
                Id = "powersaver",
                Name = "Power Saver",
                Description = "Power Saver plan, Game Mode OFF, background apps disabled.",
                IconName = "Battery24"
            }
        };
    }

    private async Task LoadCurrentProfileAsync()
    {
        var result = await _service.GetCurrentProfileAsync();
        if (result.IsSuccess)
        {
            foreach (var profile in Profiles)
            {
                profile.IsActive = profile.Id == result.Value;
            }
        }
    }

    [RelayCommand]
    private async Task ApplyProfileAsync(string profileId)
    {
        var result = await _service.ApplyProfileAsync(profileId);
        if (result.IsSuccess)
        {
            await LoadCurrentProfileAsync();
        }
    }
}

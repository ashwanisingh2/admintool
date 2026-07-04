using System.Threading;
using System.Threading.Tasks;
using SysAdminX.Core.Models;
using SysAdminX.AutoPilot.Models;

namespace SysAdminX.AutoPilot.Services;

public interface IAutoPilotService
{
    Task<Result> ScheduleAsync(string dayOfWeek, string time, AutoPilotActions actions, CancellationToken ct = default);
    Task<Result<AutoPilotTaskInfo>> GetStatusAsync(CancellationToken ct = default);
    Task<Result> UnscheduleAsync(CancellationToken ct = default);
}

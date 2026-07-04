namespace SysAdminX.AutoPilot.Models;

using System;

public class AutoPilotTaskInfo
{
    public bool IsEnabled { get; set; }
    public DateTime? LastRunTime { get; set; }
    public int? LastTaskResult { get; set; }
    public DateTime? NextRunTime { get; set; }
}

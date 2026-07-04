namespace SysAdminX.AutoPilot.Models;

using System;

[Flags]
public enum AutoPilotActions
{
    None = 0,
    Junk = 1,
    Network = 2,
    Drivers = 4,
    Sfc = 8,
    Trim = 16,
    All = Junk | Network | Drivers | Sfc | Trim
}

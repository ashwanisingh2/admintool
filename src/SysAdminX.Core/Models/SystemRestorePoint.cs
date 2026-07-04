using System;

namespace SysAdminX.Core.Models;

public class SystemRestorePoint
{
    public int SequenceNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreationTime { get; set; }
    public string EventType { get; set; } = string.Empty;
}

using System;
using System.Collections.Generic;

namespace SysAdminX.Core.Models;

public class CleanupResultModel
{
    public long TotalBytesFreed { get; set; }
    public string BackupDirectory { get; set; } = string.Empty;
    public List<(string OriginalPath, string BackupPath)> MovedFiles { get; set; } = new();
    public DateTime UndoExpiresAt { get; set; }
}

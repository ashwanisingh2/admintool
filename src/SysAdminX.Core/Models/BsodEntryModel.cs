using System;

namespace SysAdminX.Core.Models;

public class BsodEntryModel
{
    public string DumpFile { get; set; } = string.Empty;
    public string BugCheckCode { get; set; } = string.Empty;
    public string BugCheckName { get; set; } = string.Empty;
    public string LikelyCause { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string StackTrace { get; set; } = string.Empty;
}

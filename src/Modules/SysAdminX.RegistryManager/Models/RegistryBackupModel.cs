using System;

namespace SysAdminX.RegistryManager.Models;

public class RegistryBackupModel
{
    public string Timestamp { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string HklmFilePath { get; set; } = string.Empty;
    public long HklmSizeBytes { get; set; }
    public string HkcuFilePath { get; set; } = string.Empty;
    public long HkcuSizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
}

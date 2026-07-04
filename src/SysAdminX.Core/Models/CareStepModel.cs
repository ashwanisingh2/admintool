using System;

namespace SysAdminX.Core.Models;

public enum CarePhase
{
    Idle,
    Restore,
    JunkScan,
    JunkPreview,
    JunkUndo,
    NetworkCheck,
    NetworkRun,
    Sfc,
    Trim,
    Security,
    Complete
}

public class CareStepModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
}

public class StepProgressEventArgs : EventArgs
{
    public string StepName { get; }
    public string Status { get; }
    public int Progress { get; }
    public string ErrorMessage { get; }
    public string OutputLine { get; }

    public StepProgressEventArgs(string stepName, string status, int progress, string errorMessage = "", string outputLine = "")
    {
        StepName = stepName;
        Status = status;
        Progress = progress;
        ErrorMessage = errorMessage;
        OutputLine = outputLine;
    }
}

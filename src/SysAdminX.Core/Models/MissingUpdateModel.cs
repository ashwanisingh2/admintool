using System;

namespace SysAdminX.Core.Models;

/// <summary>
/// Represents a missing Windows Update.
/// </summary>
public record MissingUpdateModel
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string KBArticles { get; init; } = string.Empty;
    public bool IsDownloaded { get; init; }
}

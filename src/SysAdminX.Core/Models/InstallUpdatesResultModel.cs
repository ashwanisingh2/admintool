using System;

namespace SysAdminX.Core.Models;

/// <summary>
/// Represents the result of installing Windows Updates.
/// </summary>
public record InstallUpdatesResultModel
{
    public bool RebootRequired { get; init; }
    public int ResultCode { get; init; }
}

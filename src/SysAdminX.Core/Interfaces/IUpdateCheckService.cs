// -----------------------------------------------------------------------
// <copyright file="IUpdateCheckService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using SysAdminX.Core.Models;

namespace SysAdminX.Core.Interfaces;

/// <summary>
/// Represents the result of a GitHub release lookup.
/// </summary>
public class UpdateInfoModel
{
    /// <summary>Tag name from GitHub (e.g. "v1.2.3").</summary>
    public string TagName { get; set; } = string.Empty;

    /// <summary>Human-readable release name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>HTML URL of the release page on GitHub.</summary>
    public string HtmlUrl { get; set; } = string.Empty;

    /// <summary>Release notes (markdown).</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>When the release was published.</summary>
    public string PublishedAt { get; set; } = string.Empty;

    /// <summary>Parsed version (e.g. 1.2.3) extracted from TagName.</summary>
    public System.Version? Version { get; set; }
}

/// <summary>
/// Checks GitHub releases for newer versions of SysAdminX.
/// </summary>
public interface IUpdateCheckService
{
    /// <summary>
    /// Fetches the latest release info from the configured GitHub repository.
    /// Returns null if no releases exist or the request fails.
    /// </summary>
    Task<UpdateInfoModel?> GetLatestReleaseAsync(string repository, CancellationToken ct = default);

    /// <summary>
    /// Compares two version strings (e.g. "v1.2.3" vs "v1.2.0") and returns
    /// true if <paramref name="latest"/> is strictly newer than
    /// <paramref name="current"/>.
    /// </summary>
    bool IsNewerVersion(string current, string latest);
}

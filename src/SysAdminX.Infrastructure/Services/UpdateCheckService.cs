// -----------------------------------------------------------------------
// <copyright file="UpdateCheckService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;

namespace SysAdminX.Infrastructure.Services;

/// <summary>
/// GitHub-backed implementation of <see cref="IUpdateCheckService"/>.
///
/// Uses the unauthenticated GitHub REST API (rate limit: 60 req/hour per IP)
/// which is plenty for a once-per-startup check.
/// </summary>
public class UpdateCheckService : IUpdateCheckService
{
    private readonly ILogger<UpdateCheckService> _logger;
    private static readonly HttpClient _httpClient = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    public UpdateCheckService(ILogger<UpdateCheckService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UpdateInfoModel?> GetLatestReleaseAsync(string repository, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(repository))
        {
            _logger.LogWarning("UpdateCheckService: repository is empty.");
            return null;
        }

        // repository should be "owner/repo"
        var url = $"https://api.github.com/repos/{repository.Trim('/')}/releases/latest";
        _logger.LogInformation("Checking for updates: {Url}", url);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            // GitHub requires a User-Agent header on all REST API calls.
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("SysAdminX", "1.0"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GitHub releases API returned {Status} {Reason}",
                    (int)response.StatusCode, response.ReasonPhrase);
                return null;
            }

            var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var root = doc.RootElement;

            var tag = root.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() ?? "" : "";
            var name = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "" : "";
            var html = root.TryGetProperty("html_url", out var htmlProp) ? htmlProp.GetString() ?? "" : "";
            var body = root.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() ?? "" : "";
            var pub  = root.TryGetProperty("published_at", out var pubProp) ? pubProp.GetString() ?? "" : "";

            var version = ParseVersion(tag);

            return new UpdateInfoModel
            {
                TagName = tag,
                Name = name,
                HtmlUrl = html,
                Body = body,
                PublishedAt = pub,
                Version = version
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Update check cancelled.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch latest release info from GitHub.");
            return null;
        }
    }

    public bool IsNewerVersion(string current, string latest)
    {
        var a = ParseVersion(current);
        var b = ParseVersion(latest);
        if (a == null || b == null) return false;
        return b > a;
    }

    /// <summary>
    /// Parses a version string like "v1.2.3", "1.2.3", or "v1.2.3-beta"
    /// into a <see cref="Version"/>. Returns null on failure.
    /// </summary>
    private static Version? ParseVersion(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        // strip leading 'v' or 'V'
        var trimmed = s.Trim();
        if (trimmed.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed.Substring(1);

        // strip pre-release suffix (e.g. "-beta", "-rc1")
        var dashIdx = trimmed.IndexOf('-');
        if (dashIdx >= 0)
            trimmed = trimmed.Substring(0, dashIdx);

        return Version.TryParse(trimmed, out var v) ? v : null;
    }
}

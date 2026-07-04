using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;
using SysAdminX.StartupManager.Models;

namespace SysAdminX.StartupManager.Services;

public class StartupManagerService : IStartupManagerService
{
    private readonly IPowerShellService _powerShellService;

    public StartupManagerService(IPowerShellService powerShellService)
    {
        _powerShellService = powerShellService;
    }

    private string GetEmbeddedScript(string scriptName)
    {
        var assembly = typeof(StartupManagerService).Assembly;
        var resourceName = $"SysAdminX.StartupManager.Scripts.{scriptName}";
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new FileNotFoundException($"Embedded resource '{resourceName}' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public async Task<Result<List<StartupAppModel>>> GetStartupAppsAsync(CancellationToken ct = default)
    {
        try
        {
            var script = GetEmbeddedScript("get_startup_apps.ps1");
            var result = await _powerShellService.ExecuteScriptContentAsync(script, null, ct);
            if (!result.IsSuccess)
                return Result<List<StartupAppModel>>.Failure(result.ErrorMessage);

            if (string.IsNullOrWhiteSpace(result.Data))
                return Result<List<StartupAppModel>>.Success(new List<StartupAppModel>());

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var apps = JsonSerializer.Deserialize<List<StartupAppModel>>(result.Data, options);

            if (apps != null)
            {
                foreach (var app in apps)
                {
                    var cmd = app.Command?.ToLowerInvariant() ?? string.Empty;
                    if (cmd.Contains("spotify") || cmd.Contains("discord") || cmd.Contains("teams") || cmd.Contains("steam") || cmd.Contains("onedrive"))
                    {
                        app.Impact = "High";
                    }
                    else if (cmd.Contains("tray") || cmd.Contains("agent") || cmd.Contains("update"))
                    {
                        app.Impact = "Medium";
                    }
                    else
                    {
                        app.Impact = "Low";
                    }
                }
            }

            return Result<List<StartupAppModel>>.Success(apps ?? new List<StartupAppModel>());
        }
        catch (Exception ex)
        {
            return Result<List<StartupAppModel>>.Failure(ex.Message);
        }
    }

    public async Task<Result> ToggleStartupAppAsync(StartupAppModel app, CancellationToken ct = default)
    {
        try
        {
            var script = GetEmbeddedScript("toggle_startup_app.ps1");
            var parameters = new Dictionary<string, object>
            {
                { "Name", app.Name },
                { "Source", app.Source },
                { "Enable", app.IsEnabled }
            };

            var result = await _powerShellService.ExecuteScriptContentAsync(script, parameters, ct);
            if (!result.IsSuccess)
                return Result.Failure(result.ErrorMessage);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}

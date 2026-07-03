// -----------------------------------------------------------------------
// <copyright file="RegistryService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.Infrastructure;

/// <summary>
/// Concrete implementation of <see cref="IRegistryService"/> using Microsoft.Win32.Registry.
/// </summary>
public class RegistryService : IRegistryService
{
    private readonly ILogger<RegistryService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegistryService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public RegistryService(ILogger<RegistryService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<Result<string>> ReadStringValueAsync(string hive, string keyPath, string valueName, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            try
            {
                if (ct.IsCancellationRequested) return Result<string>.Cancelled();

                using var baseKey = GetBaseKey(hive);
                if (baseKey == null)
                    return Result<string>.Failure($"Unknown registry hive: {hive}");

                using var subKey = baseKey.OpenSubKey(keyPath);
                if (subKey == null)
                    return Result<string>.Failure($"Registry key not found: {hive}\\{keyPath}");

                var value = subKey.GetValue(valueName);
                if (value == null)
                    return Result<string>.Failure($"Registry value not found: {valueName}");

                return Result<string>.Success(value.ToString() ?? string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read registry string value: {Hive}\\{Path}\\{Value}", hive, keyPath, valueName);
                return Result<string>.Failure(ex.Message);
            }
        }, ct);
    }

    /// <inheritdoc />
    public Task<Result<int>> ReadDWordValueAsync(string hive, string keyPath, string valueName, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            try
            {
                if (ct.IsCancellationRequested) return Result<int>.Cancelled();

                using var baseKey = GetBaseKey(hive);
                if (baseKey == null)
                    return Result<int>.Failure($"Unknown registry hive: {hive}");

                using var subKey = baseKey.OpenSubKey(keyPath);
                if (subKey == null)
                    return Result<int>.Failure($"Registry key not found: {hive}\\{keyPath}");

                var value = subKey.GetValue(valueName);
                if (value == null)
                    return Result<int>.Failure($"Registry value not found: {valueName}");

                if (value is int intValue)
                    return Result<int>.Success(intValue);

                if (int.TryParse(value.ToString(), out var parsedValue))
                    return Result<int>.Success(parsedValue);

                return Result<int>.Failure($"Registry value is not an integer: {value}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read registry dword value: {Hive}\\{Path}\\{Value}", hive, keyPath, valueName);
                return Result<int>.Failure(ex.Message);
            }
        }, ct);
    }

    private static RegistryKey? GetBaseKey(string hive)
    {
        return hive.ToUpperInvariant() switch
        {
            "HKEY_LOCAL_MACHINE" or "HKLM" => Registry.LocalMachine,
            "HKEY_CURRENT_USER" or "HKCU" => Registry.CurrentUser,
            "HKEY_CLASSES_ROOT" or "HKCR" => Registry.ClassesRoot,
            "HKEY_USERS" or "HKU" => Registry.Users,
            "HKEY_CURRENT_CONFIG" or "HKCC" => Registry.CurrentConfig,
            _ => null
        };
    }
}

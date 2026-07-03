// -----------------------------------------------------------------------
// <copyright file="WmiService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.Infrastructure;

/// <summary>
/// Concrete implementation of <see cref="IWmiService"/>.
/// Executes WMI queries against the local machine using System.Management.
/// All ManagementObject instances are properly disposed.
/// </summary>
public class WmiService : IWmiService
{
    private readonly ILogger<WmiService> _logger;
    private const string DEFAULT_NAMESPACE = @"root\CIMV2";

    /// <summary>
    /// Initializes a new instance of the <see cref="WmiService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public WmiService(ILogger<WmiService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<Result<List<Dictionary<string, object?>>>> QueryAsync(string query, CancellationToken ct = default)
    {
        return QueryAsync(DEFAULT_NAMESPACE, query, ct);
    }

    /// <inheritdoc />
    public Task<Result<List<Dictionary<string, object?>>>> QueryAsync(string wmiNamespace, string query, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                _logger.LogDebug("Executing WMI query: {Query} in namespace {Namespace}", query, wmiNamespace);

                var results = new List<Dictionary<string, object?>>();
                var scope = new ManagementScope(wmiNamespace);
                scope.Connect();

                using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery(query));
                using var collection = searcher.Get();

                foreach (ManagementBaseObject obj in collection)
                {
                    ct.ThrowIfCancellationRequested();
                    using var managementObj = obj;

                    var properties = new Dictionary<string, object?>();
                    foreach (var prop in managementObj.Properties)
                    {
                        properties[prop.Name] = prop.Value;
                    }
                    results.Add(properties);
                }

                _logger.LogDebug("WMI query returned {Count} results", results.Count);
                return Result<List<Dictionary<string, object?>>>.Success(results);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("WMI query cancelled: {Query}", query);
                return Result<List<Dictionary<string, object?>>>.Cancelled();
            }
            catch (ManagementException ex)
            {
                _logger.LogError(ex, "WMI query failed: {Query} — {Error}", query, ex.Message);
                return Result<List<Dictionary<string, object?>>>.Failure($"WMI query failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during WMI query: {Query}", query);
                return Result<List<Dictionary<string, object?>>>.Failure($"WMI error: {ex.Message}", ex);
            }
        }, ct);
    }

    /// <inheritdoc />
    public async Task<Result<T>> GetSingleValueAsync<T>(string query, string propertyName, CancellationToken ct = default)
    {
        try
        {
            var result = await QueryAsync(query, ct);
            if (!result.IsSuccess)
            {
                return Result<T>.Failure(result.ErrorMessage ?? "WMI query failed");
            }

            if (result.Value == null || result.Value.Count == 0)
            {
                return Result<T>.Failure($"No results returned for query: {query}");
            }

            var firstObj = result.Value[0];
            if (!firstObj.TryGetValue(propertyName, out var value) || value == null)
            {
                return Result<T>.Failure($"Property '{propertyName}' not found or is null");
            }

            if (value is T typedValue)
            {
                return Result<T>.Success(typedValue);
            }

            // Try conversion
            var converted = (T)Convert.ChangeType(value, typeof(T));
            return Result<T>.Success(converted);
        }
        catch (OperationCanceledException)
        {
            return Result<T>.Cancelled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get single value '{Property}' from WMI", propertyName);
            return Result<T>.Failure(ex.Message, ex);
        }
    }
}

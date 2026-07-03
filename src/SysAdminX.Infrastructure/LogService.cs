// -----------------------------------------------------------------------
// <copyright file="LogService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;

namespace SysAdminX.Infrastructure;

/// <summary>
/// Concrete implementation of <see cref="ILogService"/>.
/// Delegates to Microsoft.Extensions.Logging ILogger for structured logging.
/// </summary>
public class LogService : ILogService
{
    private readonly ILogger<LogService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogService"/> class.
    /// </summary>
    /// <param name="logger">The Microsoft.Extensions.Logging logger instance.</param>
    public LogService(ILogger<LogService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void LogDebug(string message, params object[] args)
    {
        _logger.LogDebug(message, args);
    }

    /// <inheritdoc />
    public void LogInformation(string message, params object[] args)
    {
        _logger.LogInformation(message, args);
    }

    /// <inheritdoc />
    public void LogWarning(string message, params object[] args)
    {
        _logger.LogWarning(message, args);
    }

    /// <inheritdoc />
    public void LogError(Exception? exception, string message, params object[] args)
    {
        _logger.LogError(exception, message, args);
    }

    /// <inheritdoc />
    public void LogCritical(Exception? exception, string message, params object[] args)
    {
        _logger.LogCritical(exception, message, args);
    }
}

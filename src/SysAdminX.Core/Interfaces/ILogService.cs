// -----------------------------------------------------------------------
// <copyright file="ILogService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace SysAdminX.Core.Interfaces;

/// <summary>
/// Provides application-level logging capabilities.
/// All services must use this interface for structured logging.
/// </summary>
public interface ILogService
{
    /// <summary>
    /// Logs a verbose/debug message.
    /// </summary>
    /// <param name="message">The log message template.</param>
    /// <param name="args">The log message arguments.</param>
    void LogDebug(string message, params object[] args);

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="message">The log message template.</param>
    /// <param name="args">The log message arguments.</param>
    void LogInformation(string message, params object[] args);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="message">The log message template.</param>
    /// <param name="args">The log message arguments.</param>
    void LogWarning(string message, params object[] args);

    /// <summary>
    /// Logs an error message with an optional exception.
    /// </summary>
    /// <param name="exception">The exception associated with the error, if any.</param>
    /// <param name="message">The log message template.</param>
    /// <param name="args">The log message arguments.</param>
    void LogError(Exception? exception, string message, params object[] args);

    /// <summary>
    /// Logs a critical/fatal error message.
    /// </summary>
    /// <param name="exception">The exception associated with the error, if any.</param>
    /// <param name="message">The log message template.</param>
    /// <param name="args">The log message arguments.</param>
    void LogCritical(Exception? exception, string message, params object[] args);
}

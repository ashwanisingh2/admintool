// -----------------------------------------------------------------------
// <copyright file="Result.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace SysAdminX.Core.Models;

/// <summary>
/// Represents the outcome of an operation that returns a value.
/// Avoids exception-driven flow by wrapping success/failure states.
/// </summary>
/// <typeparam name="T">The type of the value returned on success.</typeparam>
public class Result<T>
{
    /// <summary>
    /// Gets whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; private init; }

    /// <summary>
    /// Gets whether the operation was cancelled.
    /// </summary>
    public bool IsCancelled { get; private init; }

    /// <summary>
    /// Gets the value returned on success. Null if the operation failed.
    /// </summary>
    public T? Value { get; private init; }

    /// <summary>
    /// Gets the error message if the operation failed. Null on success.
    /// </summary>
    public string? ErrorMessage { get; private init; }

    /// <summary>
    /// Gets the exception if the operation failed with an exception.
    /// </summary>
    public Exception? Exception { get; private init; }

    private Result() { }

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    /// <param name="value">The value to return.</param>
    /// <returns>A successful result.</returns>
    public static Result<T> Success(T value) => new()
    {
        IsSuccess = true,
        IsCancelled = false,
        Value = value,
        ErrorMessage = null,
        Exception = null
    };

    /// <summary>
    /// Creates a failure result with the specified error message.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="exception">The optional exception.</param>
    /// <returns>A failure result.</returns>
    public static Result<T> Failure(string errorMessage, Exception? exception = null) => new()
    {
        IsSuccess = false,
        IsCancelled = false,
        Value = default,
        ErrorMessage = errorMessage,
        Exception = exception
    };

    /// <summary>
    /// Creates a cancelled result.
    /// </summary>
    /// <returns>A cancelled result.</returns>
    public static Result<T> Cancelled() => new()
    {
        IsSuccess = false,
        IsCancelled = true,
        Value = default,
        ErrorMessage = "Operation was cancelled.",
        Exception = null
    };
}

/// <summary>
/// Represents the outcome of an operation that does not return a value.
/// </summary>
public class Result
{
    /// <summary>
    /// Gets whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; private init; }

    /// <summary>
    /// Gets whether the operation was cancelled.
    /// </summary>
    public bool IsCancelled { get; private init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; private init; }

    /// <summary>
    /// Gets the exception if the operation failed with an exception.
    /// </summary>
    public Exception? Exception { get; private init; }

    private Result() { }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static Result Success() => new()
    {
        IsSuccess = true,
        IsCancelled = false,
        ErrorMessage = null,
        Exception = null
    };

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="exception">The optional exception.</param>
    /// <returns>A failure result.</returns>
    public static Result Failure(string errorMessage, Exception? exception = null) => new()
    {
        IsSuccess = false,
        IsCancelled = false,
        ErrorMessage = errorMessage,
        Exception = exception
    };

    /// <summary>
    /// Creates a cancelled result.
    /// </summary>
    /// <returns>A cancelled result.</returns>
    public static Result Cancelled() => new()
    {
        IsSuccess = false,
        IsCancelled = true,
        ErrorMessage = "Operation was cancelled.",
        Exception = null
    };
}

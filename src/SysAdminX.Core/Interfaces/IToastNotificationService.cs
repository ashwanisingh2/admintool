// -----------------------------------------------------------------------
// <copyright file="IToastNotificationService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace SysAdminX.Core.Interfaces;

/// <summary>
/// Severity of a toast notification. Drives the icon and color used by the
/// <c>Sonner</c>-style toast control used in the Shell.
/// </summary>
public enum ToastSeverity
{
    /// <summary>Informational message (blue / accent).</summary>
    Info,

    /// <summary>Operation completed successfully (green).</summary>
    Success,

    /// <summary>Warning that does not block (amber).</summary>
    Warning,

    /// <summary>Error that does not crash the app (red).</summary>
    Error
}

/// <summary>
/// Lightweight cross-module notification surface. Modules should call this
/// instead of <see cref="System.Windows.MessageBox.Show(string)"/> so that
/// the Shell can decide how to surface the message (toast bar, status bar,
/// tray balloon, etc.) and so we do not block the UI thread on modal dialogs.
/// </summary>
public interface IToastNotificationService
{
    /// <summary>
    /// Shows a toast with the given title, message, and severity. The
    /// implementation is responsible for thread-marshalling onto the UI
    /// thread, so callers may invoke this from any thread.
    /// </summary>
    /// <param name="title">Short, action-oriented title (e.g. "Update installed").</param>
    /// <param name="message">Longer body text. May be empty.</param>
    /// <param name="severity">Severity / color of the toast.</param>
    void Show(string title, string message = "", ToastSeverity severity = ToastSeverity.Info);

    /// <summary>Convenience wrapper for <see cref="Show"/> with <see cref="ToastSeverity.Success"/>.</summary>
    void ShowSuccess(string title, string message = "") => Show(title, message, ToastSeverity.Success);

    /// <summary>Convenience wrapper for <see cref="Show"/> with <see cref="ToastSeverity.Warning"/>.</summary>
    void ShowWarning(string title, string message = "") => Show(title, message, ToastSeverity.Warning);

    /// <summary>Convenience wrapper for <see cref="Show"/> with <see cref="ToastSeverity.Error"/>.</summary>
    void ShowError(string title, string message = "") => Show(title, message, ToastSeverity.Error);
}

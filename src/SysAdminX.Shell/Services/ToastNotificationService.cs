// -----------------------------------------------------------------------
// <copyright file="ToastNotificationService.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace SysAdminX.Shell.Services;

/// <summary>
/// WPF-UI backed implementation of <see cref="IToastNotificationService"/>.
///
/// Uses the <c>SnackbarService</c> exposed by the WPF-UI <see cref="INavigationView"/>
/// when available. Falls back to a transient <see cref="MessageBox"/> if the
/// SnackbarService has not been attached (e.g. during early startup or in
/// design-time data contexts).
/// </summary>
public class ToastNotificationService : IToastNotificationService
{
    private readonly ILogger<ToastNotificationService> _logger;
    private ISnackbarService? _snackbarService;

    public ToastNotificationService(ILogger<ToastNotificationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Attaches the WPF-UI snackbar service. Called once from
    /// <c>App.OnStartup</c> after the main window is created.
    /// </summary>
    public void AttachSnackbarService(ISnackbarService snackbarService)
    {
        _snackbarService = snackbarService ?? throw new ArgumentNullException(nameof(snackbarService));
    }

    /// <inheritdoc />
    public void Show(string title, string message = "", ToastSeverity severity = ToastSeverity.Info)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            _logger.LogWarning("ToastNotificationService.Show called with empty title. Ignored.");
            return;
        }

        // Truncate very long messages so they don't blow up the snackbar layout.
        var body = message ?? string.Empty;
        if (body.Length > 400)
        {
            body = body.Substring(0, 397) + "...";
        }

        var icon = severity switch
        {
            ToastSeverity.Success => SymbolRegular.CheckmarkCircle24,
            ToastSeverity.Warning => SymbolRegular.Warning24,
            ToastSeverity.Error => SymbolRegular.ErrorCircle24,
            _ => SymbolRegular.Info24
        };

        var appearance = severity switch
        {
            ToastSeverity.Success => ControlAppearance.Success,
            ToastSeverity.Warning => ControlAppearance.Caution,
            ToastSeverity.Error => ControlAppearance.Danger,
            _ => ControlAppearance.Info
        };

        try
        {
            if (_snackbarService == null)
            {
                // No snackbar attached yet — log and fall back to a message box
                // only for severe messages, otherwise silently drop to avoid
                // spamming the user during early startup.
                _logger.LogInformation("Toast (no snackbar attached) [{Severity}] {Title}: {Message}",
                    severity, title, body);
                if (severity == ToastSeverity.Error || severity == ToastSeverity.Warning)
                {
                    var app = Application.Current;
                    if (app?.Dispatcher != null)
                    {
                        app.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            System.Windows.MessageBox.Show(
                                app.MainWindow,
                                $"{title}\n\n{body}",
                                severity == ToastSeverity.Error ? "Error" : "Warning",
                                System.Windows.MessageBoxButton.OK,
                                severity == ToastSeverity.Error ? System.Windows.MessageBoxImage.Error : System.Windows.MessageBoxImage.Warning);
                        }), DispatcherPriority.Normal);
                    }
                }
                return;
            }

            // Marshal onto the UI thread — SnackbarService.Show must be called
            // from the dispatcher. Callers may invoke this from a background
            // thread (e.g. Task.Run), so always BeginInvoke.
            var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
            dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    _snackbarService.Show(title, body, appearance, new SymbolIcon(icon), TimeSpan.FromSeconds(3));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SnackbarService.Show threw while showing toast {Title}", title);
                }
            }), DispatcherPriority.Normal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show toast notification {Title}", title);
        }
    }
}

// -----------------------------------------------------------------------
// <copyright file="SplashWindow.xaml.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SysAdminX.Shell.Views;

/// <summary>
/// Lightweight splash window shown during the early phases of app startup
/// (DI container build, settings load, theme apply) before the main window
/// is ready. Closes itself when <see cref="Finish"/> is called.
/// </summary>
public partial class SplashWindow : Window
{
    private readonly Stopwatch _stopwatch = new();
    private readonly Action? _onShown;

    public SplashWindow(Action? onShown = null)
    {
        InitializeComponent();
        _onShown = onShown;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _stopwatch.Start();

        // Fade-in animation so the splash doesn't pop in abruptly.
        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
        BeginAnimation(OpacityProperty, fadeIn);

        _onShown?.Invoke();
    }

    /// <summary>
    /// Closes the splash. Should be called from the UI thread once the main
    /// window is ready to show. Logs how long the splash was visible so we
    /// can spot slow startups in the log.
    /// </summary>
    public void Finish()
    {
        _stopwatch.Stop();
        System.Diagnostics.Debug.WriteLine($"Splash visible for {_stopwatch.ElapsedMilliseconds} ms");

        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));
        fadeOut.Completed += (s, _) => Close();
        BeginAnimation(OpacityProperty, fadeOut);
    }
}

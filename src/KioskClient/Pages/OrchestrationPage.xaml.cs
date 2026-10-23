using KioskClient.Core.Models;
using KioskClient.Core.Services;
using KioskClient.Pages.Actions;
using KioskClient.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;

namespace KioskClient.Pages;

/// <summary>
/// Orchestration execution page. Runs in full-screen mode and displays
/// actions sequentially or randomly based on orchestration config.
/// Handles keyboard escape, network status, and auto-retry on return.
/// </summary>
public sealed partial class OrchestrationPage : Page
{
    private OrchestrationRunner? _runner;
    private Orchestration? _orchestration;
    private DispatcherTimer? _countdownTimer;
    private DateTime _actionEndUtc;

    public OrchestrationPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is not Orchestration orchestration) return;

        _orchestration = orchestration;
        App.SettingsVM.RecordLastRun(orchestration);

        // Enter full screen
        var mainWindow = App.MainWindow as MainWindow;
        mainWindow?.EnterFullScreen();

        // Set up the runner
        _runner = App.OrchestrationRunner;
        _runner.NextAction += Runner_NextAction;
        _runner.ActionTimerStarted += Runner_ActionTimerStarted;
        _runner.OrchestrationCompleted += Runner_Completed;
        _runner.OrchestrationCancelled += Runner_Cancelled;
        _runner.NetworkStatusChanged += Runner_NetworkStatusChanged;
        _runner.StatusUpdate += Runner_StatusUpdate;

        // Ensure this page has focus so keyboard events (e.g. Escape) are received
        this.Focus(FocusState.Programmatic);

        // Start the orchestration
        _ = _runner.StartAsync(orchestration);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        CleanupRunner();

        var mainWindow = App.MainWindow as MainWindow;
        mainWindow?.ExitFullScreen();
    }

    private void Runner_NextAction(object? sender, NextActionEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            switch (e.Action)
            {
                case ImageAction imageAction:
                    ActionFrame.Navigate(typeof(ImagePage), imageAction);
                    break;
                case WebsiteAction websiteAction:
                    ActionFrame.Navigate(typeof(WebsitePage), websiteAction);
                    break;
                case VideoAction videoAction:
                    ActionFrame.Navigate(typeof(VideoPage), videoAction);
                    break;
            }
        });
    }

    private void Runner_ActionTimerStarted(object? sender, int durationSeconds)
    {
        DispatcherQueue.TryEnqueue(() => StartCountdown(durationSeconds));
    }

    /// <summary>Starts (or restarts) the countdown overlay. Called by action pages that know their own length.</summary>
    public void StartCountdown(int durationSeconds)
    {
        {
            var vm = App.SettingsVM;
            if (!vm.ShowCountdown || durationSeconds <= 0)
            {
                StopCountdown();
                return;
            }

            if (vm.CountdownAutoContrast)
            {
                CountdownText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White);
                CountdownBadge.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Black);
            }
            else
            {
                CountdownText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(vm.CountdownColor);
                CountdownBadge.Background = null;
            }
            CountdownBadge.Opacity = Math.Clamp(vm.CountdownOpacityPercent, 0, 100) / 100.0;
            CountdownBadge.Visibility = Visibility.Visible;

            _actionEndUtc = DateTime.UtcNow.AddSeconds(durationSeconds);
            UpdateCountdownText();

            _countdownTimer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _countdownTimer.Tick -= CountdownTimer_Tick;
            _countdownTimer.Tick += CountdownTimer_Tick;
            _countdownTimer.Start();
        }
    }

    private void CountdownTimer_Tick(object? sender, object e) => UpdateCountdownText();

    private void UpdateCountdownText()
    {
        var remaining = (int)Math.Ceiling((_actionEndUtc - DateTime.UtcNow).TotalSeconds);
        CountdownText.Text = Math.Max(remaining, 0).ToString();
    }

    private void StopCountdown()
    {
        _countdownTimer?.Stop();
        CountdownBadge.Visibility = Visibility.Collapsed;
    }

    private void Runner_Completed(object? sender, EventArgs e)
    {
        App.SettingsVM.AddLog("Orchestration ended: completed all actions.");
        DispatcherQueue.TryEnqueue(() => ReturnToHome(false));
    }

    private void Runner_Cancelled(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() => ReturnToHome(false));
    }

    private void Runner_NetworkStatusChanged(object? sender, bool isConnected)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            var vm = App.SettingsVM;
            var foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(vm.NetworkWatermarkForeground);
            NetworkWatermark.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(vm.NetworkWatermarkBackground);
            NetworkWatermarkIcon.Foreground = foreground;
            NetworkWatermarkText.Foreground = foreground;
            NetworkWatermark.Visibility = isConnected
                ? Visibility.Collapsed
                : Visibility.Visible;
        });
    }

    private void Runner_StatusUpdate(object? sender, OrchestrationStatusEventArgs e)
    {
        App.SettingsVM.AddLog(e.Message);
    }

    private void EscapeAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        CancelAndReturn("Escape key pressed");
        args.Handled = true;
    }

    private void Page_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Escape ||
            e.Key == Windows.System.VirtualKey.Home)
        {
            CancelAndReturn(e.Key == Windows.System.VirtualKey.Escape ? "Escape key pressed" : "Home key pressed");
        }
        else if (e.Key == Windows.System.VirtualKey.F5)
        {
            RestartOrchestration();
        }
        else if (e.Key == Windows.System.VirtualKey.R &&
                 Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
                     .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
        {
            RestartOrchestration();
        }
    }

    private void RestartOrchestration()
    {
        if (_runner is null || _orchestration is null) return;
        App.SettingsVM.AddLog("Orchestration restarted by user.");
        _runner.Stop();
        _ = _runner.StartAsync(_orchestration);
    }

    /// <summary>
    /// Called from action pages when user clicks the settings/exit button.
    /// </summary>
    public void CancelAndReturn(string reason = "Settings/exit button clicked")
    {
        App.SettingsVM.AddLog($"Orchestration stopped by user: {reason}.");
        _runner?.Stop();
        ReturnToHome(true);
    }

    private void ReturnToHome(bool wasCancelled)
    {
        CleanupRunner();

        // Navigate back - if auto-retry is configured, signal it
        if (!wasCancelled && App.SettingsVM.IsAutoRetryEnabled && App.SettingsVM.CanStart)
        {
            Frame.Navigate(typeof(HomePage), "autoRetry");
        }
        else
        {
            Frame.Navigate(typeof(SettingsPage));
        }
    }

    private void CleanupRunner()
    {
        _countdownTimer?.Stop();
        if (_runner is not null)
        {
            _runner.NextAction -= Runner_NextAction;
            _runner.ActionTimerStarted -= Runner_ActionTimerStarted;
            _runner.OrchestrationCompleted -= Runner_Completed;
            _runner.OrchestrationCancelled -= Runner_Cancelled;
            _runner.NetworkStatusChanged -= Runner_NetworkStatusChanged;
            _runner.StatusUpdate -= Runner_StatusUpdate;
        }
    }
}

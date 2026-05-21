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
    private DispatcherTimer? _pollingTimer;

    public OrchestrationPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is not Orchestration orchestration) return;

        _orchestration = orchestration;

        // Enter full screen
        var mainWindow = App.MainWindow as MainWindow;
        mainWindow?.EnterFullScreen();

        // Set up the runner
        _runner = App.OrchestrationRunner;
        _runner.NextAction += Runner_NextAction;
        _runner.OrchestrationCompleted += Runner_Completed;
        _runner.OrchestrationCancelled += Runner_Cancelled;
        _runner.NetworkStatusChanged += Runner_NetworkStatusChanged;
        _runner.StatusUpdate += Runner_StatusUpdate;

        // Ensure this page has focus so keyboard events (e.g. Escape) are received
        this.Focus(FocusState.Programmatic);

        // Start the orchestration
        _ = _runner.StartAsync(orchestration);

        // Start polling timer for URL-sourced orchestrations
        if (orchestration.Source == OrchestrationSource.URL && orchestration.PollingInterval > 0)
            StartPollingTimer(orchestration.PollingInterval);
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
            }
        });
    }

    private void Runner_Completed(object? sender, EventArgs e)
    {
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
        CancelAndReturn();
        args.Handled = true;
    }

    private void Page_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Escape ||
            e.Key == Windows.System.VirtualKey.Home)
        {
            CancelAndReturn();
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
        _runner.Stop();
        _ = _runner.StartAsync(_orchestration);
    }

    private void StartPollingTimer(int intervalSeconds)
    {
        _pollingTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(intervalSeconds) };
        _pollingTimer.Tick += PollingTimer_Tick;
        _pollingTimer.Start();
        App.SettingsVM.AddLog($"Polling: will check for orchestration updates every {intervalSeconds} second(s).");
    }

    private async void PollingTimer_Tick(object? sender, object e)
    {
        _pollingTimer?.Stop();
        App.SettingsVM.AddLog("Polling: checking for orchestration updates...");

        _runner?.Stop();

        var result = await App.SettingsVM.TryAutoStartAsync();

        if (result == StartupLoadResult.LoadedAndValid)
        {
            App.SettingsVM.AddLog("Polling: orchestration updated successfully. Resuming.");
            _orchestration = App.SettingsVM.Orchestration;
            _ = _runner?.StartAsync(_orchestration!);
            _pollingTimer?.Start();
        }
        else
        {
            App.SettingsVM.AddLog("Polling: failed to reload orchestration. Returning to settings to retry.");
            App.SettingsVM.ShouldAutoRetryStart = true;
            Frame.Navigate(typeof(SettingsPage));
        }
    }

    /// <summary>
    /// Called from action pages when user clicks the settings/exit button.
    /// </summary>
    public void CancelAndReturn()
    {
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
        _pollingTimer?.Stop();
        _pollingTimer = null;

        if (_runner is not null)
        {
            _runner.NextAction -= Runner_NextAction;
            _runner.OrchestrationCompleted -= Runner_Completed;
            _runner.OrchestrationCancelled -= Runner_Cancelled;
            _runner.NetworkStatusChanged -= Runner_NetworkStatusChanged;
            _runner.StatusUpdate -= Runner_StatusUpdate;
        }
    }
}

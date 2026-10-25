using KioskClient.Dialogs;
using KioskClient.Core.Logging;
using KioskClient.Services;
using KioskClient.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;

namespace KioskClient.Pages;

/// <summary>
/// Home/splash page shown on app startup. Displays the Kiosk Client logo,
/// loading status, and a Settings button. Handles auto-retry countdown
/// when returning from a failed orchestration.
/// </summary>
public sealed partial class HomePage : Page
{
    private DispatcherTimer? _retryTimer;
    private bool _shouldAutoRetry;
    private bool _retryPaused;

    public HomePage()
    {
        this.InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        AppLog.Debug("Startup", $"Home page opened: mode={(e.Parameter as string) ?? "(none)"}.");

        if (e.Parameter is string param && param == "showTutorial")
        {
            LoadingRing.IsActive = false;
            StatusText.Text = string.Empty;
            Frame.Navigate(typeof(SettingsPage), "tutorialPrompt");
        }
        // Auto-retry when returning from a failed orchestration run
        else if (e.Parameter is string retryParam && retryParam == "autoRetry")
        {
            StartAutoRetryCountdown();
        }
        else if (e.Parameter is string startupParam && startupParam == "startup")
        {
            await StartupLoadAsync();
        }
        else
        {
            LoadingRing.IsActive = false;
            NavigateToSettings();
        }
    }

    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        this.Loaded -= OnPageLoaded; // unsubscribe to avoid repeat calls
        await ShowTutorialDialogAsync();
    }

    private async Task ShowTutorialDialogAsync()
    {
        var choice = await TutorialDialog.ShowTutorialAsync(this.XamlRoot);
        AppLog.Debug("Startup", $"Tutorial dialog closed: choice={choice?.ToString() ?? "none"}.");

        if (choice is not null)
        {
            Frame.Navigate(typeof(SettingsPage), choice.Value);
        }
        else
        {
            StatusText.Text = "Press Settings to configure an orchestration.";
        }
    }

    private async Task StartupLoadAsync()
    {
        LoadingRing.IsActive = true;
        StatusText.Text = "Loading last orchestration...";
        AppLog.Info("Startup", "Auto-start: loading last orchestration.");

        var result = await App.SettingsVM.TryAutoStartAsync();
        AppLog.Info("Startup", $"Auto-start result: {result}.");

        switch (result)
        {
            case StartupLoadResult.LoadedAndValid:
                Frame.Navigate(typeof(OrchestrationPage), App.SettingsVM.Orchestration);
                break;
            case StartupLoadResult.LoadFailed:
                App.SettingsVM.ShouldAutoRetryStart = true;
                Frame.Navigate(typeof(SettingsPage));
                break;
            default:
                Frame.Navigate(typeof(SettingsPage));
                break;
        }
    }


    private void StartAutoRetryCountdown()
    {
        var vm = App.SettingsVM;
        if (!vm.IsAutoRetryEnabled || !vm.CanStart)
        {
            AppLog.Info("Startup", $"Auto-retry not started: enabled={vm.IsAutoRetryEnabled}, canStart={vm.CanStart}.");
            LoadingRing.IsActive = false;
            StatusText.Text = "Orchestration returned. Press Settings to reconfigure.";
            return;
        }

        _shouldAutoRetry = true;
        _retryPaused = false;
        vm.CurrentAutoRetryCountdown = vm.AutoRetrySeconds;
        vm.IsAutoRetryActive = true;
        AppLog.Info("Startup", $"Auto-retry countdown started: {vm.AutoRetrySeconds}s.");

        LoadingRing.IsActive = false;
        StatusText.Text = "Retrying orchestration...";
        RetryPanel.Visibility = Visibility.Visible;
        RetryCountdownText.Text = $"Retry in {vm.CurrentAutoRetryCountdown}s";

        _retryTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _retryTimer.Tick += RetryTimer_Tick;
        _retryTimer.Start();
    }

    private void RetryTimer_Tick(object? sender, object e)
    {
        var vm = App.SettingsVM;
        if (_retryPaused) return;

        vm.CurrentAutoRetryCountdown--;
        RetryCountdownText.Text = $"Retry in {vm.CurrentAutoRetryCountdown}s";

        if (vm.CurrentAutoRetryCountdown <= 0)
        {
            StopRetryTimer();
            AppLog.Info("Startup", $"Auto-retry countdown elapsed: canStart={vm.CanStart}, hasOrchestration={vm.Orchestration is not null}.");
            if (vm.CanStart && vm.Orchestration is not null)
            {
                Frame.Navigate(typeof(OrchestrationPage), vm.Orchestration);
            }
        }
    }

    private void StopRetryButton_Click(object sender, RoutedEventArgs e)
    {
        AppLog.Info("Startup", "Auto-retry stopped by user.");
        StopRetryTimer();
        RetryPanel.Visibility = Visibility.Collapsed;
        StatusText.Text = "Auto-retry stopped. Press Settings to reconfigure.";
        App.SettingsVM.IsAutoRetryActive = false;
    }

    private void StopRetryTimer()
    {
        _retryTimer?.Stop();
        _retryTimer = null;
        _shouldAutoRetry = false;
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        StopRetryTimer();
        NavigateToSettings();
    }

    private void NavigateToSettings()
    {
        Frame.Navigate(typeof(SettingsPage));
    }

    private void Page_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Escape ||
            e.Key == Windows.System.VirtualKey.Home)
        {
            NavigateToSettings();
        }
    }
}

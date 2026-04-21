using KioskClient.Dialogs;
using KioskClient.Services;
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

        if (e.Parameter is string param && param == "showTutorial")
        {
            LoadingRing.IsActive = false;
            StatusText.Text = string.Empty;

            // Defer until the page is fully loaded and XamlRoot is available
            this.Loaded += OnPageLoaded;
        }
        // Auto-retry when returning from a failed orchestration run
        else if (e.Parameter is string retryParam && retryParam == "autoRetry")
        {
            StartAutoRetryCountdown();
        }
        else
        {
            LoadingRing.IsActive = false;
            StatusText.Text = "Press Settings to configure an orchestration.";
        }
    }

    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        this.Loaded -= OnPageLoaded; // unsubscribe to avoid repeat calls
        await ShowTutorialDialogAsync();
    }

    private async Task ShowTutorialDialogAsync()
    {
        var dialog = new TutorialDialog { XamlRoot = this.XamlRoot };
        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            // User wants to run the demo - navigate to settings
            NavigateToSettings();
        }
        else
        {
            StatusText.Text = "Press Settings to configure an orchestration.";
        }
    }

    private void StartAutoRetryCountdown()
    {
        var vm = App.SettingsVM;
        if (!vm.IsAutoRetryEnabled || !vm.CanStart)
        {
            LoadingRing.IsActive = false;
            StatusText.Text = "Orchestration returned. Press Settings to reconfigure.";
            return;
        }

        _shouldAutoRetry = true;
        _retryPaused = false;
        vm.CurrentAutoRetryCountdown = vm.AutoRetrySeconds;
        vm.IsAutoRetryActive = true;

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
            // Navigate to settings to start the orchestration
            if (vm.CanStart && vm.Orchestration is not null)
            {
                Frame.Navigate(typeof(OrchestrationPage), vm.Orchestration);
            }
        }
    }

    private void StopRetryButton_Click(object sender, RoutedEventArgs e)
    {
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

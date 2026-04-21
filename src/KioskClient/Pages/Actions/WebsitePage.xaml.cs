using KioskClient.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;

namespace KioskClient.Pages.Actions;

/// <summary>
/// Displays a website in a WebView2 control with optional auto-scrolling.
/// Auto-scrolling smoothly scrolls through the page content over the configured
/// ScrollingTime, then resets to the top after ScrollingResetDelay.
/// A settings/exit button is shown for SettingsDisplayTime seconds, then hidden.
/// </summary>
public sealed partial class WebsitePage : Page
{
    private WebsiteAction? _action;
    private DispatcherTimer? _scrollingTimer;
    private DispatcherTimer? _settingsButtonTimer;
    private double _currentTick;
    private double _totalTicks;
    private double _webviewContentHeight;
    private const int RefreshRate = 60;

    public WebsitePage()
    {
        this.InitializeComponent();
        WebviewDisplay.NavigationCompleted += WebviewDisplay_NavigationCompleted;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is not WebsiteAction action) return;
        _action = action;

        try
        {
            WebviewDisplay.Source = new Uri(_action.Path);
            WebviewDisplay.Visibility = Visibility.Visible;

            // Set up auto-scroll timer
            if (_action.AutoScroll && _action.ScrollingTime > 0)
            {
                _currentTick = 0;
                _totalTicks = RefreshRate * _action.ScrollingTime;

                _scrollingTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds((1.0 / RefreshRate) * 1000)
                };
                _scrollingTimer.Tick += ScrollingTimer_Tick;
            }

            // Set up settings button hide timer
            if (_action.SettingsDisplayTime > 0)
            {
                _settingsButtonTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(_action.SettingsDisplayTime)
                };
                _settingsButtonTimer.Tick += SettingsButtonTimer_Tick;
            }
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _scrollingTimer?.Stop();
        _settingsButtonTimer?.Stop();
    }

    private async void WebviewDisplay_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        try
        {
            if (!args.IsSuccess)
            {
                ShowError($"Navigation failed: {args.WebErrorStatus}");
                return;
            }

            // Get the document height for scroll calculations
            var heightResult = await WebviewDisplay.ExecuteScriptAsync("document.body.scrollHeight");
            if (!string.IsNullOrEmpty(heightResult) && double.TryParse(heightResult, out var height))
            {
                _webviewContentHeight = height;
                _scrollingTimer?.Start();
            }

            _settingsButtonTimer?.Start();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private async void ScrollingTimer_Tick(object? sender, object e)
    {
        try
        {
            if (++_currentTick > _totalTicks)
            {
                // Pause at bottom, then scroll back to top
                if (_action?.ScrollingResetDelay > 0)
                {
                    _scrollingTimer?.Stop();
                    await Task.Delay(_action.ScrollingResetDelay * 1000);
                    _scrollingTimer?.Start();
                }

                _currentTick = 0;
                await WebviewDisplay.ExecuteScriptAsync("window.scrollTo(0,0);");
            }
            else if (_webviewContentHeight > 0)
            {
                var scrollPosition = (_currentTick / _totalTicks) * _webviewContentHeight;
                await WebviewDisplay.ExecuteScriptAsync($"window.scrollTo(0,{scrollPosition});");
            }
        }
        catch
        {
            // WebView may be disposed during navigation
        }
    }

    private void SettingsButtonTimer_Tick(object? sender, object e)
    {
        _settingsButtonTimer?.Stop();
        SettingsButton.Visibility = Visibility.Collapsed;
    }

    private void ShowError(string message)
    {
        WebviewDisplay.Visibility = Visibility.Collapsed;
        ErrorPanel.Visibility = Visibility.Visible;
        ErrorDetail.Text = message;
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (this.Frame.Parent is Frame parentFrame &&
            parentFrame.Parent is Pages.OrchestrationPage orchestrationPage)
        {
            orchestrationPage.CancelAndReturn();
        }
    }
}

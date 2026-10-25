using KioskClient.Core.Logging;
using KioskClient.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;

namespace KioskClient.Pages.Actions;

/// <summary>
/// Displays a website in a WebView2 control with optional auto-scrolling.
/// Auto-scrolling moves at a fixed, readable speed for as long as the action is displayed,
/// optionally after ScrollingStartDelay seconds.
/// A settings/exit button is shown for SettingsDisplayTime seconds, then hidden.
/// </summary>
public sealed partial class WebsitePage : Page
{
    private WebsiteAction? _action;
    private DispatcherTimer? _scrollingTimer;
    private DispatcherTimer? _settingsButtonTimer;
    private double _position;
    private readonly System.Diagnostics.Stopwatch _stopwatch = new();
    private TimeSpan _lastTick;
    private const int RefreshRate = 60;
    private const double PixelsPerSecond = 60;
    private bool _escapeHooked;
    private bool _scrollErrorLogged;
    public WebsitePage()
    {
        this.InitializeComponent();
        SettingsButton.Opacity = App.SettingsVM.SettingsButtonOpacity;
        WebviewDisplay.NavigationCompleted += WebviewDisplay_NavigationCompleted;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is not WebsiteAction action) return;
        _action = action;

        AppLog.Debug("WebsitePage", $"Loading website: target={AppLog.Describe(_action.Path)}, autoScroll={_action.AutoScroll}, scrollSpeed={_action.ScrollSpeed}, scrollDelay={_action.ScrollingStartDelay}s, settingsDisplayTime={_action.SettingsDisplayTime}s.");

        try
        {
            await WebviewDisplay.EnsureCoreWebView2Async();
            if (!_escapeHooked)
            {
                _escapeHooked = true;
                // WebView2 swallows key events, so forward Escape from the page via a web message.
                await WebviewDisplay.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
                    "window.addEventListener('keydown',function(e){if(e.key==='Escape'){window.chrome.webview.postMessage('kiosk-escape');}},true);");
                WebviewDisplay.CoreWebView2.WebMessageReceived += (s, a) =>
                {
                    if (a.TryGetWebMessageAsString() == "kiosk-escape")
                        DispatcherQueue.TryEnqueue(() => SettingsButton_Click(this, new RoutedEventArgs()));
                };
            }

            WebviewDisplay.Source = new Uri(_action.Path);
            WebviewDisplay.Visibility = Visibility.Visible;

            // Set up auto-scroll timer
            if (_action.AutoScroll)
            {
                _position = 0;

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
            AppLog.Error("WebsitePage", $"Failed to load website {AppLog.Describe(_action.Path)}.", ex);
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
                AppLog.Error("WebsitePage", $"Navigation failed: status={args.WebErrorStatus}, httpStatus={args.HttpStatusCode}.");
                ShowError($"Navigation failed: {args.WebErrorStatus}");
                return;
            }

            AppLog.Debug("WebsitePage", $"Navigation completed: httpStatus={args.HttpStatusCode}.");

            if (_scrollingTimer is not null)
            {
                _position = 0;
                _stopwatch.Restart();
                _lastTick = TimeSpan.Zero;
                _scrollingTimer.Start();
            }

            _settingsButtonTimer?.Start();
        }
        catch (Exception ex)
        {
            AppLog.Error("WebsitePage", "Error handling navigation completion.", ex);
            ShowError(ex.Message);
        }
    }

    private async void ScrollingTimer_Tick(object? sender, object e)
    {
        try
        {
            var now = _stopwatch.Elapsed;
            var dt = Math.Min((now - _lastTick).TotalSeconds, 0.25);
            _lastTick = now;

            if (_action is null || now.TotalSeconds < _action.ScrollingStartDelay) return;

            _position += PixelsPerSecond * _action.ScrollSpeed * dt;
            var y = _position.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
            await WebviewDisplay.ExecuteScriptAsync($"window.scrollTo(0,{y});");
        }
        catch (Exception ex)
        {
            // WebView may be disposed during navigation
            if (!_scrollErrorLogged)
            {
                _scrollErrorLogged = true;
                AppLog.Debug("WebsitePage", $"Auto-scroll step failed (WebView may be navigating): {ex.GetType().Name}.");
            }
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
        // Visual tree: OrchestrationPage > Grid > ActionFrame > WebsitePage
        if (this.Frame.Parent is FrameworkElement parent &&
            parent.Parent is Pages.OrchestrationPage orchestrationPage)
        {
            orchestrationPage.CancelAndReturn();
        }
    }
}

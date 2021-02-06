/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using KioskClient.Pages;
using KioskLibrary.Actions;
using KioskLibrary.Helpers;
using Serilog;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace KioskLibrary.Pages.Actions
{
    /// <summary>
    /// A page for displaying a website
    /// </summary>
    public sealed partial class WebsitePage : Page
    {
        private DispatcherTimer _scrollingTimer;
        private DispatcherTimer _settingsButtonTimer;
        private WebsiteAction _websiteAction;
        private double _currentTick;
        private double _totalTicks;
        private double _webviewContentHeight;
        private string _scrollToTopString = @"window.scrollTo(0,0);";

        public WebsitePage()
        {
            InitializeComponent();
            _scrollingTimer = new DispatcherTimer();
            _settingsButtonTimer = new DispatcherTimer();
            Webview_Display.LoadCompleted += WvDisplay_LoadCompleted;
        }

        private async void WvDisplay_LoadCompleted(object sender, NavigationEventArgs e)
        {
            var heightString = await Webview_Display.InvokeScriptAsync("eval", new[] { "document.body.scrollHeight.toString()" });
            if (!string.IsNullOrEmpty(heightString))
                if (double.TryParse(heightString, out var height))
                {
                    _webviewContentHeight = height;
                    _scrollingTimer.Start();
                }

            _settingsButtonTimer.Interval = TimeSpan.FromSeconds(5);
            _settingsButtonTimer.Tick += _settingsTimer_Tick;
            _settingsButtonTimer.Start();
        }

        private void _settingsTimer_Tick(object sender, object e)
        {
            _settingsButtonTimer.Stop();
            Button_Settings.Visibility = Visibility.Collapsed;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _websiteAction = e.Parameter as WebsiteAction;

            Log.Information("WebsitePage OnNavigatedTo: {data}", SerializationHelper.JSONSerialize(_websiteAction));

            Webview_Display.Source = new Uri(_websiteAction.Path);

            if (!_websiteAction.ScrollInterval.HasValue || _websiteAction.ScrollInterval.Value <= 0)
                _websiteAction.ScrollInterval = 1;

            if (_websiteAction.AutoScroll && _websiteAction.ScrollDuration.HasValue)
            {
                _currentTick = 0;
                _totalTicks = _websiteAction.ScrollDuration.Value / _websiteAction.ScrollInterval.Value;

                _scrollingTimer.Interval = TimeSpan.FromSeconds(_websiteAction.ScrollInterval.Value);
                _scrollingTimer.Tick += _scrollingTimer_Tick;
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e) => _scrollingTimer.Stop();

        private async void _scrollingTimer_Tick(object sender, object e)
        {
            if (++_currentTick >= _totalTicks)
            {
                if (_websiteAction.ScrollResetDelay.HasValue)
                    System.Threading.Thread.Sleep(_websiteAction.ScrollResetDelay.Value * 1000);

                // Reset _currentTick
                _currentTick = 0;

                // Scroll to top
                await Webview_Display.InvokeScriptAsync("eval", new string[] { _scrollToTopString });
            }
            else if (_webviewContentHeight > 0) // Scroll a bit
                await Webview_Display.InvokeScriptAsync("eval", new string[] { $"window.scrollTo(0,{(_currentTick / _totalTicks) * _webviewContentHeight});" });
        }

        private void Button_Settings_Click(object sender, RoutedEventArgs e)
        {
            PagesHelper.GoToSettings();
        }
    }
}

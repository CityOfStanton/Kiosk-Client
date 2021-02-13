/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using KioskClient.Dialogs;
using KioskLibrary.Actions;
using KioskLibrary.Helpers;
using KioskLibrary.ViewModels;
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
        private WebsiteViewModel State { get; set; } // Variable name is not in _ format because it is being referenced in associated partial class
        private readonly DispatcherTimer _scrollingTimer;
        private readonly DispatcherTimer _settingsButtonTimer;
        private WebsiteAction _websiteAction;
        private double _currentTick;
        private double _totalTicks;
        private double _webviewContentHeight;
        private readonly string _scrollToTopString = @"window.scrollTo(0,0);";

        public WebsitePage()
        {
            InitializeComponent();
            _scrollingTimer = new DispatcherTimer();
            _settingsButtonTimer = new DispatcherTimer();
            Webview_Display.LoadCompleted += WvDisplay_LoadCompleted;

            if (State == null)
                State = new WebsiteViewModel();
        }

        private async void WvDisplay_LoadCompleted(object sender, NavigationEventArgs e)
        {
            try
            {
                var documentBodyScrollHeight = await Webview_Display.InvokeScriptAsync("eval", new[] { "document.body.scrollHeight.toString()" });

                if (!string.IsNullOrEmpty(documentBodyScrollHeight))
                    if (double.TryParse(documentBodyScrollHeight, out var height))
                    {
                        _webviewContentHeight = height;
                        _scrollingTimer.Start();
                    }

                _settingsButtonTimer.Interval = TimeSpan.FromSeconds(_websiteAction.SettingsDisplayTime);
                _settingsButtonTimer.Tick += SettingsTimer_Tick;
                _settingsButtonTimer.Start();
            }
            catch (Exception ex)
            {
                TextBlock_ConnectionError.Text += $" - {ex.Message}";
                State.IsWebpageValid = false;
                Log.Error(ex, ex.Message);
            }
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            _websiteAction = e.Parameter as WebsiteAction;

            Log.Information("WebsitePage OnNavigatedTo: {data}", SerializationHelper.JSONSerialize(_websiteAction));

            var refreshRate = 60;

            State.IsLoaded = true;

            (State.IsWebpageValid, _, _) = await _websiteAction.ValidateAsync();

            if (State.IsWebpageValid)
            {
                Webview_Display.Source = new Uri(_websiteAction.Path);
             
                if (_websiteAction.AutoScroll && _websiteAction.ScrollingTime.HasValue)
                {
                    _currentTick = 0;
                    _totalTicks = Convert.ToDouble(refreshRate * _websiteAction.ScrollingTime);

                    _scrollingTimer.Interval = TimeSpan.FromMilliseconds((1.0 / refreshRate) * 1000);
                    _scrollingTimer.Tick += ScrollingTimer_Tick;
                }
            }
            else
                TextBlock_ConnectionError.Visibility = Visibility.Visible;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _scrollingTimer.Stop();
            _settingsButtonTimer.Stop();
        }

        private void SettingsTimer_Tick(object sender, object e)
        {
            _settingsButtonTimer.Stop();
            Button_Settings.Visibility = Visibility.Collapsed;
        }

        private async void ScrollingTimer_Tick(object sender, object e)
        {
            if (++_currentTick > _totalTicks)
            {
                if (_websiteAction.ScrollingResetDelay.HasValue)
                    System.Threading.Thread.Sleep(_websiteAction.ScrollingResetDelay.Value * 1000);

                // Reset _currentTick
                _currentTick = 0;

                // Scroll to top
                await Webview_Display.InvokeScriptAsync("eval", new string[] { _scrollToTopString });
            }
            else if (_webviewContentHeight > 0) // Scroll a bit
                await Webview_Display.InvokeScriptAsync("eval", new string[] { $"window.scrollTo(0,{(_currentTick / _totalTicks) * _webviewContentHeight});" });
        }

        private void Button_Settings_Click(object sender, RoutedEventArgs e) => PagesHelper.GoToSettings();
    }
}

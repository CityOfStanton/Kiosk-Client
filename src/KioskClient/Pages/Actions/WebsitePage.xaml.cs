/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using KioskClient.Dialogs;
using KioskClient.Pages.PageArguments;
using KioskLibrary.Actions;
using KioskLibrary.Helpers;
using KioskLibrary.ViewModels;
using Serilog;
using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace KioskLibrary.Pages.Actions
{
    /// <summary>
    /// A page for displaying a website
    /// </summary>
    public sealed partial class WebsitePage : Page
    {
        private ActionViewModel State { get; set; } // Variable name is not in _ format because it is being referenced in associated partial class
        private readonly DispatcherTimer _scrollingTimer;
        private readonly DispatcherTimer _settingsButtonTimer;
        private WebsiteAction _action;
        private double _currentTick;
        private double _totalTicks;
        private double _webviewContentHeight;
        private readonly string _scrollToTopString = @"window.scrollTo(0,0);";
        private System.Action _cancelOrchestration;

        public WebsitePage()
        {
            InitializeComponent();
            _scrollingTimer = new DispatcherTimer();
            _settingsButtonTimer = new DispatcherTimer();
            Webview_Display.LoadCompleted += Webview_Display_LoadCompleted;

            if (State == null)
                State = new ActionViewModel();
        }

        private async void Webview_Display_LoadCompleted(object sender, NavigationEventArgs e)
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

                _settingsButtonTimer.Interval = TimeSpan.FromSeconds(_action.SettingsDisplayTime);
                _settingsButtonTimer.Tick += SettingsTimer_Tick;
                _settingsButtonTimer.Start();
            }
            catch (Exception ex)
            {
                State.IsContentSourceValid = false;
                State.FailedToLoadContentMessageDetail = ex.Message;
                Log.Error(ex, ex.Message);
            }
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                var apa = e.Parameter as ActionPageArguments;
                _action = apa.Action as WebsiteAction;
                _cancelOrchestration = apa.CancelOrchestration;

                Log.Information("WebsitePage OnNavigatedTo: {data}", SerializationHelper.JSONSerialize(_action));

                var refreshRate = 60;

                var validationResult = await _action.ValidateAsync();

                State.IsContentSourceValid = validationResult.IsValid;
                State.FailedToLoadContentMessageDetail = validationResult.GetValidationSummaryOfChildren();

                if (State.IsContentSourceValid.Value)
                {
                    Webview_Display.Source = new Uri(_action.Path);

                    if (_action.AutoScroll && _action.ScrollingTime.HasValue)
                    {
                        _currentTick = 0;
                        _totalTicks = Convert.ToDouble(refreshRate * _action.ScrollingTime);

                        _scrollingTimer.Interval = TimeSpan.FromMilliseconds((1.0 / refreshRate) * 1000);
                        _scrollingTimer.Tick += ScrollingTimer_Tick;
                    }
                }
                else
                    Log.Error("Failed to validate {action} due to the following errors: {errors}", _action, validationResult);
            }
            catch (Exception ex)
            {
                State.IsContentSourceValid = false;
                State.FailedToLoadContentMessageDetail = ex.Message;
                Log.Error(ex, ex.Message);
            }
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
                if (_action.ScrollingResetDelay.HasValue)
                    System.Threading.Thread.Sleep(_action.ScrollingResetDelay.Value * 1000);

                // Reset _currentTick
                _currentTick = 0;

                // Scroll to top
                await Webview_Display.InvokeScriptAsync("eval", new string[] { _scrollToTopString });
            }
            else if (_webviewContentHeight > 0) // Scroll a bit
                await Webview_Display.InvokeScriptAsync("eval", new string[] { $"window.scrollTo(0,{(_currentTick / _totalTicks) * _webviewContentHeight});" });
        }

        private void Button_Settings_Click(object sender, RoutedEventArgs e) => _cancelOrchestration();
    }
}

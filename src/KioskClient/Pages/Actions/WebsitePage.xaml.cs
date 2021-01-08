using KioskLibrary.Actions;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace KioskClient.Pages.Actions
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WebsitePage : Page
    {
        private DispatcherTimer _scrollingTimer;
        private WebsiteAction _webstiteAction;
        private double _currentTick;
        private double _totalTicks;
        private double _webviewContentHeight;
        private string _scrollToTopString = @"window.scrollTo(0,0);";

        public WebsitePage()
        {
            InitializeComponent();
            _scrollingTimer = new DispatcherTimer();
            wvDisplay.LoadCompleted += WvDisplay_LoadCompleted;
        }

        private async void WvDisplay_LoadCompleted(object sender, NavigationEventArgs e)
        {
            var heightString = await wvDisplay.InvokeScriptAsync("eval", new[] { "document.body.scrollHeight.toString()" });
            if (!string.IsNullOrEmpty(heightString))
                if (double.TryParse(heightString, out var height))
                {
                    _webviewContentHeight = height;
                    _scrollingTimer.Start();
                }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _webstiteAction = e.Parameter as WebsiteAction;
            wvDisplay.Source = new Uri(_webstiteAction.Path);

            if (!_webstiteAction.ScrollInterval.HasValue || _webstiteAction.ScrollInterval.Value <= 0)
                _webstiteAction.ScrollInterval = 1;

            if (_webstiteAction.AutoScroll && _webstiteAction.ScrollDuration.HasValue)
            {
                _currentTick = 0;
                _totalTicks = _webstiteAction.ScrollDuration.Value / _webstiteAction.ScrollInterval.Value;

                _scrollingTimer.Interval = TimeSpan.FromSeconds(_webstiteAction.ScrollInterval.Value);
                _scrollingTimer.Tick += _scrollingTimer_Tick;
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e) => _scrollingTimer.Stop();

        private async void _scrollingTimer_Tick(object sender, object e)
        {
            if (++_currentTick >= _totalTicks)
            {
                if (_webstiteAction.ScrollResetDelay.HasValue)
                    System.Threading.Thread.Sleep(_webstiteAction.ScrollResetDelay.Value * 1000);

                // Reset _currentTick
                _currentTick = 0;

                // Scroll to top
                await wvDisplay.InvokeScriptAsync("eval", new string[] { _scrollToTopString });
            }
            else if (_webviewContentHeight > 0) // Scroll a bit
                await wvDisplay.InvokeScriptAsync("eval", new string[] { $"window.scrollTo(0,{(_currentTick / _totalTicks) * _webviewContentHeight});" });
        }
    }
}

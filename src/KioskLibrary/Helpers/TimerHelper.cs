using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml;

namespace KioskLibrary.Helpers
{
    /// <summary>
    /// A testable representation of the <see cref="IDispatcherTimer"/>
    /// </summary>
    public class TimerHelper : ITimeHelper
    {
        private readonly DispatcherTimer _timer;

        /// <summary>
        /// Constructor
        /// </summary>
        public TimerHelper()
        {
            _timer = new DispatcherTimer();
            _timer.Tick += Timer_Tick;
        }

        /// <inheritdoc/>
        public TimeSpan Interval
        {
            get { return _timer.Interval; }
            set { _timer.Interval = value; }
        }

        /// <inheritdoc/>
        public bool IsEnabled
        {
            get { return _timer.IsEnabled; }
        }

        /// <inheritdoc/>
        public event EventHandler<object> Tick;

        /// <inheritdoc/>
        public void Start() => _timer.Start();

        /// <inheritdoc/>
        public void Stop() => _timer.Stop();

        private void Timer_Tick(object sender, object e) => Tick?.Invoke(sender, e);
    }
}

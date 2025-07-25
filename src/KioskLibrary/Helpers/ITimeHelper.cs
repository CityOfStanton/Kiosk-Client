using System;
using Microsoft.UI.Xaml;

namespace KioskLibrary.Helpers
{
    /// <summary>
    /// Interface to support a testable <see cref="DispatcherTimer"/>
    /// </summary>
    public interface ITimeHelper
    {
        /// <summary>
        /// <see cref="DispatcherTimer.IsEnabled"/>
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// <see cref="DispatcherTimer.Interval"/>
        /// </summary>
        TimeSpan Interval { get; set; }

        /// <summary>
        /// <see cref="DispatcherTimer.Tick"/>
        /// </summary>
        event EventHandler<object> Tick;

        /// <summary>
        /// <see cref="DispatcherTimer.Start()"/>
        /// </summary>
        void Start();

        /// <summary>
        /// <see cref="DispatcherTimer.Stop()"/>
        /// </summary>
        void Stop();
    }
}
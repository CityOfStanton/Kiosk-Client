/*  
 * Copyright 2020
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 */

using System.Collections.Generic;

namespace KioskLibrary.Actions
{
    /// <summary>
    /// Settings for a slideshow action
    /// </summary>
    public class SlideshowAction : Action
    {
        public List<ImageAction> Images { get; set; }
        public Ordering Order { get; set; } = Ordering.Sequential;

        public SlideshowAction() { }

        public SlideshowAction(string name, int? duration, List<ImageAction> images, Ordering order = Ordering.Sequential)
        : base(name, duration)
        {
            Images = images;
            Order = order;
        }
    }
}

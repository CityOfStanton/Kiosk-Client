/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using System.Collections.Generic;
using Windows.ApplicationModel.Background;
using Windows.Networking.BackgroundTransfer;

namespace KioskLibrary.Storage
{
    public class ApplicationCacheProcessor : IBackgroundTask
    {
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            var details = (BackgroundTransferCompletionGroupTriggerDetails)taskInstance.TriggerDetails;
            IReadOnlyList<DownloadOperation> downloads = details.Downloads;

            // Do post-processing on each finished operation in the list of downloads
        }
    }
}

/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.Web;
using Serilog;
using KioskLibrary.Actions;

namespace KioskLibrary.Storage
{
    public class DownloadFile
    {
        private List<DownloadOperation> activeDownloads;
        private CancellationTokenSource cts;

        public DownloadFile()
        {
            cts = new CancellationTokenSource();
            activeDownloads = new List<DownloadOperation>();
        }

        public void Dispose()
        {
            if (cts != null)
            {
                cts.Dispose();
                cts = null;
            }

            GC.SuppressFinalize(this);
        }

        // Enumerate the downloads that were going on in the background while the app was closed.
        public async Task DiscoverActiveDownloadsAsync()
        {
            activeDownloads = new List<DownloadOperation>();

            IReadOnlyList<DownloadOperation> downloads;
            try
            {
                downloads = await BackgroundDownloader.GetCurrentDownloadsAsync();
            }
            catch (Exception ex)
            {
                if (!IsExceptionHandled("Discovery error", ex))
                    throw;
                return;
            }

            Log.Information("Loading background downloads: " + downloads.Count);

            if (downloads.Count > 0)
            {
                var tasks = new List<Task>();
                foreach (DownloadOperation download in downloads)
                {
                    Log.Information(string.Format(CultureInfo.CurrentCulture,
                        "Discovered background download: {0}, Status: {1}", download.Guid,
                        download.Progress.Status));

                    // Attach progress and completion handlers.
                    tasks.Add(HandleDownloadAsync(download, false));
                }

                // Don't await HandleDownloadAsync() in the foreach loop since we would attach to the second
                // download only when the first one completed; attach to the third download when the second one
                // completes etc. We want to attach to all downloads immediately.
                // If there are actions that need to be taken once downloads complete, await tasks here, outside
                // the loop.
                await Task.WhenAll(tasks);
            }
        }

        public async Task StartDownload(Actions.Action action, BackgroundTransferPriority priority = BackgroundTransferPriority.Default)
        {
            // Validating the URI is required since it was received from an untrusted source (user input).
            // The URI is validated by calling Uri.TryCreate() that will return 'false' for strings that are not valid URIs.
            // Note that when enabling the text box users may provide URIs to machines on the intrAnet that require
            // the "Private Networks (Client and Server)" capability.
            if (!Uri.TryCreate((action as IActionPath).Path.Trim(), UriKind.Absolute, out Uri source))
            {
                Log.Error("Invalid URI.");
                return;
            }

            var key = action.LocalKey ?? Guid.NewGuid().ToString();

            var tempCacheStore = await ApplicationCache.CreateApplicationCacheInstance(CacheStore.Temporary);
            var destinationFile = await tempCacheStore.CreateAsync(key);

            var downloader = new BackgroundDownloader();
            DownloadOperation download = downloader.CreateDownload(source, destinationFile);

            Log.Information(string.Format(CultureInfo.CurrentCulture, "Downloading {0} to {1} with {2} priority, {3}",
                source.AbsoluteUri, destinationFile.Name, priority, download.Guid));

            download.Priority = priority;

            // Attach progress and completion handlers.
            await HandleDownloadAsync(download, true);
        }

        private void ResumeAll_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Downloads: " + activeDownloads.Count);

            foreach (DownloadOperation download in activeDownloads)
            {
                // DownloadOperation.Progress is updated in real-time while the operation is ongoing. Therefore,
                // we must make a local copy so that we can have a consistent view of that ever-changing state
                // throughout this method's lifetime.
                BackgroundDownloadProgress currentProgress = download.Progress;

                if (currentProgress.Status == BackgroundTransferStatus.PausedByApplication)
                {
                    download.Resume();
                    Log.Information("Resumed: " + download.Guid);
                }
                else
                {
                    Log.Information(string.Format(CultureInfo.CurrentCulture, "Skipped: {0}, Status: {1}", download.Guid,
                        currentProgress.Status));
                }
            }
        }

        private void CancelAll_Click(object _, RoutedEventArgs _)
        {
            Log.Information("Canceling Downloads: " + activeDownloads.Count);

            cts.Cancel();
            cts.Dispose();

            // Re-create the CancellationTokenSource and activeDownloads for future downloads.
            cts = new CancellationTokenSource();
            activeDownloads = new List<DownloadOperation>();
        }

        // Note that this event is invoked on a background thread, so we cannot access the UI directly.
        private void DownloadProgress(DownloadOperation download)
        {
            // DownloadOperation.Progress is updated in real-time while the operation is ongoing. Therefore,
            // we must make a local copy so that we can have a consistent view of that ever-changing state
            // throughout this method's lifetime.
            BackgroundDownloadProgress currentProgress = download.Progress;

            LogProgress(string.Format(CultureInfo.CurrentCulture, "Progress: {0}, Status: {1}", download.Guid,
                currentProgress.Status));

            double percent = 100;
            if (currentProgress.TotalBytesToReceive > 0)
                percent = currentProgress.BytesReceived * 100 / currentProgress.TotalBytesToReceive;

            LogProgress(string.Format(
                CultureInfo.CurrentCulture,
                " - Transferred bytes: {0} of {1}, {2}%",
                currentProgress.BytesReceived,
                currentProgress.TotalBytesToReceive,
                percent));

            if (currentProgress.HasRestarted)
                LogProgress(" - Download restarted");

            if (currentProgress.HasResponseChanged)
            {
                // We have received new response headers from the server.
                // Be aware that GetResponseInformation() returns null for non-HTTP transfers (e.g., FTP).
                ResponseInformation response = download.GetResponseInformation();
                int headersCount = response != null ? response.Headers.Count : 0;

                LogProgress(" - Response updated; Header count: " + headersCount);

                // If you want to stream the response data this is a good time to start.
                // download.GetResultStreamAt(0);
            }
        }

        private void LogProgress(string message)
        {
            Log.Information(message);
        }

        private async Task HandleDownloadAsync(DownloadOperation download, bool start)
        {
            try
            {
                Log.Information("Running: " + download.Guid);

                // Store the download so we can pause/resume.
                activeDownloads.Add(download);

                var progressCallback = new Progress<DownloadOperation>(DownloadProgress);
                if (start)
                {
                    // Start the download and attach a progress handler.
                    await download.StartAsync().AsTask(cts.Token, progressCallback);
                }
                else
                {
                    // The download was already running when the application started, re-attach the progress handler.
                    await download.AttachAsync().AsTask(cts.Token, progressCallback);
                }

                ResponseInformation response = download.GetResponseInformation();

                // GetResponseInformation() returns null for non-HTTP transfers (e.g., FTP).
                string statusCode = response != null ? response.StatusCode.ToString() : string.Empty;

                Log.Information(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        "Completed: {0}, Status Code: {1}",
                        download.Guid,
                        statusCode));
            }
            catch (TaskCanceledException)
            {
                Log.Information("Canceled: " + download.Guid);
            }
            catch (Exception ex)
            {
                if (!IsExceptionHandled("Execution error", ex, download))
                    throw;
            }
            finally
            {
                activeDownloads.Remove(download);
            }
        }

        private bool IsExceptionHandled(string title, Exception ex, DownloadOperation download = null)
        {
            WebErrorStatus error = BackgroundTransferError.GetStatus(ex.HResult);
            if (error == WebErrorStatus.Unknown)
                return false;

            if (download == null)
                Log.Information(string.Format(CultureInfo.CurrentCulture, "Error: {0}: {1}", title, error));
            else
                Log.Information(string.Format(CultureInfo.CurrentCulture, "Error: {0} - {1}: {2}", download.Guid, title, error));

            return true;
        }
    }
}

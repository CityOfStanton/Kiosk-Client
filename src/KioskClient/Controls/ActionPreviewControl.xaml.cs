using KioskClient.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;

namespace KioskClient.Controls;

/// <summary>
/// Displays a preview of an Image, Website, or Video action and reports whether
/// the action's path could actually be loaded.
/// </summary>
public sealed partial class ActionPreviewControl : UserControl
{
    private static readonly TimeSpan LoadTimeout = TimeSpan.FromSeconds(15);

    public ActionPreviewControl()
    {
        this.InitializeComponent();
    }

    /// <summary>Clears any displayed preview and releases media resources.</summary>
    public void Clear()
    {
        HideContent();
        PreviewImage.Source = null;
        PreviewMessage.Visibility = Visibility.Collapsed;
        PreviewProgress.IsActive = false;
        try { PreviewWeb.CoreWebView2?.Stop(); } catch { }
    }

    /// <summary>
    /// Loads the action's path into the preview.
    /// </summary>
    /// <returns><see langword="null"/> when the path loaded successfully; otherwise an error message.</returns>
    public async Task<string?> ShowAsync(ActionBase action)
    {
        Clear();
        PreviewProgress.IsActive = true;

        try
        {
            var path = action switch
            {
                ImageAction i => i.Path,
                WebsiteAction w => w.Path,
                VideoAction v => v.Path,
                _ => null
            };

            if (path is null) return Fail("Preview not available for this action type.");
            if (string.IsNullOrWhiteSpace(path)) return Fail("Path is required.");
            if (!Uri.TryCreate(path.Trim(), UriKind.Absolute, out var uri))
                return Fail("The path is not a valid URL or full file path.");
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeFile)
                return Fail("Only http, https, and file paths are supported.");
            if (uri.IsFile && action is not WebsiteAction && !File.Exists(uri.LocalPath))
                return Fail("File not found.");

            string? error = action switch
            {
                ImageAction => await LoadImageAsync(uri),
                WebsiteAction => await LoadWebsiteAsync(uri),
                _ => await LoadVideoAsync(uri)
            };

            return error is null ? null : Fail(error);
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
        finally
        {
            PreviewProgress.IsActive = false;
        }
    }

    private async Task<string?> LoadImageAsync(Uri uri)
    {
        var bitmap = new BitmapImage();

        if (uri.IsFile)
        {
            var file = await StorageFile.GetFileFromPathAsync(uri.LocalPath);
            using var stream = await file.OpenReadAsync();
            await bitmap.SetSourceAsync(stream);
            PreviewImage.Source = bitmap;
            PreviewImage.Visibility = Visibility.Visible;
            return null;
        }

        var tcs = new TaskCompletionSource<string?>();
        bitmap.ImageOpened += (_, _) => tcs.TrySetResult(null);
        bitmap.ImageFailed += (_, e) => tcs.TrySetResult($"Unable to load image: {e.ErrorMessage}");
        PreviewImage.Source = bitmap;
        PreviewImage.Visibility = Visibility.Visible;
        bitmap.UriSource = uri;
        return await WaitAsync(tcs.Task);
    }

    private async Task<string?> LoadWebsiteAsync(Uri uri)
    {
        PreviewWeb.Visibility = Visibility.Visible;
        await PreviewWeb.EnsureCoreWebView2Async();
        var core = PreviewWeb.CoreWebView2;
        core.IsMuted = true;

        var tcs = new TaskCompletionSource<string?>();
        void Handler(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs args) =>
            tcs.TrySetResult(args.IsSuccess ? null : $"Unable to load website ({args.WebErrorStatus}).");

        core.NavigationCompleted += Handler;
        try
        {
            core.Navigate(uri.ToString());
            return await WaitAsync(tcs.Task);
        }
        finally
        {
            core.NavigationCompleted -= Handler;
        }
    }

    private async Task<string?> LoadVideoAsync(Uri uri)
    {
        var tcs = new TaskCompletionSource<string?>();
        var player = new Windows.Media.Playback.MediaPlayer
        {
            IsMuted = true,
            IsLoopingEnabled = true,
            AutoPlay = true
        };
        player.MediaOpened += (_, _) => tcs.TrySetResult(null);
        player.MediaFailed += (_, e) => tcs.TrySetResult($"Unable to play video: {e.ErrorMessage}");
        player.Source = Windows.Media.Core.MediaSource.CreateFromUri(uri);
        PreviewVideo.SetMediaPlayer(player);
        PreviewVideo.Visibility = Visibility.Visible;
        return await WaitAsync(tcs.Task);
    }

    private static async Task<string?> WaitAsync(Task<string?> task)
    {
        var completed = await Task.WhenAny(task, Task.Delay(LoadTimeout));
        return completed == task ? await task : "Timed out waiting for the content to load.";
    }

    private string Fail(string message)
    {
        HideContent();
        PreviewMessage.Text = message;
        PreviewMessage.Visibility = Visibility.Visible;
        return message;
    }

    private void HideContent()
    {
        PreviewImage.Visibility = Visibility.Collapsed;
        PreviewWeb.Visibility = Visibility.Collapsed;
        if (PreviewVideo.MediaPlayer is { } player)
        {
            PreviewVideo.SetMediaPlayer(null);
            player.Dispose();
        }
        PreviewVideo.Visibility = Visibility.Collapsed;
    }
}

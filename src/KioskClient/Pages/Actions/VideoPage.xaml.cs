using KioskClient.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace KioskClient.Pages.Actions;

/// <summary>
/// Plays a video from a URL or file path as part of an orchestration action.
/// </summary>
public sealed partial class VideoPage : Page
{
    private VideoAction? _action;

    public VideoPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is not VideoAction action) return;
        _action = action;

        try
        {
            if (!Uri.TryCreate(_action.Path, UriKind.Absolute, out var uri))
                throw new UriFormatException("The video path is not a valid URL or file path.");

            var player = new MediaPlayer
            {
                Source = MediaSource.CreateFromUri(uri),
                IsMuted = _action.IsMuted,
                IsLoopingEnabled = _action.PlayMode == VideoPlayMode.DurationLoop,
                AutoPlay = true
            };
            player.MediaFailed += Player_MediaFailed;
            player.MediaEnded += Player_MediaEnded;

            VideoDisplay.Stretch = ConvertStretch(_action.Stretch);
            VideoDisplay.SetMediaPlayer(player);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        var player = VideoDisplay.MediaPlayer;
        if (player is not null)
        {
            player.MediaFailed -= Player_MediaFailed;
            player.MediaEnded -= Player_MediaEnded;
            player.Pause();
            VideoDisplay.SetMediaPlayer(null);
            player.Dispose();
        }
    }

    private void Player_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    {
        DispatcherQueue.TryEnqueue(() => ShowError(args.ErrorMessage));
    }

    private void Player_MediaEnded(MediaPlayer sender, object args)
    {
        if (_action?.PlayMode == VideoPlayMode.PlayOnce)
            App.OrchestrationRunner.NotifyActionCompleted();
    }

    private void ShowError(string message)
    {
        VideoDisplay.Visibility = Visibility.Collapsed;
        ErrorPanel.Visibility = Visibility.Visible;
        ErrorDetail.Text = message;

        // Don't stall a play-once action on a video that can't play.
        if (_action?.PlayMode == VideoPlayMode.PlayOnce)
            App.OrchestrationRunner.NotifyActionCompleted();
    }

    private static Microsoft.UI.Xaml.Media.Stretch ConvertStretch(ImageStretch stretch)
    {
        return stretch switch
        {
            ImageStretch.None => Microsoft.UI.Xaml.Media.Stretch.None,
            ImageStretch.Fill => Microsoft.UI.Xaml.Media.Stretch.Fill,
            ImageStretch.UniformToFill => Microsoft.UI.Xaml.Media.Stretch.UniformToFill,
            _ => Microsoft.UI.Xaml.Media.Stretch.Uniform
        };
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        // Visual tree: OrchestrationPage > Grid > ActionFrame > VideoPage
        if (this.Frame.Parent is FrameworkElement parent &&
            parent.Parent is Pages.OrchestrationPage orchestrationPage)
        {
            orchestrationPage.CancelAndReturn();
        }
    }
}

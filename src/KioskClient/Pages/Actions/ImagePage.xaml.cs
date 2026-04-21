using KioskClient.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;

namespace KioskClient.Pages.Actions;

/// <summary>
/// Displays an image from a URL or file path as part of an orchestration action.
/// Shows loading/error states and a settings button overlay.
/// </summary>
public sealed partial class ImagePage : Page
{
    private ImageAction? _action;

    public ImagePage()
    {
        this.InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is not ImageAction action) return;
        _action = action;

        await LoadImageAsync();
    }

    private async Task LoadImageAsync()
    {
        if (_action is null) return;

        try
        {
            LoadingPanel.Visibility = Visibility.Visible;
            ErrorPanel.Visibility = Visibility.Collapsed;
            ImageDisplay.Visibility = Visibility.Collapsed;

            var bitmap = new BitmapImage();

            if (Uri.TryCreate(_action.Path, UriKind.Absolute, out var uri))
            {
                bitmap.UriSource = uri;
            }
            else
            {
                // Try local file
                var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(_action.Path);
                using var stream = await file.OpenReadAsync();
                await bitmap.SetSourceAsync(stream);
            }

            ImageDisplay.Source = bitmap;
            ImageDisplay.Stretch = ConvertStretch(_action.Stretch);

            LoadingPanel.Visibility = Visibility.Collapsed;
            ImageDisplay.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Visible;
            ErrorDetail.Text = ex.Message;
        }
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
        // Navigate back to cancel orchestration
        if (this.Frame.Parent is Frame parentFrame &&
            parentFrame.Parent is Pages.OrchestrationPage orchestrationPage)
        {
            orchestrationPage.CancelAndReturn();
        }
    }
}

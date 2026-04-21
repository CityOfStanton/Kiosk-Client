using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel;
using Windows.Storage;

namespace KioskClient.Dialogs;

public sealed partial class AboutDialog : ContentDialog
{
    public AboutDialog()
    {
        this.InitializeComponent();

        var version = Package.Current.Id.Version;
        VersionText.Text = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }

    private void ContentDialog_KeyUp(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Escape)
            Hide();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private async void LogsButton_Click(object sender, RoutedEventArgs e)
    {
        var localCacheFolder = ApplicationData.Current.LocalCacheFolder;
        await Windows.System.Launcher.LaunchFolderAsync(localCacheFolder);
    }
}

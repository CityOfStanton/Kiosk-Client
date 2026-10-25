using KioskClient.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace KioskClient.Dialogs;

public sealed partial class TutorialDialog : ContentDialog
{
    public TutorialDialog()
    {
        this.InitializeComponent();
        DoNotShowCheckBox.IsChecked = App.Settings.GetSetting(SettingsKeys.DoNotShowTutorial, false);
    }

    /// <summary>
    /// Shows the welcome dialog, persists the "do not show again" choice, and returns the chosen path (null if dismissed).
    /// </summary>
    public static async Task<TutorialChoice?> ShowTutorialAsync(XamlRoot xamlRoot)
    {
        var dialog = new TutorialDialog { XamlRoot = xamlRoot };
        var result = await dialog.ShowAsync();

        App.Settings.SaveSetting(SettingsKeys.DoNotShowTutorial, dialog.DoNotShowCheckBox.IsChecked == true);

        return result switch
        {
            ContentDialogResult.Primary => TutorialChoice.Create,
            ContentDialogResult.Secondary => TutorialChoice.Run,
            _ => null
        };
    }

    private void ContentDialog_KeyUp(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Escape)
            Hide();
    }
}

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace KioskClient.Dialogs;

public sealed partial class TutorialDialog : ContentDialog
{
    public bool RunTutorialOnClose { get; set; }
    public bool DoNotShowThisAgain => DoNotShowCheckBox.IsChecked == true;

    public TutorialDialog()
    {
        this.InitializeComponent();
    }

    private void ContentDialog_KeyUp(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Escape)
            Hide();
    }

    private void YesButton_Click(object sender, RoutedEventArgs e)
    {
        RunTutorialOnClose = true;
        Hide();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }
}

using KioskClient.Pages;
using KioskClient.Services;
using Microsoft.UI.Xaml;

namespace KioskClient;

/// <summary>
/// Main application window. Hosts the root frame for page navigation.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();

        // Set minimum window size
        var appWindow = this.AppWindow;
        appWindow.Title = "Kiosk Client";

        // Check if tutorial should be shown
        var showTutorial = !App.Settings.GetSetting(SettingsKeys.DoNotShowTutorial, false);

        if (showTutorial)
        {
            RootFrame.Navigate(typeof(HomePage), "showTutorial");
        }
        else
        {
            RootFrame.Navigate(typeof(HomePage));
        }
    }

    /// <summary>
    /// Navigates the root frame to the specified page.
    /// </summary>
    public void NavigateTo(Type pageType, object? parameter = null)
    {
        RootFrame.Navigate(pageType, parameter);
    }

    /// <summary>
    /// Enters full screen mode for orchestration display, hiding the taskbar.
    /// </summary>
    public void EnterFullScreen()
    {
        this.AppWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);
    }

    /// <summary>
    /// Exits full screen mode and restores the overlapped (normal) window.
    /// </summary>
    public void ExitFullScreen()
    {
        this.AppWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.Overlapped);
    }
}

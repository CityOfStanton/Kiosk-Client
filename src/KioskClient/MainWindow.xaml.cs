using KioskClient.Core.Logging;
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
        appWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico"));

        // Check if tutorial should be shown
        var showTutorial = !App.Settings.GetSetting(SettingsKeys.DoNotShowTutorial, false);
        AppLog.Info("Startup", $"Main window created: showTutorial={showTutorial}.");

        if (showTutorial)
        {
            RootFrame.Navigate(typeof(HomePage), "showTutorial");
        }
        else
        {
            RootFrame.Navigate(typeof(HomePage), "startup");
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
    /// Enters full screen mode for orchestration display, covering the Windows taskbar.
    /// </summary>
    public void EnterFullScreen()
    {
        AppLog.Debug("Window", "Entering full screen.");
        this.AppWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);
    }

    /// <summary>
    /// Exits full screen mode.
    /// </summary>
    public void ExitFullScreen()
    {
        AppLog.Debug("Window", "Exiting full screen.");
        this.AppWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.Default);
    }
}

using KioskClient.Core.Services;
using KioskClient.Services;
using KioskClient.ViewModels;
using Microsoft.UI.Xaml;

namespace KioskClient;

/// <summary>
/// Application entry point. Manages service initialization and the main window.
/// </summary>
public partial class App : Application
{
    private Window? _window;

    // Services (simple service locator for AOT compatibility)
    public static ISettingsService Settings { get; private set; } = null!;
    public static IHttpService HttpService { get; private set; } = null!;
    public static IOrchestrationLoader OrchestrationLoader { get; private set; } = null!;
    public static OrchestrationRunner OrchestrationRunner { get; private set; } = null!;
    public static SettingsViewModel SettingsVM { get; private set; } = null!;

    public static Window MainWindow => ((App)Current)._window!;

    public App()
    {
        this.InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Initialize services
        Settings = new SettingsService();
        HttpService = new HttpService();
        OrchestrationLoader = new OrchestrationLoader(HttpService);
        OrchestrationRunner = new OrchestrationRunner(OrchestrationLoader);
        SettingsVM = new SettingsViewModel(Settings, OrchestrationLoader, HttpService);

        _window = new MainWindow();
        _window.Activate();
    }
}

using KioskClient.Core.Logging;
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

    /// <summary>
    /// Opens the orchestration editor in its own window, optionally loading an existing orchestration.
    /// </summary>
    public static async Task OpenEditorAsync(KioskClient.Core.Models.Orchestration? orchestration, bool startTutorial = false)
    {
        AppLog.Info("Editor", $"Opening editor window (existing orchestration: {orchestration is not null}).");
        var editor = new EditorWindow();
        editor.BringToFront();
        if (orchestration is not null)
            await editor.LoadFromOrchestrationAsync(orchestration);
        editor.BringToFront();
        if (startTutorial)
            editor.StartTutorial();
    }

    public App()
    {
        InitializeLogging();
        this.InitializeComponent();
        this.UnhandledException += App_UnhandledException;
    }

    private static void InitializeLogging()
    {
        AppLog.Initialize(SettingsService.GetLogFolderPath());

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            AppLog.Error("App", $"Unhandled domain exception (terminating={e.IsTerminating}).", e.ExceptionObject as Exception);
            if (e.IsTerminating) AppLog.Shutdown();
        };
        TaskScheduler.UnobservedTaskException += (_, e) =>
            AppLog.Error("App", "Unobserved task exception.", e.Exception);
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            AppLog.Info("App", "Application exiting.");
            AppLog.Shutdown();
        };

        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        AppLog.Info("App", $"Application starting: version={version}, runtime={System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}, " +
                           $"os={Environment.OSVersion.Version}, arch={System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}.");
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        AppLog.Error("App", $"Unhandled UI exception: {e.Message}", e.Exception);
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        AppLog.Info("Startup", "Launching: initializing services.");

        // Initialize services
        Settings = new SettingsService();
        HttpService = new HttpService();
        OrchestrationLoader = new OrchestrationLoader(HttpService);
        OrchestrationRunner = new OrchestrationRunner(OrchestrationLoader);
        SettingsVM = new SettingsViewModel(Settings, OrchestrationLoader, HttpService);

        _window = new MainWindow();
        _window.Activate();
        AppLog.Info("Startup", "Main window activated.");
    }
}

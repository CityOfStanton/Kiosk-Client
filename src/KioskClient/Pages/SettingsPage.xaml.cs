using KioskClient.Core.Models;
using KioskClient.Dialogs;
using KioskClient.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;

namespace KioskClient.Pages;

/// <summary>
/// Settings page with NavigationView and Pivot tabs for orchestration management,
/// validation results, logging, and application settings.
/// </summary>
public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel => App.SettingsVM;

    public SettingsPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        UpdateValidationDisplay();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel.SaveState();
    }

    private async void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer is not NavigationViewItem item) return;

        switch (item.Tag?.ToString())
        {
            case "Run":
                RunOrchestration();
                break;
            case "Save":
                SaveAsStartup();
                break;
            case "Reset":
                ResetSettings();
                break;
            case "Examples":
                await ShowExamplesDialogAsync();
                break;
            case "Tutorial":
                await ShowTutorialDialogAsync();
                break;
            case "Help":
                await Windows.System.Launcher.LaunchUriAsync(
                    new Uri("https://github.com/CityOfStanton/Kiosk-Client/wiki"));
                break;
            case "About":
                await ShowAboutDialogAsync();
                break;
        }
    }

    private void RunOrchestration()
    {
        if (ViewModel.Orchestration is null || !ViewModel.CanStart) return;

        ViewModel.AddLog("Starting orchestration...");
        Frame.Navigate(typeof(OrchestrationPage), ViewModel.Orchestration);
    }

    private void SaveAsStartup()
    {
        if (ViewModel.Orchestration is null) return;

        ViewModel.SaveState();
        ViewModel.AddLog("Orchestration saved as startup configuration.");
    }

    private void ResetSettings()
    {
        ViewModel.ResetCommand.Execute(null);
    }

    private async Task ShowExamplesDialogAsync()
    {
        var dialog = new ExamplesDialog { XamlRoot = this.XamlRoot };
        await dialog.ShowAsync();
    }

    private async Task ShowTutorialDialogAsync()
    {
        var dialog = new TutorialDialog { XamlRoot = this.XamlRoot };
        await dialog.ShowAsync();
    }

    private async Task ShowAboutDialogAsync()
    {
        var dialog = new AboutDialog { XamlRoot = this.XamlRoot };
        await dialog.ShowAsync();
    }

    private async void BrowseFile_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeFilter.Add(".json");
        picker.FileTypeFilter.Add(".xml");

        // Initialize the picker with the window handle
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file is not null)
        {
            await ViewModel.LoadFromFileCommand.ExecuteAsync(file.Path);
            UpdateValidationDisplay();
        }
    }

    private void UpdateValidationDisplay()
    {
        var result = ViewModel.OrchestrationValidationResult;
        if (result is not null)
        {
            PassedCount.Text = result.PassedCount.ToString();
            FailedCount.Text = result.FailedCount.ToString();
            BuildValidationTree(result);
        }
    }

    private void BuildValidationTree(ValidationResult root)
    {
        ValidationTree.RootNodes.Clear();
        var rootNode = CreateTreeNode(root);
        ValidationTree.RootNodes.Add(rootNode);
    }

    private static TreeViewNode CreateTreeNode(ValidationResult result)
    {
        var icon = result.IsValid == true ? "\u2713" : "\u2717";
        var node = new TreeViewNode
        {
            Content = $"{icon} {result.Identifier}: {result.Message}",
            IsExpanded = true
        };

        foreach (var child in result.Children)
        {
            node.Children.Add(CreateTreeNode(child));
        }

        return node;
    }

    private void CopyLog_Click(object sender, RoutedEventArgs e)
    {
        var text = string.Join(Environment.NewLine, ViewModel.LogEntries);
        var dataPackage = new DataPackage();
        dataPackage.SetText(text);
        Clipboard.SetContent(dataPackage);
    }

    private void ClearLog_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.LogEntries.Clear();
    }
}

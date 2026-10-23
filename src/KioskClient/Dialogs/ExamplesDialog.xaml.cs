using KioskClient.Core.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace KioskClient.Dialogs;

public sealed partial class ExamplesDialog : ContentDialog
{
    public List<string> Logs { get; } = new();

    public ExamplesDialog()
    {
        this.InitializeComponent();
    }

    private void ContentDialog_KeyUp(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Escape)
            Hide();
    }

    private async void JsonButton_Click(object sender, RoutedEventArgs e)
    {
        await SaveExampleFileAsync("json");
    }

    private async void XmlButton_Click(object sender, RoutedEventArgs e)
    {
        await SaveExampleFileAsync("xml");
    }

    private async Task SaveExampleFileAsync(string format)
    {
        try
        {
            var savePicker = new FileSavePicker();

            // WinUI 3 requires the window handle for the picker
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

            if (format == "json")
            {
                savePicker.FileTypeChoices.Add("JSON Files", new List<string> { ".json" });
                savePicker.SuggestedFileName = "Settings.json";
            }
            else
            {
                savePicker.FileTypeChoices.Add("XML Files", new List<string> { ".xml" });
                savePicker.SuggestedFileName = "Settings.xml";
            }

            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                var content = format == "json"
                    ? ExampleGenerator.GenerateJsonExample()
                    : ExampleGenerator.GenerateXmlExample();

                CachedFileManager.DeferUpdates(file);
                await FileIO.WriteTextAsync(file, content);
                await CachedFileManager.CompleteUpdatesAsync(file);

                var msg = $"Saved example {format.ToUpperInvariant()} to: {file.Path}";
                Logs.Add(msg);
                StatusText.Text = msg;
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }
}

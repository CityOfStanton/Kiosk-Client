using KioskLibrary.Actions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace KioskClient
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Settings : Page
    {
        public Settings()
        {
            InitializeComponent();

            var settingsUri = Common.GetSettingsUri();
            tbSettingsUri.Text = settingsUri?.AbsoluteUri ?? "";
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            (bool isValid, string message) = await Common.VerifySettingsUri(tbSettingsUri.Text);
            tbValidation.Text = message;
            btnStart.IsEnabled = isValid;
        }

        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            Common.SaveSettingsUri(tbSettingsUri.Text);
            await Common.GetSettingsFromServer();
        }

        private Orchistration ComposeExampleOrchistration()
        {
            Orchistration orchistration = new Orchistration
            {
                Lifecycle = LifecycleBehavior.ContinuousLoop
            };

            orchistration.Actions.Add(new ImageAction(
                "Show a single image",
                30,
                "https://some-uri.com/images/the-image-to-show.jpg",
                Windows.UI.Xaml.Media.Stretch.Uniform));

            orchistration.Actions.Add(new SlideshowAction(
                "Show a slideshow",
                null,
                new List<ImageAction>()
                {
                    new ImageAction(
                        "Show the first image in the slideshow for 40 seconds.",
                        40,
                        "https://some-uri.com/images/slideshow1/slideshow-image1.jpg",
                        Windows.UI.Xaml.Media.Stretch.Fill),
                    new ImageAction(
                        "Show the second image in the slideshow for 50 seconds.",
                        50,
                        "https://some-uri.com/images/slideshow1/slideshow-image2.jpg",
                        Windows.UI.Xaml.Media.Stretch.UniformToFill)
                },
                Ordering.Sequential));

            orchistration.Actions.Add(new WebsiteAction(
                "Display the website",
                120,
                "https://some-uri.com"
                ));

            return orchistration;
        }

        private async void btnJSON_Click(object sender, RoutedEventArgs e)
        {
            var savePicker = new Windows.Storage.Pickers.FileSavePicker
            {
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary
            };
            savePicker.FileTypeChoices.Add("JSON Files", new List<string>() { ".json" });
            savePicker.SuggestedFileName = "Settings.json";

            Windows.Storage.StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                Orchistration orchistration = ComposeExampleOrchistration();
                Windows.Storage.CachedFileManager.DeferUpdates(file);
                var fileText = JsonSerializer.Serialize(orchistration, options: new JsonSerializerOptions() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                await Windows.Storage.FileIO.WriteTextAsync(file, fileText);
                await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);
            }
        }

        private async void btnXML_Click(object sender, RoutedEventArgs e)
        {
            var savePicker = new Windows.Storage.Pickers.FileSavePicker
            {
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary
            };
            savePicker.FileTypeChoices.Add("XML Files", new List<string>() { ".xml" });
            savePicker.SuggestedFileName = "Settings.xml";

            Windows.Storage.StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                Orchistration orchistration = ComposeExampleOrchistration();
                Windows.Storage.CachedFileManager.DeferUpdates(file);
                var sb = new StringBuilder();
                var sw = new StringWriter(sb);
                new XmlSerializer(typeof(Orchistration)).Serialize(sw, orchistration);
                sw.Close();
                await Windows.Storage.FileIO.WriteTextAsync(file, sb.ToString());
                await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);
            }
        }
    }
}

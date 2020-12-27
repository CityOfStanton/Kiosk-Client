using KioskLibrary.Actions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Storage.Pickers;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

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

            ApplicationView.GetForCurrentView().ExitFullScreenMode();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var settingsUri = Common.GetSettingsUri();

            if (!string.IsNullOrEmpty(settingsUri?.AbsoluteUri))
            {
                tbURLPath.Text = settingsUri.AbsoluteUri;
                await VerifyURI();
            }
        }

        /// <summary>
        /// Remove the KeyDown binding for this page
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs e) => Window.Current.CoreWindow.KeyDown -= Common.CommonKeyUp;

        /// <summary>
        /// Add the KeyDown binding back when we leave
        /// </summary>
        protected override void OnNavigatedFrom(NavigationEventArgs e) => Window.Current.CoreWindow.KeyDown += Common.CommonKeyUp;

        private async void ButtonVerify_Click(object sender, RoutedEventArgs e) => await VerifyURI();

        private async Task VerifyURI()
        {
            (bool isValid, string message) = await Common.VerifySettingsUri(tbURLPath.Text);
            tbValidation.Text = message;
            ButtonStart.IsEnabled = isValid;
        }

        private async void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            Common.SaveSettingsUri(tbURLPath.Text);
            await Common.LoadOrchestration();
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

        private async void ButtonJSON_Click(object sender, RoutedEventArgs e)
        {
            var savePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            savePicker.FileTypeChoices.Add("JSON Files", new List<string>() { ".json" });
            savePicker.SuggestedFileName = "Settings.json";

            Windows.Storage.StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                Orchistration orchistration = ComposeExampleOrchistration();
                Windows.Storage.CachedFileManager.DeferUpdates(file);
                var fileText = JsonSerializer.Serialize(orchistration, options: Common.DefaultJsonOptions);
                await Windows.Storage.FileIO.WriteTextAsync(file, fileText);
                await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);
            }
        }

        private async void ButtonXML_Click(object sender, RoutedEventArgs e)
        {
            var savePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
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

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var openPicker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary
            };
            openPicker.FileTypeFilter.Add(".json");
            openPicker.FileTypeFilter.Add(".xml");

            var file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                tbURLPath.Text = file.Path;
                await VerifyURI();
            }
        }

        private void ToggleSwitch_InputMode_Toggled(object sender, RoutedEventArgs e)
        {
            tbValidation.Text = "";
        }
    }
}

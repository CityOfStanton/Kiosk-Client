using KioskClient.ViewModels;
using KioskLibrary.Actions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using Windows.Storage.Pickers;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using KioskLibrary.Orchestration;
using KioskLibrary.Storage;
using Windows.Web.Http;
using KioskLibrary.Common;
using KioskLibrary.DataSerialization;
using KioskLibrary;
using System.Linq;

namespace KioskClient.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Settings : Page
    {
        public SettingsViewModel State { get; set; }
        public List<PollingInterval> PollingIntervals
        {
            get
            {
                return new List<PollingInterval>()
                {
                    new PollingInterval("Never", -1),
                    new PollingInterval("15 Minutes", 900),
                    new PollingInterval("30 Minutes", 1800),
                    new PollingInterval("45 Minutes", 2700),
                    new PollingInterval("1 Hour", 3600),
                    new PollingInterval("2 Hours", 7200),
                    new PollingInterval("4 Hours", 14400),
                    new PollingInterval("12 Hours", 43200),
                    new PollingInterval("1 Day", 86400)
                };
            }
        }

        public Settings()
        {
            State = new SettingsViewModel();
            try
            {
                State = ApplicationStorage.GetFromStorage<SettingsViewModel>(Constants.SettingsViewModel);
            }
            catch { }

            ApplicationView.GetForCurrentView().ExitFullScreenMode();

            InitializeComponent();
        }

        /// <summary>
        /// Remove the KeyDown binding for this page
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs e) => Window.Current.CoreWindow.KeyDown -= PagesHelper.CommonKeyUp;

        /// <summary>
        /// Add the KeyDown binding back when we leave
        /// </summary>
        protected override void OnNavigatedFrom(NavigationEventArgs e) => Window.Current.CoreWindow.KeyDown += PagesHelper.CommonKeyUp;

        private async void Button_UrlLoad_Click(object sender, RoutedEventArgs e)
        {
            (bool isValid, string message) = await Orchestrator.VerifyOrchestrationInstance(State.UriPath, HttpStatusCode.Ok);
            State.PathValidationMessage = message;
            State.IsUriPathVerified = isValid;

            if (isValid)
                State.Orchestration = await Orchestrator.GetOrchestrationInstance(new Uri(State.UriPath));

            Button_Flyout.ShowAt(sender as FrameworkElement);
        }

        private async void Button_FileLoad_Click(object sender, RoutedEventArgs e)
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
                State.IsLocalPathVerified = true;
                State.LocalPath = file.Path;
                var fileStream = await file.OpenStreamForReadAsync();
                var sr = new StreamReader(fileStream);
                var content = sr.ReadToEnd();
                sr.Close();
                fileStream.Close();

                LoadOrchestration(content);

                State.PathValidationMessage = "File verified.";
                Button_Flyout.ShowAt(sender as FrameworkElement);
            }
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e) => Save();

        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            Save();
            Start();
        }

        private void Save()
        {
            ApplicationStorage.SaveToStorage(Constants.SettingsViewModel, State);
            ApplicationStorage.SaveToStorage(Constants.PollingInterval, State.PollingInterval);
            ApplicationStorage.SaveToStorage(Constants.CurrentOrchestrationURI, State.UriPath);
            ApplicationStorage.SaveToStorage(Constants.CurrentOrchestration, State.Orchestration);
        }

        private void Start()
        {
            var rootFrame = Window.Current.Content as Frame;
            rootFrame.Navigate(typeof(MainPage));
        }

        private void LoadOrchestration(string content)
        {
            var orchestration = Orchestrator.ConvertStringToOrchestrationInstance(content);
            State.Orchestration = orchestration;
        }

        private void ComboBox_PollingInterval_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            State.PollingInterval = (sender as ComboBox).SelectedItem as PollingInterval;
        }

        #region Examples
        private OrchestrationInstance ComposeExampleOrchestration()
        {
            OrchestrationInstance orchestration = new OrchestrationInstance
            {
                Lifecycle = LifecycleBehavior.ContinuousLoop
            };

            orchestration.Actions.Add(new ImageAction(
                "Show a single image. Use multiple ImageActions to create a slideshow.",
                30,
                "https://some-uri.com/images/the-image-to-show.jpg",
                Windows.UI.Xaml.Media.Stretch.Uniform));

            orchestration.Actions.Add(new WebsiteAction(
                "Display the website",
                120,
                "https://some-uri.com",
                true,
                55,
                1.0,
                5
                ));

            return orchestration;
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
                OrchestrationInstance orchestration = ComposeExampleOrchestration();
                Windows.Storage.CachedFileManager.DeferUpdates(file);
                var fileText = Serialization.Serialize(orchestration);
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
                OrchestrationInstance orchestration = ComposeExampleOrchestration();
                Windows.Storage.CachedFileManager.DeferUpdates(file);
                var sb = new StringBuilder();
                var sw = new StringWriter(sb);
                new XmlSerializer(typeof(OrchestrationInstance)).Serialize(sw, orchestration);
                sw.Close();
                await Windows.Storage.FileIO.WriteTextAsync(file, sb.ToString());
                await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);
            }
        }
        #endregion

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (ComboBox_PollingInterval != null && State.PollingInterval != null)
                ComboBox_PollingInterval.SelectedItem = ComboBox_PollingInterval.Items?.FirstOrDefault(x => (x as PollingInterval).Seconds == State.PollingInterval.Seconds);
        }
    }
}

/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using KioskLibrary.ViewModels;
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
using KioskClient.Pages;
using KioskLibrary.Helpers;
using System.Threading.Tasks;
using KioskClient.Pages.PageArguments;

namespace KioskLibrary.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Settings : Page
    {
        private SettingsViewModel State { get; set; } // Variable name is not in _ format because it is being referenced in associated partial class
        private SettingsPageArguments _currentPageArguments;
        private readonly IHttpHelper _httpHelper;

        /// <summary>
        /// Constructor
        /// </summary>
        public Settings()
        {
            try
            {
                State = ApplicationStorage.GetFromStorage<SettingsViewModel>(Constants.ApplicationStorage.SettingsViewModel);

                if (State == null)
                    State = new SettingsViewModel();
            }
            catch { }

            State.PropertyChanged += State_PropertyChanged;

            ApplicationView.GetForCurrentView().ExitFullScreenMode();
            InitializeComponent();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="httpHelper">The <see cref="IHttpHelper"/> to use for HTTP requests</param>
        public Settings(IHttpHelper httpHelper)
                    : this()
        {
            _httpHelper = httpHelper;
        }

        /// <summary>
        /// Handles the PropertyChanged event for anything in the <see cref="State" />
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The event arguments</param>
        private void State_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "PathValidationMessage")
                Log(State.PathValidationMessage);
        }

        /// <summary>
        /// Remove the KeyDown binding for this page
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Window.Current.CoreWindow.KeyDown -= PagesHelper.CommonKeyUp;
            _currentPageArguments = e.Parameter as SettingsPageArguments;

            if (_currentPageArguments != null)
                if (_currentPageArguments.Log != null)
                    foreach (var error in _currentPageArguments.Log)
                        Log(error);
        }

        /// <summary>
        /// Add the KeyDown binding back when we leave
        /// </summary>
        protected override void OnNavigatedFrom(NavigationEventArgs e) => Window.Current.CoreWindow.KeyDown += PagesHelper.CommonKeyUp;

        private void Log(string message)
        {
            var currentTime = DateTime.Now.ToString("HH:mm:ss").PadRight(8);
            ListBox_Log.Items.Add($"{currentTime} - {message}");
            ListBox_Log.SelectedIndex = ListBox_Log.Items.Count - 1;
        }

        private async void Button_UrlLoad_Click(object sender, RoutedEventArgs e)
        {
            State.IsUriLoading = true;
            OrchestrationInstance tmpOrchestrationInstance;

            (bool isValid, string message) = await _httpHelper.ValidateURI(State.UriPath, HttpStatusCode.Ok);
            State.PathValidationMessage = message;
            State.IsUriPathVerified = isValid;

            if (isValid)
            {
                tmpOrchestrationInstance = await OrchestrationInstance.GetOrchestrationInstance(new Uri(State.UriPath), _httpHelper);
                await ValidateOrchestration(tmpOrchestrationInstance, OrchestrationSource.URL);
            }
            else
            {
                State.OrchestrationInstance = null;
                if (!string.IsNullOrEmpty(State.UriPath))
                    Log($"Unable to resolve: {State.UriPath}");
                Log("Orchestration failed validation!");
            }

            State.IsUriLoading = false;
        }

        private async Task ValidateOrchestration(OrchestrationInstance orchestrationInstance, OrchestrationSource orchestrationSource)
        {
            Log("Validating orchestration...");

            if (orchestrationInstance == null)
                Log("Orchestration has no content or is corrupt.");
            else
            {
                (bool status, List<string> errors) = await orchestrationInstance.ValidateAsync();
                if (status)
                {
                    State.OrchestrationInstance = orchestrationInstance;
                    Log("Orchestration valid!");

                    if (orchestrationSource == OrchestrationSource.File)
                        State.IsLocalPathVerified = true;
                    else
                        State.IsUriPathVerified = true;
                }
                else
                {
                    foreach (var error in errors)
                        Log(error);

                    if (orchestrationSource == OrchestrationSource.File)
                        State.IsLocalPathVerified = false;
                    else
                        State.IsUriPathVerified = false;

                    Log("Orchestration failed validation!");
                }
            }
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
                State.IsFileLoading = true;
                State.LocalPath = file.Path;
                var fileStream = await file.OpenStreamForReadAsync();
                var sr = new StreamReader(fileStream);
                var content = sr.ReadToEnd();
                sr.Close();
                fileStream.Close();

                var orchestration = OrchestrationInstance.ConvertStringToOrchestrationInstance(content);
                State.OrchestrationInstance = orchestration;

                await ValidateOrchestration(orchestration, OrchestrationSource.File);
                State.IsFileLoading = false;
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
            ApplicationStorage.SaveToStorage(Constants.ApplicationStorage.SettingsViewModel, State);
            ApplicationStorage.SaveToStorage(Constants.ApplicationStorage.CurrentOrchestrationURI, State.UriPath);
            ApplicationStorage.SaveToStorage(Constants.ApplicationStorage.CurrentOrchestration, State.OrchestrationInstance);
            ApplicationStorage.SaveToStorage(Constants.ApplicationStorage.CurrentOrchestrationSource, State.IsLocalFile ? OrchestrationSource.File : OrchestrationSource.URL);
            Log("Orchestration saved!");
        }

        private void Start()
        {
            // Navigate to the mainpage.
            // This should trigger the application startup workflow that automatically starts the orchestration.
            var rootFrame = Window.Current.Content as Frame;
            rootFrame.Navigate(typeof(MainPage));
        }

        #region Examples
        private OrchestrationInstance ComposeExampleOrchestration()
        {
            OrchestrationInstance orchestration = new OrchestrationInstance
            {
                PollingIntervalMinutes = 15,
                Order = Ordering.Sequential,
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
                var fileText = SerializationHelper.Serialize(orchestration);
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
    }
}

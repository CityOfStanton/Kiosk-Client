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
using Serilog;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

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
        private readonly IApplicationStorage _applicationStorage;
        private Queue<TeachingTip> _walkThrough;

        /// <summary>
        /// Constructor
        /// </summary>
        public Settings()
        {
            try
            {
                if (_httpHelper == null)
                    _httpHelper = new HttpHelper();

                if (_applicationStorage == null)
                    _applicationStorage = new ApplicationStorage();

                State = _applicationStorage.GetFromStorage<SettingsViewModel>(Constants.ApplicationStorage.SettingsViewModel);

                Log.Information("Settings State: {state}", SerializationHelper.JSONSerialize(State));

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
        /// <param name="applicationStorage">The <see cref="IApplicationStorage"/> to use for interacting with local application storage</param>
        public Settings(IHttpHelper httpHelper, IApplicationStorage applicationStorage)
            : this()
        {
            _httpHelper = httpHelper;
            _applicationStorage = applicationStorage;
        }

        /// <summary>
        /// Handles the PropertyChanged event for anything in the <see cref="State" />
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The event arguments</param>
        private void State_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "PathValidationMessage")
                LogToListbox(State.PathValidationMessage);
        }

        /// <summary>
        /// Remove the KeyDown binding for this page
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Log.Information("OnNavigatedTo");

            Window.Current.CoreWindow.KeyDown -= PagesHelper.CommonKeyUp;
            _currentPageArguments = e.Parameter as SettingsPageArguments;

            if (_currentPageArguments != null)
                if (_currentPageArguments.Log != null)
                    foreach (var error in _currentPageArguments.Log)
                        LogToListbox(error);
        }

        /// <summary>
        /// Add the KeyDown binding back when we leave
        /// </summary>
        protected override void OnNavigatedFrom(NavigationEventArgs e) => Window.Current.CoreWindow.KeyDown += PagesHelper.CommonKeyUp;

        private void LogToListbox(string message)
        {
            var currentTime = DateTime.Now.ToString("HH:mm:ss").PadRight(8);
            ListBox_Log.Items.Add($"{currentTime} - {message}");
            ListBox_Log.SelectedIndex = ListBox_Log.Items.Count - 1;
        }

        private async void Button_UrlLoad_Click(object sender, RoutedEventArgs e)
        {
            State.IsUriLoading = true;
            OrchestrationInstance tmpOrchestrationInstance;

            Log.Information("Button_UrlLoad_Click UriPath: {UriPath}", State.UriPath);

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
                    LogToListbox($"Unable to resolve: {State.UriPath}");
                LogToListbox("Orchestration failed validation!");
            }

            State.IsUriLoading = false;
        }

        private async Task ValidateOrchestration(OrchestrationInstance orchestrationInstance, OrchestrationSource orchestrationSource)
        {
            LogToListbox("Validating orchestration...");

            if (orchestrationInstance == null)
                LogToListbox("Orchestration has no content or is corrupt.");
            else
            {
                (bool status, List<string> errors) = await orchestrationInstance.ValidateAsync();
                if (status)
                {
                    State.OrchestrationInstance = orchestrationInstance;
                    LogToListbox("Orchestration valid!");

                    if (orchestrationSource == OrchestrationSource.File)
                        State.IsLocalPathVerified = true;
                    else
                        State.IsUriPathVerified = true;
                }
                else
                {
                    foreach (var error in errors)
                        LogToListbox(error);

                    if (orchestrationSource == OrchestrationSource.File)
                        State.IsLocalPathVerified = false;
                    else
                        State.IsUriPathVerified = false;

                    LogToListbox("Orchestration failed validation!");
                }
            }
        }

        private async void Button_FileLoad_Click(object _, RoutedEventArgs e)
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
                Log.Information("Button_FileLoad_Click FilePath: {FilePath}", file.Path);

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

        private void ButtonSave_Click(object _, RoutedEventArgs e) => Save();

        private void ButtonStart_Click(object _, RoutedEventArgs e)
        {
            Save();
            Start();
        }

        private void Save()
        {
            Log.Information("Saving Orchestration to Application Storage");

            _applicationStorage.SaveToStorage(Constants.ApplicationStorage.SettingsViewModel, State);
            _applicationStorage.SaveToStorage(Constants.ApplicationStorage.CurrentOrchestrationURI, State.UriPath);
            _applicationStorage.SaveToStorage(Constants.ApplicationStorage.CurrentOrchestration, State.OrchestrationInstance);
            _applicationStorage.SaveToStorage(Constants.ApplicationStorage.CurrentOrchestrationSource, State.IsLocalFile ? OrchestrationSource.File : OrchestrationSource.URL);
            LogToListbox("Orchestration saved!");
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
                "Show the Social Share iamge from GitHub",
                5,
                "https://raw.githubusercontent.com/CityOfStanton/Kiosk-Client/develop/logo/Kiosk-Client_GitHub%20Social%20Preview.png",
                Windows.UI.Xaml.Media.Stretch.Uniform));

            orchestration.Actions.Add(new WebsiteAction(
                "Display the Kiosk Client GitHub page",
                20,
                "https://github.com/CityOfStanton/Kiosk-Client",
                true,
                15,
                .005,
                0,
                5));

            return orchestration;
        }

        private async void ButtonJSON_Click(object _, RoutedEventArgs e)
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
                var fileText = SerializationHelper.JSONSerialize(orchestration);
                await Windows.Storage.FileIO.WriteTextAsync(file, fileText);
                await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);
            }
        }

        private async void ButtonXML_Click(object _, RoutedEventArgs e)
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
                var serializedString = SerializationHelper.XMLSerialize<OrchestrationInstance>(orchestration);
                await Windows.Storage.FileIO.WriteTextAsync(file, serializedString);
                await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);
            }
        }
        #endregion

        private void Button_Help_Click(object sender, RoutedEventArgs e)
        {
            _walkThrough = new Queue<TeachingTip>();
            _walkThrough.Enqueue(TeachTip_Wiki);
            _walkThrough.Enqueue(TeachTip_LoadFile);
            _walkThrough.Enqueue(TeachTip_SaveFilToTheWeb);
            _walkThrough.Enqueue(TeachTip_SaveLocally);
            _walkThrough.Enqueue(TeachTip_Clear);
            _walkThrough.Enqueue(TeachTip_Start);
            _walkThrough.Enqueue(TeachTip_Update);
            _walkThrough.Enqueue(TeachTip_FileLoad);
            _walkThrough.Enqueue(TeachTip_Enjoy);

            TeachTip_Welcome.IsOpen = true;
        }

        private void TeachTip_Closed(TeachingTip sender, TeachingTipClosedEventArgs args)
        {
            if (_walkThrough.Count > 0)
                _walkThrough.Dequeue().IsOpen = true;
        }

        private void Button_Clear_Click(object sender, RoutedEventArgs e)
        {
            _applicationStorage.ClearItemFromStorage(Constants.ApplicationStorage.CurrentOrchestration);
            _applicationStorage.ClearItemFromStorage(Constants.ApplicationStorage.CurrentOrchestrationSource);
            _applicationStorage.ClearItemFromStorage(Constants.ApplicationStorage.NextOrchestration);
        }

        private void ListBox_Log_Clear_Click(object sender, RoutedEventArgs e)
        {
            ListBox_Log.Items.Clear();
        }

        private void ListBox_Log_Copy_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null && ListBox_Log.SelectedItem != null)
            {
                var dataPackage = new DataPackage
                {
                    RequestedOperation = DataPackageOperation.Copy
                };
                dataPackage.SetText(ListBox_Log.SelectedItem?.ToString());
                Clipboard.SetContent(dataPackage);
            }
        }

        private async void Button_About_Click(object sender, RoutedEventArgs e)
        {
            var about = new About();
            await about.ShowAsync();
        }
    }
}

/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using KioskLibrary.ViewModels;
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
using KioskClient.Dialogs;
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
        public SettingsViewModel State { get; set; }

        private SettingsPageArguments _currentPageArguments;
        private readonly IHttpHelper _httpHelper;
        private readonly IApplicationStorage _applicationStorage;
        private Queue<TeachingTip> _walkThrough;
        private bool Walkthrough_StartingIsLocalState;

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
            }
            catch { }

            ApplicationView.GetForCurrentView().ExitFullScreenMode();
            InitializeComponent();

            State = new SettingsViewModel();

            State.PropertyChanged += State_PropertyChanged;
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

        #region Control Events

        /// <summary>
        /// Fires after the page has loaded
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The event arguments</param>
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var stateFromStorage = await _applicationStorage.GetFileFromStorageAsync<SettingsViewModel>(Constants.ApplicationStorage.Files.SettingsViewModel);
            State.IsLocalFile = stateFromStorage.IsLocalFile;
            State.LocalPath = stateFromStorage.LocalPath;
            State.OrchestrationInstance = stateFromStorage.OrchestrationInstance;
            State.UriPath = stateFromStorage.UriPath;
            SetDefaultState();

            Log.Information("Settings State: {state}", SerializationHelper.JSONSerialize(State));
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
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            Log.Information("Settings OnNavigatedTo");

            Window.Current.CoreWindow.KeyDown -= PagesHelper.CommonKeyUp;
            _currentPageArguments = e.Parameter as SettingsPageArguments;

            if (_currentPageArguments != null)
                if (_currentPageArguments.Log != null)
                    foreach (var error in _currentPageArguments.Log)
                        LogToListbox(error);

            var doNotShowTutorialOnStartup = _applicationStorage.GetSettingFromStorage<bool>(Constants.ApplicationStorage.Settings.DoNotShowTutorialOnStartup);

            if (!doNotShowTutorialOnStartup)
            {
                var tutorialDialog = new RunTutorial();
                await tutorialDialog.ShowAsync();

                _applicationStorage.SaveSettingToStorage(Constants.ApplicationStorage.Settings.DoNotShowTutorialOnStartup, tutorialDialog.DoNotShowThisAgain);

                if (tutorialDialog.RunTutorialOnClose)
                    StartTutorial();
            }
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
                LogToListbox("Orchestration failed validation");
            }

            State.IsUriLoading = false;
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

        private void TeachTip_Closed(TeachingTip sender, TeachingTipClosedEventArgs args)
        {
            if (_walkThrough.Count > 0)
            {
                var tip = _walkThrough.Dequeue();

                if (tip == TeachTip_FileInputMode)
                    State.IsLocalFile = true;
                else if (tip == TeachTip_SaveFileToTheWeb)
                    State.IsLocalFile = false;

                tip.IsOpen = true;
            }
            else
                State.IsLocalFile = Walkthrough_StartingIsLocalState; // Restore the state of the IsLocal toggle after the walkthrough.
        }

        private void ListBox_Log_Clear_Click(object sender, RoutedEventArgs e)
        {
            ListBox_Log.Items.Clear();
        }

        private void ListBox_Log_Copy_Click(object sender, RoutedEventArgs e)
        {
            if (ListBox_Log.SelectedItem != null)
            {
                var dataPackage = new DataPackage
                {
                    RequestedOperation = DataPackageOperation.Copy
                };
                dataPackage.SetText(ListBox_Log.SelectedItem?.ToString());
                Clipboard.SetContent(dataPackage);
            }
        }
        private async void ListBox_Log_Save_Click(object sender, RoutedEventArgs e)
        {
            var savePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            savePicker.FileTypeChoices.Add("Text File", new List<string>() { ".txt" });
            savePicker.SuggestedFileName = $"Kiosk_Client_Log-{DateTime.Now:yyyyMMddHHmmss}";

            Windows.Storage.StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                var fileText = "";
                foreach (var item in ListBox_Log.Items)
                    fileText += $"{item}{Environment.NewLine}";

                Windows.Storage.CachedFileManager.DeferUpdates(file);
                await Windows.Storage.FileIO.WriteTextAsync(file, fileText);
                await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);
            }
        }

        private async void NavigationView_Menu_ItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
        {
            switch (args?.InvokedItemContainer?.Tag)
            {
                case "Run":
                    await Save();
                    Run();
                    break;

                case "Save":
                    await Save();
                    break;

                case "Clear":
                    Clear();
                    break;

                case "Examples":
                    await Examples();

                    break;

                case "Tutorial":
                    StartTutorial();
                    break;

                case "Help":
                    await LaunchHelpSite();
                    break;

                case "About":
                    await LaunchAboutDialog();
                    break;
            }
        }

        #endregion

        #region Private Methods

        private void SetDefaultState()
        {
            State.IsFileLoading = false;
            State.IsUriLoading = false;
        }

        private async Task ValidateOrchestration(OrchestrationInstance orchestrationInstance, OrchestrationSource orchestrationSource)
        {
            LogToListbox("Validating orchestration...");

            if (orchestrationInstance == null)
                LogToListbox("Orchestration has no content or is corrupt");
            else
            {
                (bool status, List<string> errors) = await orchestrationInstance.ValidateAsync();
                if (status)
                {
                    State.OrchestrationInstance = orchestrationInstance;
                    LogToListbox($"Orchestration Instance: \"{orchestrationInstance.Name ?? "[No Name]"}\"");

                    if (orchestrationInstance.Actions != null)
                        foreach (var action in orchestrationInstance.Actions)
                            LogToListbox($"{action.GetType().Name}: \"{action.Name ?? "[No Name]"}\"");

                    LogToListbox($"Orchestration valid: \"{orchestrationInstance.Name ?? "[No Name]"}\"");

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

                    LogToListbox("Orchestration failed validation");
                }
            }
        }

        private async Task Examples()
        {
            var examples = new Examples();
            await examples.ShowAsync();

            foreach (var log in examples.Logs)
                LogToListbox(log);
        }

        private static async Task LaunchAboutDialog()
        {
            var about = new About();
            await about.ShowAsync();
        }

        private async Task Save()
        {
            Log.Information("Saving Orchestration to Application Storage");

            await _applicationStorage.SaveFileToStorageAsync(Constants.ApplicationStorage.Files.SettingsViewModel, State);
            await _applicationStorage.SaveFileToStorageAsync(Constants.ApplicationStorage.Files.DefaultOrchestration, State.OrchestrationInstance);

            _applicationStorage.SaveSettingToStorage(Constants.ApplicationStorage.Settings.DefaultOrchestrationURI, State.UriPath);
            _applicationStorage.SaveSettingToStorage(Constants.ApplicationStorage.Settings.DefaultOrchestrationSource, State.IsLocalFile ? OrchestrationSource.File : OrchestrationSource.URL);

            LogToListbox("Orchestration saved as startup Orchestration");
        }

        private void Run()
        {
            // Navigate to the mainpage.
            // This should trigger the application startup workflow that automatically starts the orchestration.
            var rootFrame = Window.Current.Content as Frame;
            rootFrame.Navigate(typeof(MainPage));
        }

        private void Clear()
        {
            _applicationStorage.ClearFileFromStorageAsync(Constants.ApplicationStorage.Files.DefaultOrchestration);
            _applicationStorage.ClearFileFromStorageAsync(Constants.ApplicationStorage.Files.NextOrchestration);

            _applicationStorage.ClearSettingFromStorage(Constants.ApplicationStorage.Settings.DefaultOrchestrationSource);
            _applicationStorage.ClearSettingFromStorage(Constants.ApplicationStorage.Settings.DefaultOrchestrationURI);

            State.OrchestrationInstance = null;

            LogToListbox("Startup Orchestration has been removed");
        }

        private void StartTutorial()
        {
            Walkthrough_StartingIsLocalState = State.IsLocalFile; // Capture the state of the IsLocal toggle when we start the walkthrough.
            State.IsLocalFile = false;

            _walkThrough = new Queue<TeachingTip>();
            _walkThrough.Enqueue(TeachTip_Wiki);
            _walkThrough.Enqueue(TeachTip_FileInputMode);
            _walkThrough.Enqueue(TeachTip_LoadFile);
            _walkThrough.Enqueue(TeachTip_SaveFileToTheWeb);
            _walkThrough.Enqueue(TeachTip_SaveLocally);
            _walkThrough.Enqueue(TeachTip_Clear);
            _walkThrough.Enqueue(TeachTip_Start);
            _walkThrough.Enqueue(TeachTip_Update);
            _walkThrough.Enqueue(TeachTip_FileLoad);
            _walkThrough.Enqueue(TeachTip_About);
            _walkThrough.Enqueue(TeachTip_Enjoy);

            TeachTip_Welcome.IsOpen = true;
        }

        private static async Task LaunchHelpSite()
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("https://github.com/CityOfStanton/Kiosk-Client/wiki"));
        }
        #endregion
    }
}

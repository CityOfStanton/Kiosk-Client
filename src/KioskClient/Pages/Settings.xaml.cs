﻿/*
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
using KioskLibrary.Orchestrations;
using KioskLibrary.Storage;
using KioskLibrary.Common;
using KioskClient.Dialogs;
using KioskLibrary.Helpers;
using System.Threading.Tasks;
using KioskClient.Pages.PageArguments;
using Serilog;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using System.Linq;
using Windows.Storage;

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
        private int Walkthrough_StartingPivotItem;

        private const string UrlHistoryKey = "UrlHistory";
        private const int MaxUrlHistory = 10;

        /// <summary>
        /// Constructor
        /// </summary>
        public Settings()
        {
            try
            {
                _httpHelper ??= new HttpHelper();

                _applicationStorage ??= new ApplicationStorage();
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
            State.IsLocalFile = stateFromStorage?.IsLocalFile ?? default;
            State.LocalPath = stateFromStorage?.LocalPath ?? default;
            State.Orchestration = stateFromStorage?.Orchestration ?? default;
            State.UriPath = stateFromStorage?.UriPath ?? default;
            State.IsFileLoading = false;
            State.IsUriLoading = false;

            Log.Information("Settings State: {state}", SerializationHelper.JSONSerialize(State));

            // Load URL history from settings
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey(UrlHistoryKey))
            {
                var historyString = localSettings.Values[UrlHistoryKey] as string;
                if (!string.IsNullOrEmpty(historyString))
                {
                    var urls = historyString.Split(new[] {'|'}, StringSplitOptions.RemoveEmptyEntries);
                    State.UrlHistory.Clear();
                    foreach (var url in urls)
                        State.UrlHistory.Add(url);
                }
            }
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

            //Window.Current.CoreWindow.KeyDown -= PagesHelper.CommonKeyUp;
            _currentPageArguments = e.Parameter as SettingsPageArguments;

            if (_currentPageArguments != null)
                if (_currentPageArguments.Log != null)
                    foreach (var error in _currentPageArguments.Log)
                        LogToListbox(error);

            await ValidateOrchestration(_currentPageArguments.Orchestration);

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

        private void LogToListbox(string message)
        {
            var currentTime = DateTime.Now.ToString("HH:mm:ss").PadRight(8);
            ListBox_Log.Items.Add($"{currentTime} - {message}");
            ListBox_Log.SelectedIndex = ListBox_Log.Items.Count - 1;
        }

        private async void Button_UrlLoad_Click(object sender, RoutedEventArgs e)
        {
            State.IsUriLoading = true;
            Orchestration orchestration;

            Log.Information("Button_UrlLoad_Click UriPath: {UriPath}", State.UriPath);

            orchestration = await Orchestration.GetOrchestration(new Uri(State.UriPath), _httpHelper);
            orchestration.OrchestrationSource = OrchestrationSource.URL;
            await ValidateOrchestration(orchestration);

            State.IsUriLoading = false;

            // Update URL history if load was successful
            if (!string.IsNullOrWhiteSpace(ComboBox_URLPath.Text))
            {
                var urlPath = ComboBox_URLPath.Text.Trim();
                if (State.UrlHistory.Contains(urlPath))
                    State.UrlHistory.Remove(urlPath);
                State.UrlHistory.Insert(0, urlPath);
                while (State.UrlHistory.Count > MaxUrlHistory)
                    State.UrlHistory.RemoveAt(State.UrlHistory.Count - 1);
                State.UriPath = urlPath;

                // Persist history
                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values[UrlHistoryKey] = string.Join("|", State.UrlHistory);
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

                var orchestration = Orchestration.ConvertStringToOrchestration(content);
                orchestration.OrchestrationSource = OrchestrationSource.File;
                State.Orchestration = orchestration;

                await ValidateOrchestration(orchestration);
                State.IsFileLoading = false;
            }
        }

        private void TeachingTip_Closed(TeachingTip sender, TeachingTipClosedEventArgs args)
        {
            if (_walkThrough.Count > 0)
            {
                var tip = _walkThrough.Dequeue();

                if (tip == TeachingTip_FileInputMode)
                    State.IsLocalFile = true;
                else if (tip == TeachingTip_SaveFileToTheWeb)
                    State.IsLocalFile = false;
                else if (tip == TeachingTip_Log)
                    Pivot_Orchestration.SelectedItem = PivotItem_Log;
                else if (tip == TeachingTip_Validation)
                    Pivot_Orchestration.SelectedItem = PivotItem_Validation;
                else if (tip == TeachingTip_Summary)
                    Pivot_Orchestration.SelectedItem = PivotItem_Summary;

                tip.IsOpen = true;
            }
            else
            {
                State.IsLocalFile = Walkthrough_StartingIsLocalState; // Restore the state of the IsLocal toggle after the walkthrough.
                Pivot_Orchestration.SelectedIndex = Walkthrough_StartingPivotItem; // Restore the selected pivot item
            }
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

                case "Reset":
                    Reset();
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

        private void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            Pivot_Orchestration.SelectedItem = PivotItem_Validation;
        }

        #endregion

        #region Private Methods

        private async Task ValidateOrchestration(Orchestration orchestration)
        {
            LogToListbox("Validating orchestration...");

            if (orchestration == null)
                LogToListbox("Orchestration has no content or is corrupt");
            else
            {
                await orchestration.ValidateAsync();
                State.Orchestration = orchestration;

                if (orchestration.ValidationResult.FirstOrDefault()?.IsValid ?? false)
                {
                    LogToListbox($"Orchestration: \"{orchestration.Name ?? "[No Name]"}\"");

                    if (orchestration.Actions != null)
                        foreach (var action in orchestration.Actions)
                            LogToListbox($"{action.GetType().Name}: \"{action.Name ?? "[No Name]"}\"");

                    LogToListbox($"Orchestration valid: \"{orchestration.Name ?? "[No Name]"}\"");

                    if (orchestration.OrchestrationSource == OrchestrationSource.File)
                        State.IsLocalPathVerified = true;
                    else
                        State.IsUriPathVerified = true;
                }
                else
                {
                    if (orchestration.OrchestrationSource == OrchestrationSource.File)
                        State.IsLocalPathVerified = false;
                    else
                        State.IsUriPathVerified = false;

                    Pivot_Orchestration.SelectedItem = PivotItem_Validation;

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
            await _applicationStorage.SaveFileToStorageAsync(Constants.ApplicationStorage.Files.DefaultOrchestration, State.Orchestration);

            _applicationStorage.SaveSettingToStorage(Constants.ApplicationStorage.Settings.DefaultOrchestrationURI, State.UriPath);
            _applicationStorage.SaveSettingToStorage(Constants.ApplicationStorage.Settings.DefaultOrchestrationSource, State.IsLocalFile ? OrchestrationSource.File : OrchestrationSource.URL);

            LogToListbox("Orchestration saved as startup Orchestration");
        }

        private void Run()
        {
            // Navigate to the mainpage.
            // This should trigger the application startup workflow that automatically starts the orchestration.
            var rootFrame = Window.Current.Content as Frame;
            rootFrame.Navigate(typeof(MainPage), true);
        }

        private void Reset()
        {
            _applicationStorage.ClearFileFromStorageAsync(Constants.ApplicationStorage.Files.DefaultOrchestration);
            _applicationStorage.ClearFileFromStorageAsync(Constants.ApplicationStorage.Files.NextOrchestration);

            _applicationStorage.ClearSettingFromStorage(Constants.ApplicationStorage.Settings.DefaultOrchestrationSource);
            _applicationStorage.ClearSettingFromStorage(Constants.ApplicationStorage.Settings.DefaultOrchestrationURI);

            State.Reset();

            LogToListbox("Startup Orchestration has been removed");
        }

        private void StartTutorial()
        {
            Walkthrough_StartingIsLocalState = State.IsLocalFile; // Capture the state of the IsLocal toggle when we start the walkthrough.
            Walkthrough_StartingPivotItem = Pivot_Orchestration.SelectedIndex;
            State.IsLocalFile = false;

            _walkThrough = new Queue<TeachingTip>();
            _walkThrough.Enqueue(TeachingTip_Wiki);
            _walkThrough.Enqueue(TeachingTip_FileInputMode);
            _walkThrough.Enqueue(TeachingTip_LoadFile);
            _walkThrough.Enqueue(TeachingTip_Log);
            _walkThrough.Enqueue(TeachingTip_Validation);
            _walkThrough.Enqueue(TeachingTip_Summary);
            _walkThrough.Enqueue(TeachingTip_SaveFileToTheWeb);
            _walkThrough.Enqueue(TeachingTip_SaveLocally);
            _walkThrough.Enqueue(TeachingTip_Reset);
            _walkThrough.Enqueue(TeachingTip_Start);
            _walkThrough.Enqueue(TeachingTip_Update);
            _walkThrough.Enqueue(TeachingTip_FileLoad);
            _walkThrough.Enqueue(TeachingTip_About);
            _walkThrough.Enqueue(TeachingTip_Enjoy);

            TeachingTip_Welcome.IsOpen = true;
        }

        private static async Task LaunchHelpSite()
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("https://github.com/CityOfStanton/Kiosk-Client/wiki"));
        }
        #endregion

        private void DeleteUrlHistoryItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string url)
            {
                State.UrlHistory.Remove(url);
                // Persist history
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                localSettings.Values[UrlHistoryKey] = string.Join("|", State.UrlHistory);
            }
        }
    }
}

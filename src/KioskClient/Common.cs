using KioskClient.Actions;
using KioskLibrary.Actions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Web.Http;
using Action = KioskLibrary.Actions.Action;

namespace KioskClient
{
    public class Common
    {
        public static void CommonKeyUp(object sender, Windows.UI.Core.KeyEventArgs args)
        {
            if (args.VirtualKey == Windows.System.VirtualKey.Home || args.VirtualKey == Windows.System.VirtualKey.Escape)
            {
                var rootFrame = Window.Current.Content as Frame;
                rootFrame.Navigate(typeof(MainPage), new MainPageArguments(showSetupInformation: true));
            }
        }

        public static JsonSerializerOptions DefaultJsonOptions
        {
            get
            {
                return new JsonSerializerOptions()
                {
                    AllowTrailingCommas = true,
                    WriteIndented = true,
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters =
                    {
                        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                    }
                };
            }
        }

        public async static Task<Orchistration> GetSettings()
        {
            // Get URI from local store
            var localSettings = ApplicationData.Current.LocalSettings;
            var settingsUri = localSettings.Values[Constants.SystemUriSetting];

            if (settingsUri != null)
            {
                Orchistration actionSettings = null;
                var settingsUriPath = settingsUri.ToString();
                var uri = new Uri(settingsUriPath);
                string body = null;

                if (uri.IsFile)
                {
                    if (File.Exists(settingsUriPath))
                    {
                        StreamReader sr = null;
                        try
                        {
                            sr = new StreamReader(settingsUriPath);
                            body = sr.ReadToEnd();
                        }
                        finally
                        {
                            sr?.Close();
                        }
                    }
                }
                else
                {
                    // Retreive settings from server
                    var client = new HttpClient();
                    var result = await client.GetAsync(uri);

                    if (result.StatusCode == HttpStatusCode.Ok)
                        body = await result.Content.ReadAsStringAsync();
                }

                try
                {
                    actionSettings = JsonSerializer.Deserialize<Orchistration>(body, DefaultJsonOptions);
                }
                catch (JsonException)
                {
                    using var sr = new StringReader(body);
                    try
                    {
                        actionSettings = new XmlSerializer(typeof(Orchistration)).Deserialize(sr) as Orchistration;
                    }
                    catch { }
                    finally
                    {
                        sr.Close();
                    }
                }

                return actionSettings;
            }

            return null;
        }

        public static async Task LoadOrchestration()
        {
            var orchestration = await GetSettings();
            var rootFrame = Window.Current.Content as Frame;
            if (orchestration != null)
            {
                ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
                LoadNextAction(orchestration, null, rootFrame);
            }
            else
                rootFrame.Navigate(typeof(Settings));
        }

        private static void LoadNextAction(Orchistration orchestration, Action action, Frame frame)
        {
            if (orchestration.Actions.Any())
                if (orchestration.Actions.Count == 1)
                    if (action == null)
                        NavigateToNextActionPage(orchestration.Actions[0], frame);
                    else
                        return;
                else if (orchestration.Order == Ordering.Random)
                {
                    var remainingActions = orchestration.Actions.Except(new List<Action>() { action }).ToList();
                    var nextAction = remainingActions[new Random().Next(remainingActions.Count - 1)];
                    NavigateToNextActionPage(nextAction, frame);
                }
                else if (orchestration.Order == Ordering.Sequential)
                    if (orchestration.Actions.Last() == action)
                        NavigateToNextActionPage(orchestration.Actions[0], frame);
                    else
                        NavigateToNextActionPage(orchestration.Actions[orchestration.Actions.IndexOf(action) + 1], frame);
        }

        private static void NavigateToNextActionPage(Action action, Frame frame)
        {
            Type nextPage;

            if (action is ImageAction)
                nextPage = typeof(ImagePage);
            else if (action is WebsiteAction)
                nextPage = typeof(WebsitePage);
            else if (action is SlideshowAction)
                nextPage = typeof(SlideshowPage);
            else
                throw new NotImplementedException($"The action [{action.GetType()}] is not supported.");

            frame.Navigate(nextPage, action);
        }

        public async static Task<(bool, string)> VerifySettingsUri(string settingsUri)
        {
            try
            {
                var uri = new Uri(settingsUri);
                if (uri.IsFile)
                    if (File.Exists(settingsUri))
                        return (true, "File exists!");
                    else
                        return (false, "File does not exist!");
                else
                {
                    var client = new HttpClient();
                    var result = await client.GetAsync(uri);

                    return (result.StatusCode == HttpStatusCode.Ok, "URI is valid!");
                }
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public static void SaveSettingsUri(string settingsUri)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[Constants.SystemUriSetting] = settingsUri;
        }

        public static Uri GetSettingsUri()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            var settingsUri = localSettings.Values[Constants.SystemUriSetting];
            if (!string.IsNullOrEmpty(settingsUri?.ToString()))
                return new Uri(settingsUri?.ToString());
            return null;
        }
    }
}

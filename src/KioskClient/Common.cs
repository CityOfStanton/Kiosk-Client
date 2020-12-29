using KioskClient.Actions;
using KioskClient.ViewModels;
using KioskLibrary.Actions;
using KioskLibrary.Actions.Common;
using KioskLibrary.Actions.Orchestration;
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

        internal static T GetFromStorage<T>(string setting)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values[setting] != null)
                if (typeof(T).IsPrimitive)
                    return (T)localSettings.Values[setting];
                else
                    return Deserialize<T>(localSettings.Values[setting].ToString());
            return default;
        }

        internal static void SaveToStorage(string setting, object toSave)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (toSave != null)
                if (toSave.GetType().IsPrimitive)
                    localSettings.Values[setting] = toSave;
                else
                    localSettings.Values[setting] = Serialize(toSave);
        }

        private static T Deserialize<T>(string toDeserialize) => JsonSerializer.Deserialize<T>(toDeserialize, DefaultJsonOptions);

        private static string Serialize(object toSerialize) => JsonSerializer.Serialize(toSerialize, DefaultJsonOptions);

        public async static Task<Orchestration> GetOrchestrationFromURL(Uri uri)
        {
            try
            {
                var client = new HttpClient();
                var result = await client.GetAsync(uri);
                if (result.StatusCode == HttpStatusCode.Ok)
                    return ConvertStringToOrchestration(await result.Content.ReadAsStringAsync());

            }
            catch { }

            return null;
        }

        public static Orchestration ConvertStringToOrchestration(string body)
        {
            try
            {
                // Try to parse the text as JSON
                return JsonSerializer.Deserialize<Orchestration>(body, DefaultJsonOptions);
            }
            catch (JsonException)
            {
                // Try to parse the text as XML
                using var sr = new StringReader(body);
                try
                {
                    return new XmlSerializer(typeof(Orchestration)).Deserialize(sr) as Orchestration;
                }
                catch { }
                finally
                {
                    sr.Close();
                }
            }

            return null;
        }

        public async static Task StartOrchestration()
        {
            var rootFrame = Window.Current.Content as Frame;

            // Load Orchestration from Storage
            var orchestration = GetFromStorage<Orchestration>(Constants.CurrentOrchestration);
            var orchestrationPath = GetFromStorage<string>(Constants.CurrentOrchestrationURI);

            // If we are pulling from a URL, pull a new orchestration and set up polling
            // If we are pulling from a file, use the orchestration from Storage
            if (!string.IsNullOrEmpty(orchestrationPath))
                if (Uri.TryCreate(orchestrationPath, UriKind.Absolute, out var orchestrationUri))
                    orchestration = await GetOrchestrationFromURL(orchestrationUri);
                else
                    rootFrame.Navigate(typeof(Settings));

            // Invoke the orchestration
            if (orchestration != null)
            {
                ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
                EvaluateNextAction(orchestration, null, rootFrame);
            }
            else
                rootFrame.Navigate(typeof(Settings));
        }

        private static void EvaluateNextAction(Orchestration orchestration, Action action, Frame frame)
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
                var client = new HttpClient();
                var result = await client.GetAsync(uri);

                return (result.StatusCode == HttpStatusCode.Ok, "Settings URL is valid!");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}

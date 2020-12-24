using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.Web.Http;
using Action = KioskLibrary.Actions.Action;

namespace KioskClient
{
    public class Common
    {
        public static void CommonKeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Home || e.Key == Windows.System.VirtualKey.Escape)
            {
                var rootFrame = Window.Current.Content as Frame;
                rootFrame.Navigate(typeof(MainPage), new MainPageArguments(showSetupInformation: true));
            }
        }

        public async static Task<List<Action>> GetSettingsFromServer()
        {
            // Get server URI from local store
            var localSettings = ApplicationData.Current.LocalSettings;
            var settingsUri = localSettings.Values[Constants.SystemUriSetting];

            if (settingsUri != null)
            {
                // Retreive settings from server
                var client = new HttpClient();
                var result = await client.GetAsync(new Uri(settingsUri.ToString()));

                if (result.StatusCode == HttpStatusCode.Ok)
                {
                    var body = await result.Content.ReadAsStringAsync();
                    List<Action> actionSettings = null;

                    try
                    {
                        var options = new JsonSerializerOptions()
                        {
                            AllowTrailingCommas = true,
                            PropertyNameCaseInsensitive = true
                        };

                        actionSettings = JsonSerializer.Deserialize<List<Action>>(body);
                    }
                    catch (JsonException)
                    {
                        using var sr = new StringReader(body);
                        try
                        {
                            actionSettings = new XmlSerializer(typeof(List<Action>)).Deserialize(sr) as List<Action>;
                        }
                        catch { }
                        finally
                        {
                            sr.Close();
                        }
                    }

                    return actionSettings;
                }
            }

            return null;
        }

        public async static Task<(bool, string)> VerifySettingsUri(string settingsUri)
        {
            try
            {
                var client = new HttpClient();
                var uri = new Uri(settingsUri);
                var result = await client.GetAsync(uri);

                return (result.StatusCode == HttpStatusCode.Ok, "URI is valid!");
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

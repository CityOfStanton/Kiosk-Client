using System;
using System.Threading.Tasks;
using Windows.Web.Http;

namespace KioskLibrary.Helpers
{
    public class HttpHelper
    {
        public async static Task<(bool, string)> ValidateURI(string settingsUri, HttpStatusCode expectedResult)
        {
            try
            {
                var uri = new Uri(settingsUri);
                var client = new HttpClient();
                var result = await client.GetAsync(uri);

                return (result.StatusCode == expectedResult, "URL is valid!");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}

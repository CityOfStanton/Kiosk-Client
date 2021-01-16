/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using System;
using System.Threading.Tasks;
using Windows.Web.Http;

namespace KioskLibrary.Helpers
{
    /// <summary>
    /// Helper class for HTTP related methods
    /// </summary>
    public class HttpHelper
    {
        /// <summary>
        /// Validates that <paramref name="settingsUri" /> returns <paramref name="expectedResult" />
        /// </summary>
        /// <param name="settingsUri">The settings URI</param>
        /// <param name="expectedResult">The expected result</param>
        /// <returns>A boolean indicating whether or nor the validation succeeded as well as a corresponding message</returns>
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

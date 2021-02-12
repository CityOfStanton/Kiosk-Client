/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using KioskLibrary.Common;
using System;
using System.Threading.Tasks;
using Windows.Web.Http;

namespace KioskLibrary.Helpers
{
    /// <summary>
    /// Helper class for HTTP related methods
    /// </summary>
    public class HttpHelper : IHttpHelper
    {
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Constructor
        /// </summary>
        public HttpHelper()
        {
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="httpClient">The <see cref="HttpClient"/> to use for HTTP requests</param>
        public HttpHelper(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <inheritDoc />
        public virtual async Task<HttpResponseMessage> GetAsync(Uri uri) => await _httpClient.GetAsync(uri);
                
        /// <inheritDoc />
        public virtual async Task<(bool IsValid, string Message)> ValidateURI(string settingsUri, HttpStatusCode expectedResult)
        {
            try
            {
                var uri = new Uri(settingsUri);
                var result = await GetAsync(uri);

                return (result.StatusCode == expectedResult, Constants.ValidationMessages.OrchestrationInstance.ValidURIMessage);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}

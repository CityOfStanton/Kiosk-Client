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
    public interface IHttpHelper
    {
        /// <summary>
        /// Performs a GET REST call on the specified <paramref name="uri"/>
        /// </summary>
        /// <param name="uri">The uri to target for this request</param>
        /// <returns>A <see cref="HttpResponseMessage"/></returns>
        Task<HttpResponseMessage> GetAsync(Uri uri);

        /// <summary>
        /// Validates that <paramref name="settingsUri" /> returns <paramref name="expectedResult" />
        /// </summary>
        /// <param name="settingsUri">The settings URI</param>
        /// <param name="expectedResult">The expected result</param>
        /// <returns>A boolean indicating whether or nor the validation succeeded as well as a corresponding message</returns>
        Task<(bool, string)> ValidateURI(string settingsUri, HttpStatusCode expectedResult);
    }
}

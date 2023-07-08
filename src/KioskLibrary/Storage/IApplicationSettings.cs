/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using System.Threading.Tasks;
using Windows.Foundation;

namespace KioskLibrary.Storage
{
    public interface IApplicationSettings
    {
        /// <summary>
        /// Gets the contents for a settings
        /// </summary>
        /// <typeparam name="T">The type of the setting</typeparam>
        /// <param name="key">The key for the setting</param>
        /// <returns>The contents of the setting. If no content exists, <see cref="null"/> is returned.</returns>
        T GetSettingFromStorage<T>(string key);

        /// <summary>
        /// Stores a setting in application storage where the object can be identified by <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key for the setting</param>
        /// <param name="toSave">The setting to save</param>
        /// <remarks>If the object is primitive, it is stored as-is. If it is complex, the contents are serialized to JSON and stored as a string. If your object is not capable of being serialized, you may need to save the object in parts.</remarks>
        void SaveSettingToStorage(string key, object toSave);

        /// <summary>
        /// Sets the value of the setting identified by <paramref name="key"/> to null.
        /// </summary>
        /// <param name="key">The key for the setting</param>
        void ClearSettingFromStorage(string key);

        /// <summary>
        /// Gets the contents for a file
        /// </summary>
        /// <typeparam name="T">The type of the file</typeparam>
        /// <param name="key">The key for the file</param>
        /// <returns>The contents of the file. If no content exists, <see cref="null"/> is returned.</returns>
        Task<T> GetFileFromStorageAsync<T>(string key);

        /// <summary>
        /// Stores a file in application storage where the object can be identified by <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key for the setting</param>
        /// <param name="toSave">The file to save</param>
        /// <remarks>If the object is primitive, it is stored as-is. If it is complex, the contents are serialized to JSON and stored as a string. If your object is not capable of being serialized, you may need to save the object in parts.</remarks>
        Task SaveFileToStorageAsync(string key, object toSave);

        /// <summary>
        /// Sets the value of the file identified by <paramref name="key"/> to null.
        /// </summary>
        /// <param name="key">The key for the file</param>
        Task ClearFileFromStorageAsync(string key);

        /// <summary>
        /// Clears all values from application storage
        /// </summary>
        IAsyncAction ClearStorage();
    }
}
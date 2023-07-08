/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;

namespace KioskLibrary.Storage
{
    public interface IApplicationCache
    {
        /// <summary>
        /// Gets the contents for a file
        /// </summary>
        /// <typeparam name="T">The type of the file</typeparam>
        /// <param name="key">The key for the file</param>
        /// <returns>The contents of the file. If no content exists, <see cref="null"/> is returned.</returns>
        Task<T> GetAsync<T>(string key);

        /// <summary>
        /// Stores a file in application cache where the object can be identified by <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key for the setting</param>
        /// <param name="toSave">The file to save</param>
        /// <remarks>If the object is primitive, it is stored as-is. If it is complex, the contents are serialized to JSON and stored as a string. If your object is not capable of being serialized, you may need to save the object in parts.</remarks>
        Task SaveAsync(string key, object toSave);

        /// <summary>
        /// Lists all the cache files
        /// </summary>
        /// <returns>An <see cref="IReadOnlyList{StorageFile}" /> representing all of the files in the container.</returns>
        Task<IReadOnlyList<StorageFile>> ListAllAsync();

        /// <summary>
        /// Gets the contents for a file
        /// </summary>
        /// <typeparam name="T">The type of the file</typeparam>
        /// <param name="oldKey">The key for the old file</param>
        /// <param name="newKey">The key for the new file</param>
        /// <returns>The contents of the file. If no content exists, <see cref="null"/> is returned.</returns>
        Task RenameAsync(string oldKey, string newKey);

        /// <summary>
        /// Sets the value of the file identified by <paramref name="key"/> to null.
        /// </summary>
        /// <param name="key">The key for the file</param>
        Task DeleteAsync(string key);

        /// <summary>
        /// Clears all values from application storage
        /// </summary>
        Task ClearAllAsync();
    }
}
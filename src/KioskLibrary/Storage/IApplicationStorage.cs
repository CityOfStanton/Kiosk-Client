/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using Windows.Foundation;

namespace KioskLibrary.Storage
{
    public interface IApplicationStorage
    {
        /// <summary>
        /// Gets the contents for a settings
        /// </summary>
        /// <typeparam name="T">The type of the setting</typeparam>
        /// <param name="setting">The key for the setting</param>
        /// <returns>The contents of the setting. If no content exists, <see cref="null"/> is returned.</returns>
        T GetFromStorage<T>(string setting);

        /// <summary>
        /// Stores an object in the application storage where the object can be identified by <paramref name="setting"/>.
        /// </summary>
        /// <param name="setting">The key for the setting</param>
        /// <param name="toSave">The object to save</param>
        /// <remarks>If the object is primitive, it is stored as-is. If it is complex, the contents are serialized to JSON and stored as a string. If your object is not capable of being serialized, you may need to save the object in parts.</remarks>
        void SaveToStorage(string setting, object toSave);

        /// <summary>
        /// Sets the value of the item identified by <paramref name="setting"/> to null.
        /// </summary>
        /// <param name="setting">The key for the setting</param>
        void ClearItemFromStorage(string setting);

        /// <summary>
        /// Clears all values from application storage
        /// </summary>
        IAsyncAction ClearStorage();
    }
}
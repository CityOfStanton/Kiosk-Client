/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using Windows.Foundation;
using Windows.Storage;
using KioskLibrary.Helpers;

namespace KioskLibrary.Storage
{
    /// <summary>
    /// Handles working with the Application Storage
    /// </summary>
    public class ApplicationStorage
    {
        /// <summary>
        /// The <see cref="ApplicationData"/> referenced by <see cref="ApplicationData.Current"/>
        /// </summary>
        public static ApplicationData CurrentApplicationData { get { return ApplicationData.Current; } }

        /// <summary>
        /// Gets the contents for a settings
        /// </summary>
        /// <typeparam name="T">The type of the setting</typeparam>
        /// <param name="setting">The key for the setting</param>
        /// <returns>The contents of the setting. If no content exists, <see cref="null"/> is returned.</returns>
        public static T GetFromStorage<T>(string setting)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values[setting] != null)
                if (typeof(T).IsPrimitive)
                    return (T)localSettings.Values[setting];
                else
                    return SerializationHelper.JSONDeserialize<T>(localSettings.Values[setting].ToString());
            return default;
        }

        /// <summary>
        /// Stores an object in the application storage where the object can be identified by <paramref name="setting"/>.
        /// </summary>
        /// <param name="setting">The key for the setting</param>
        /// <param name="toSave">The object to save</param>
        /// <remarks>If the object is primitive, it is stored as-is. If it is complex, the contents are serialized to JSON and stored as a string. If your object is not capable of being serialized, you may need to save the object in parts.</remarks>
        public static void SaveToStorage(string setting, object toSave)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (toSave != null)
                if (toSave.GetType().IsPrimitive)
                    localSettings.Values[setting] = toSave;
                else
                    localSettings.Values[setting] = SerializationHelper.JSONSerialize(toSave);
        }

        /// <summary>
        /// Sets the value of the item identified by <paramref name="setting"/> to null.
        /// </summary>
        /// <param name="setting">The key for the setting</param>
        public static void ClearItemFromStorage(string setting) => SaveToStorage(setting, null);

        /// <summary>
        /// Clears all values from application storage
        /// </summary>
        public static IAsyncAction ClearStorage() => ApplicationData.Current.ClearAsync();
    }
}

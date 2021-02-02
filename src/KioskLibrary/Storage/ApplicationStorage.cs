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
    public class ApplicationStorage : IApplicationStorage
    {
        /// <inheritdoc />
        public T GetFromStorage<T>(string setting)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values[setting] != null)
                if (typeof(T).IsPrimitive)
                    return (T)localSettings.Values[setting];
                else
                    return SerializationHelper.JSONDeserialize<T>(localSettings.Values[setting].ToString());
            return default;
        }

        /// <inheritdoc />
        public void SaveToStorage(string setting, object toSave)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (toSave != null)
                if (toSave.GetType().IsPrimitive)
                    localSettings.Values[setting] = toSave;
                else
                    localSettings.Values[setting] = SerializationHelper.JSONSerialize(toSave);
        }

        /// <inheritdoc />
        public void ClearItemFromStorage(string setting) => SaveToStorage(setting, null);

        /// <inheritdoc />
        public IAsyncAction ClearStorage() => ApplicationData.Current.ClearAsync();
    }
}

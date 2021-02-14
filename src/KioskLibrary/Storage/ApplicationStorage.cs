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
using System;
using System.Threading.Tasks;
using System.IO;

namespace KioskLibrary.Storage
{
    /// <summary>
    /// Handles working with the Application Storage
    /// </summary>
    public class ApplicationStorage : IApplicationStorage
    {
        /// <inheritdoc />
        public virtual T GetSettingFromStorage<T>(string key)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values[key] != null)
                if (typeof(T).IsPrimitive)
                    return (T)localSettings.Values[key];
                else
                    return SerializationHelper.JSONDeserialize<T>(localSettings.Values[key].ToString());
            return default;
        }

        /// <inheritdoc />
        public virtual void SaveSettingToStorage(string key, object toSave)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (toSave != null)
                if (toSave.GetType().IsPrimitive)
                    localSettings.Values[key] = toSave;
                else
                    localSettings.Values[key] = SerializationHelper.JSONSerialize(toSave);
        }

        /// <inheritdoc />
        public virtual void ClearSettingFromStorage(string key) => SaveSettingToStorage(key, null);

        /// <inheritdoc />
        public async virtual Task<T> GetFileFromStorageAsync<T>(string key)
        {
            try
            {
                var localCacheFolder = ApplicationData.Current.LocalCacheFolder;
                var file = await localCacheFolder.GetFileAsync(key);
                var result = await FileIO.ReadTextAsync(file);
                return SerializationHelper.JSONDeserialize<T>(result);
            }
            catch (FileNotFoundException)
            {
                return default;
            }
        }

        /// <inheritdoc />
        public async virtual Task SaveFileToStorageAsync(string key, object toSave)
        {
            var localCacheFolder = ApplicationData.Current.LocalCacheFolder;
            var file = await localCacheFolder.CreateFileAsync(key, CreationCollisionOption.ReplaceExisting);
            var serializedContent = SerializationHelper.JSONSerialize(toSave);
            await FileIO.WriteTextAsync(file, serializedContent);
        }

        /// <inheritdoc />
        public async virtual Task ClearFileFromStorageAsync(string key)
        {
            try
            {
                var localCacheFolder = ApplicationData.Current.LocalCacheFolder;
                var file = await localCacheFolder.GetFileAsync(key);
                await file.DeleteAsync();
            }
            catch (FileNotFoundException) { }
        }

        /// <inheritdoc />
        public virtual IAsyncAction ClearStorage() => ApplicationData.Current.ClearAsync();
    }
}

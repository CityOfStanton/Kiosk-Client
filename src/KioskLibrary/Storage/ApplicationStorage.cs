/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using Windows.Foundation;
using KioskLibrary.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

namespace KioskLibrary.Storage
{
    /// <summary>
    /// Handles working with the Application Storage
    /// </summary>
    public class ApplicationStorage : IApplicationStorage
    {
        private static readonly string _basePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KioskClient");

        private static readonly string _settingsFilePath = Path.Combine(_basePath, "settings.json");
        private static readonly string _cacheFolderPath = Path.Combine(_basePath, "LocalCache");

        private readonly object _settingsLock = new();

       /// <summary>
       /// Constructor
       /// </summary>
       public ApplicationStorage()
        {
            Directory.CreateDirectory(_basePath);
            Directory.CreateDirectory(_cacheFolderPath);
        }

        /// <inheritdoc />
        public virtual T GetSettingFromStorage<T>(string key)
        {
            lock (_settingsLock)
            {
                var settings = ReadSettings;
                if (settings.TryGetValue(key, out var value) && value != null)
                    if (typeof(T).IsPrimitive)
                        return (T)Convert.ChangeType(value, typeof(T));
                    else
                        return SerializationHelper.JSONDeserialize<T>(value.ToString());
                return default;
            }
        }

        /// <inheritdoc />
        public virtual void SaveSettingToStorage(string key, object toSave)
        {
            lock (_settingsLock)
            {
                var settings = ReadSettings;
                if (toSave == null)
                    settings.Remove(key);
                else if (toSave.GetType().IsPrimitive)
                    settings[key] = toSave;
                else
                    settings[key] = SerializationHelper.JSONSerialize(toSave);
                WriteSettings(settings);
            }
        }

        /// <inheritdoc />
        public virtual void ClearSettingFromStorage(string key) => SaveSettingToStorage(key, null);

        /// <inheritdoc />
        public async virtual Task<T> GetFileFromStorageAsync<T>(string key)
        {
            try
            {
                var filePath = Path.Combine(_cacheFolderPath, key);
                var result = await File.ReadAllTextAsync(filePath);
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
            var filePath = Path.Combine(_cacheFolderPath, key);
            var serializedContent = SerializationHelper.JSONSerialize(toSave);
            await File.WriteAllTextAsync(filePath, serializedContent);
        }

        /// <inheritdoc />
        public async virtual Task ClearFileFromStorageAsync(string key)
        {
            try
            {
                var filePath = Path.Combine(_cacheFolderPath, key);
                await Task.Run(() => File.Delete(filePath));
            }
            catch (FileNotFoundException) { }
        }

        /// <inheritdoc />
        public virtual Task ClearStorageAsync()
        {
            return Task.Run(() =>
            {
                if (Directory.Exists(_basePath))
                {
                    Directory.Delete(_basePath, true);
                    Directory.CreateDirectory(_basePath);
                    Directory.CreateDirectory(_cacheFolderPath);
                }
            });
        }

        private static Dictionary<string, object> ReadSettings
        {
            get
            {
                if (!File.Exists(_settingsFilePath))
                    return [];

                var json = File.ReadAllText(_settingsFilePath);
                return string.IsNullOrWhiteSpace(json)
                    ? []
                    : SerializationHelper.JSONDeserialize<Dictionary<string, object>>(json)
                      ?? [];
            }
        }

        private static void WriteSettings(Dictionary<string, object> settings)
        {
            var json = SerializationHelper.JSONSerialize(settings);
            File.WriteAllText(_settingsFilePath, json);
        }
    }
}

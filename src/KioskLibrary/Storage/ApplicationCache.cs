/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using Windows.Storage;
using KioskLibrary.Helpers;
using System;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using KioskLibrary.Common;

namespace KioskLibrary.Storage
{
    /// <summary>
    /// Handles working with the Local Cache
    /// </summary>
    public class ApplicationCache : IApplicationCache
    {
        private readonly StorageFolder _folder;

        private ApplicationCache(StorageFolder folder) => _folder = folder;

        /// <summary>
        /// Constructor
        /// </summary>
        public static async Task<ApplicationCache> CreateApplicationCacheInstance(CacheStore store)
        {
            StorageFolder folder = store switch
            {
                CacheStore.OrchestrationCache => await ApplicationData.Current.LocalCacheFolder.CreateFolderAsync(Constants.ApplicationStorage.Files.CacheFolders.OrchestrationCache, CreationCollisionOption.OpenIfExists),
                CacheStore.Temporary => await ApplicationData.Current.LocalCacheFolder.CreateFolderAsync(Constants.ApplicationStorage.Files.CacheFolders.Temporary, CreationCollisionOption.OpenIfExists),
                _ => throw new NotImplementedException($"CacheStore location not supported: {store}"),
            };
            return new ApplicationCache(folder);
        }

        /// <inheritdoc />
        public async virtual Task<StorageFile> CreateAsync(string key) => await _folder.CreateFileAsync(key, CreationCollisionOption.ReplaceExisting);

        /// <inheritdoc />
        public async virtual Task<T> GetAsync<T>(string key)
        {
            try
            {
                var file = await _folder.GetFileAsync(key);
                var result = await FileIO.ReadTextAsync(file);
                return SerializationHelper.JSONDeserialize<T>(result);
            }
            catch (FileNotFoundException)
            {
                return default;
            }
        }

        /// <inheritdoc />
        public async virtual Task<IReadOnlyList<StorageFile>> ListAllAsync()
        {
            try
            {
                var files = await _folder.GetFilesAsync();
                return files;
            }
            catch (UnauthorizedAccessException)
            {
                return default;
            }
        }

        /// <inheritdoc />
        public async virtual Task SaveAsync(string key, object toSave)
        {
            var file = await _folder.CreateFileAsync(key, CreationCollisionOption.ReplaceExisting);
            var serializedContent = SerializationHelper.JSONSerialize(toSave);
            await FileIO.WriteTextAsync(file, serializedContent);
        }

        /// <inheritdoc />
        public async virtual Task RenameAsync(string oldKey, string newKey)
        {
            var file = await _folder.GetFileAsync(oldKey);
            await file.RenameAsync(newKey, NameCollisionOption.ReplaceExisting);
        }

        /// <inheritdoc />
        public async virtual Task DeleteAsync(string key)
        {
            try
            {
                var file = await _folder.GetFileAsync(key);
                await file.DeleteAsync();
            }
            catch (FileNotFoundException) { }
        }

        /// <inheritdoc />
        public async virtual Task ClearAllAsync()
        {
            var allFiles = await ListAllAsync();
            foreach (var f in allFiles)
                await f.DeleteAsync();
        }
    }
}

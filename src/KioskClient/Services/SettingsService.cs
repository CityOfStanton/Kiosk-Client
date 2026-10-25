using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using KioskClient.Core.Logging;
using Windows.Storage;
using AppDataStorage = Microsoft.Windows.Storage.ApplicationData;

namespace KioskClient.Services;

/// <summary>
/// Provides persistent storage for application settings and cached files
/// using Windows application storage APIs.
/// </summary>
public interface ISettingsService
{
    /// <summary>Gets a setting value from local storage.</summary>
    T? GetSetting<T>(string key, T? defaultValue = default);

    /// <summary>Saves a setting value to local storage.</summary>
    void SaveSetting<T>(string key, T value);

    /// <summary>Removes a setting from local storage.</summary>
    void RemoveSetting(string key);

    /// <summary>Gets a complex object from a cached file.</summary>
    Task<T?> GetFileAsync<T>(string key);

    /// <summary>Saves a complex object to a cached file.</summary>
    Task SaveFileAsync<T>(string key, T value);

    /// <summary>Removes a cached file.</summary>
    Task RemoveFileAsync(string key);
}

/// <summary>
/// Settings service using Windows App SDK storage APIs for packaged and unpackaged WinUI 3 apps.
/// </summary>
public class SettingsService : ISettingsService
{
    private static JsonTypeInfo<T> TypeInfo<T>() =>
        SettingsJsonContext.Default.GetTypeInfo(typeof(T)) as JsonTypeInfo<T>
        ?? throw new NotSupportedException($"{typeof(T)} is not registered in SettingsJsonContext.");

    // Lazily determined once at runtime
    private static readonly bool IsPackaged = CheckPackageIdentity();

    private static bool CheckPackageIdentity()
    {
        try { _ = AppDataStorage.GetDefault(); return true; }
        catch (InvalidOperationException) { return false; }
    }

    // Packaged path: Windows App SDK ApplicationData
    private static Microsoft.Windows.Storage.ApplicationDataContainer? LocalSettings =>
        IsPackaged ? AppDataStorage.GetDefault().LocalSettings : null;

    // Packaged: Windows App SDK LocalCacheFolder; Unpackaged: %LOCALAPPDATA%\KioskClient\cache
    private StorageFolder LocalCacheFolder =>
        IsPackaged
            ? AppDataStorage.GetDefault().LocalCacheFolder
            : GetUnpackagedCacheFolder();

    // Unpackaged fallback path: %LOCALAPPDATA%\KioskClient\settings.json
    private static readonly string UnpackagedSettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KioskClient", "settings.json");

    private static readonly string UnpackagedCachePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KioskClient", "cache");

    /// <summary>
    /// Gets the folder path where the app writes its cache/log files,
    /// for both packaged and unpackaged execution.
    /// </summary>
    public static string GetCacheFolderPath()
    {
        if (IsPackaged)
            return AppDataStorage.GetDefault().LocalCacheFolder.Path;

        Directory.CreateDirectory(UnpackagedCachePath);
        return UnpackagedCachePath;
    }

    /// <summary>
    /// Gets the folder where the daily debug log files are written (a Debug folder inside the cache folder).
    /// </summary>
    public static string GetLogFolderPath()
    {
        var path = Path.Combine(GetCacheFolderPath(), "Debug");
        Directory.CreateDirectory(path);
        return path;
    }

    private static StorageFolder GetUnpackagedCacheFolder()
    {
        Directory.CreateDirectory(UnpackagedCachePath);
        return StorageFolder.GetFolderFromPathAsync(UnpackagedCachePath).GetAwaiter().GetResult();
    }

    private readonly Dictionary<string, object?> _unpackagedCache = LoadUnpackagedSettings();

    private static Dictionary<string, object?> LoadUnpackagedSettings()
    {
        try
        {
            if (!File.Exists(UnpackagedSettingsPath)) return [];
            var json = File.ReadAllText(UnpackagedSettingsPath);
            var loaded = JsonSerializer.Deserialize(json, SettingsJsonContext.Default.DictionaryStringObject);
            return loaded is null ? [] : new Dictionary<string, object?>(loaded!);
        }
        catch (Exception ex)
        {
            AppLog.Warn("Settings", "Could not read the settings file; using defaults.", ex);
            return [];
        }
    }

    private void SaveUnpackagedSettings()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(UnpackagedSettingsPath)!);
        File.WriteAllText(UnpackagedSettingsPath,
            JsonSerializer.Serialize((Dictionary<string, object>)_unpackagedCache!, SettingsJsonContext.Default.DictionaryStringObject));
    }

    public T? GetSetting<T>(string key, T? defaultValue = default)
    {
        object? raw;
        if (IsPackaged)
        {
            if (!LocalSettings!.Values.TryGetValue(key, out raw)) return defaultValue;
        }
        else
        {
            if (!_unpackagedCache.TryGetValue(key, out raw)) return defaultValue;
        }

        if (raw is T typed) return typed;
        if (raw is string json)
        {
            try { return JsonSerializer.Deserialize(json, TypeInfo<T>()); }
            catch (Exception ex)
            {
                AppLog.Warn("Settings", $"Could not deserialize setting '{key}'; using default.", ex);
                return defaultValue;
            }
        }
        if (raw is JsonElement el)
        {
            try
            {
                // Complex values are stored as JSON text inside a JSON string.
                if (el.ValueKind == JsonValueKind.String && typeof(T) != typeof(string))
                    return JsonSerializer.Deserialize(el.GetString()!, TypeInfo<T>());

                return el.Deserialize(TypeInfo<T>());
            }
            catch (Exception ex)
            {
                AppLog.Warn("Settings", $"Could not deserialize setting '{key}'; using default.", ex);
                return defaultValue;
            }
        }
        return defaultValue;
    }

    public void SaveSetting<T>(string key, T value)
    {
        if (IsPackaged)
        {
            LocalSettings!.Values[key] = (value is string or int or long or double or float or bool)
                ? (object?)value
                : JsonSerializer.Serialize(value, TypeInfo<T>());
        }
        else
        {
            _unpackagedCache[key] = (value is string or int or long or double or float or bool)
                ? (object?)value
                : JsonSerializer.Serialize(value, TypeInfo<T>());
            SaveUnpackagedSettings();
        }
    }

    public void RemoveSetting(string key)
    {
        if (IsPackaged) LocalSettings!.Values.Remove(key);
        else { _unpackagedCache.Remove(key); SaveUnpackagedSettings(); }
    }

    public async Task<T?> GetFileAsync<T>(string key)
    {
        try
        {
            var file = await LocalCacheFolder.GetFileAsync(SanitizeFileName(key));
            var content = await FileIO.ReadTextAsync(file);
            return JsonSerializer.Deserialize(content, TypeInfo<T>());
        }
        catch (FileNotFoundException)
        {
            AppLog.Debug("Settings", $"Cache file '{key}' not found.");
            return default;
        }
        catch (Exception ex)
        {
            AppLog.Error("Settings", $"Failed to read cache file '{key}'.", ex);
            throw;
        }
    }

    public async Task SaveFileAsync<T>(string key, T value)
    {
        try
        {
            var file = await LocalCacheFolder.CreateFileAsync(
                SanitizeFileName(key),
                CreationCollisionOption.ReplaceExisting);
            var json = JsonSerializer.Serialize(value, TypeInfo<T>());
            await FileIO.WriteTextAsync(file, json);
        }
        catch (Exception ex)
        {
            AppLog.Error("Settings", $"Failed to write cache file '{key}'.", ex);
            throw;
        }
    }

    public async Task RemoveFileAsync(string key)
    {
        try
        {
            var file = await LocalCacheFolder.GetFileAsync(SanitizeFileName(key));
            await file.DeleteAsync();
        }
        catch (FileNotFoundException)
        {
            // Already gone
        }
        catch (Exception ex)
        {
            AppLog.Error("Settings", $"Failed to remove cache file '{key}'.", ex);
            throw;
        }
    }

    private static string SanitizeFileName(string key)
    {
        // Remove characters invalid in file names
        foreach (var c in Path.GetInvalidFileNameChars())
            key = key.Replace(c, '_');
        return key + ".json";
    }
}

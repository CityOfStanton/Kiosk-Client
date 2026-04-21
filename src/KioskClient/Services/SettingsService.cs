using System.Text.Json;
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
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    // Lazily determined once at runtime
    private static readonly bool IsPackaged = CheckPackageIdentity();

    private static bool CheckPackageIdentity()
    {
        try { _ = AppDataStorage.GetDefault(); return true; }
        catch (InvalidOperationException) { return false; }
    }

    // Packaged path: Windows App SDK ApplicationData
    private Microsoft.Windows.Storage.ApplicationDataContainer? LocalSettings =>
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

    private static StorageFolder GetUnpackagedCacheFolder()
    {
        Directory.CreateDirectory(UnpackagedCachePath);
        return StorageFolder.GetFolderFromPathAsync(UnpackagedCachePath).GetAwaiter().GetResult();
    }

    private Dictionary<string, object?> _unpackagedCache = LoadUnpackagedSettings();

    private static Dictionary<string, object?> LoadUnpackagedSettings()
    {
        try
        {
            if (!File.Exists(UnpackagedSettingsPath)) return new();
            var json = File.ReadAllText(UnpackagedSettingsPath);
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(json, JsonOptions) ?? new();
        }
        catch { return new(); }
    }

    private void SaveUnpackagedSettings()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(UnpackagedSettingsPath)!);
        File.WriteAllText(UnpackagedSettingsPath,
            JsonSerializer.Serialize(_unpackagedCache, JsonOptions));
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
            try { return JsonSerializer.Deserialize<T>(json, JsonOptions); }
            catch { return defaultValue; }
        }
        if (raw is JsonElement el)
        {
            try { return el.Deserialize<T>(JsonOptions); }
            catch { return defaultValue; }
        }
        return defaultValue;
    }

    public void SaveSetting<T>(string key, T value)
    {
        if (IsPackaged)
        {
            LocalSettings!.Values[key] = (value is string or int or long or double or float or bool)
                ? (object?)value
                : JsonSerializer.Serialize(value, JsonOptions);
        }
        else
        {
            _unpackagedCache[key] = (value is string or int or long or double or float or bool)
                ? (object?)value
                : JsonSerializer.Serialize(value, JsonOptions);
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
            return JsonSerializer.Deserialize<T>(content, JsonOptions);
        }
        catch (FileNotFoundException)
        {
            return default;
        }
    }

    public async Task SaveFileAsync<T>(string key, T value)
    {
        var file = await LocalCacheFolder.CreateFileAsync(
            SanitizeFileName(key),
            CreationCollisionOption.ReplaceExisting);
        var json = JsonSerializer.Serialize(value, JsonOptions);
        await FileIO.WriteTextAsync(file, json);
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
    }

    private static string SanitizeFileName(string key)
    {
        // Remove characters invalid in file names
        foreach (var c in Path.GetInvalidFileNameChars())
            key = key.Replace(c, '_');
        return key + ".json";
    }
}

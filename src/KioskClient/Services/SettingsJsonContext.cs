using System.Text.Json;
using System.Text.Json.Serialization;

namespace KioskClient.Services;

/// <summary>Source-generated JSON metadata for every type <see cref="SettingsService"/> can persist.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
[JsonSerializable(typeof(Dictionary<string, object?>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(float))]
[JsonSerializable(typeof(bool))]
internal sealed partial class SettingsJsonContext : JsonSerializerContext;

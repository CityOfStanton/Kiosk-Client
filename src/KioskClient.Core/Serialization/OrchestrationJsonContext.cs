using System.Text.Json;
using System.Text.Json.Serialization;
using KioskClient.Core.Models;

namespace KioskClient.Core.Serialization;

/// <summary>Source-generated JSON metadata so orchestration (de)serialization is trim- and AOT-safe.</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    WriteIndented = true)]
[JsonSerializable(typeof(Orchestration))]
internal sealed partial class OrchestrationJsonContext : JsonSerializerContext;

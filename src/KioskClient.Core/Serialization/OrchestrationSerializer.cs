using System.Text.Json;
using System.Xml;
using System.Xml.Serialization;
using KioskClient.Core.Models;

namespace KioskClient.Core.Serialization;

/// <summary>
/// Handles serialization and deserialization of orchestration files
/// in both JSON and XML formats for legacy compatibility.
/// </summary>
public static class OrchestrationSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    /// <summary>
    /// Deserializes an orchestration from a JSON string.
    /// </summary>
    public static Orchestration? DeserializeJson(string json)
    {
        return JsonSerializer.Deserialize<Orchestration>(json, JsonOptions);
    }

    /// <summary>
    /// Serializes an orchestration to a JSON string.
    /// </summary>
    public static string SerializeJson(Orchestration orchestration)
    {
        return JsonSerializer.Serialize(orchestration, JsonOptions);
    }

    /// <summary>
    /// Deserializes an orchestration from an XML string.
    /// </summary>
    public static Orchestration? DeserializeXml(string xml)
    {
        var serializer = new XmlSerializer(typeof(Orchestration));
        using var reader = new StringReader(xml);
        return serializer.Deserialize(reader) as Orchestration;
    }

    /// <summary>
    /// Serializes an orchestration to an XML string.
    /// </summary>
    public static string SerializeXml(Orchestration orchestration)
    {
        var serializer = new XmlSerializer(typeof(Orchestration));
        using var writer = new StringWriter();
        using var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings
        {
            Indent = true,
            OmitXmlDeclaration = false
        });
        serializer.Serialize(xmlWriter, orchestration);
        return writer.ToString();
    }

    /// <summary>
    /// Attempts to deserialize content as JSON first, then XML if JSON fails.
    /// </summary>
    public static Orchestration? Deserialize(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        var trimmed = content.TrimStart();

        // Try JSON first if it looks like JSON
        if (trimmed.StartsWith('{'))
        {
            try
            {
                return DeserializeJson(content);
            }
            catch (JsonException)
            {
                // Fall through to XML
            }
        }

        // Try XML
        if (trimmed.StartsWith('<'))
        {
            try
            {
                return DeserializeXml(content);
            }
            catch (InvalidOperationException)
            {
                // XML deserialization failed
            }
        }

        return null;
    }
}

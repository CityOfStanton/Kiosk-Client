using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace KioskClient.Core.Models;

/// <summary>
/// An action that displays an image from a URL or file path.
/// </summary>
public class ImageAction : ActionBase
{
    /// <summary>URL or file path to the image.</summary>
    [JsonPropertyName("path")]
    [XmlElement("Path")]
    public string Path { get; set; } = string.Empty;

    /// <summary>How to stretch the image within the display area.</summary>
    [JsonPropertyName("stretch")]
    [JsonConverter(typeof(JsonStringEnumConverter<ImageStretch>))]
    [XmlElement("Stretch")]
    public ImageStretch Stretch { get; set; } = ImageStretch.Uniform;

    /// <inheritdoc/>
    public override List<ValidationResult> Validate()
    {
        var results = base.Validate();

        results.Add(ValidatePath(Path, "Set the Path property to a valid URL or file path."));

        return results;
    }
}

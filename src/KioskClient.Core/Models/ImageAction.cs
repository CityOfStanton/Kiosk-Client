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
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [XmlElement("Stretch")]
    public ImageStretch Stretch { get; set; } = ImageStretch.Uniform;

    /// <inheritdoc/>
    public override List<ValidationResult> Validate()
    {
        var results = base.Validate();

        if (string.IsNullOrWhiteSpace(Path))
        {
            results.Add(new ValidationResult
            {
                Identifier = $"{Name} - Path",
                IsValid = false,
                Message = "Path is required.",
                Guidance = "Set the Path property to a valid URL or file path."
            });
        }
        else
        {
            results.Add(new ValidationResult
            {
                Identifier = $"{Name} - Path",
                IsValid = true,
                Message = "Path is set."
            });
        }

        return results;
    }
}

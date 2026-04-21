using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace KioskClient.Core.Models;

/// <summary>
/// Base class for all orchestration actions. Each action represents a single
/// content display step in an orchestration.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ImageAction), "KioskLibrary.Actions.ImageAction, KioskLibrary")]
[JsonDerivedType(typeof(WebsiteAction), "KioskLibrary.Actions.WebsiteAction, KioskLibrary")]
[XmlInclude(typeof(ImageAction))]
[XmlInclude(typeof(WebsiteAction))]
public abstract class ActionBase
{
    /// <summary>Display name of the action.</summary>
    [JsonPropertyName("name")]
    [XmlElement("Name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Version of this action definition.</summary>
    [JsonPropertyName("version")]
    [XmlElement("Version")]
    public string Version { get; set; } = "1.0";

    /// <summary>Duration in seconds to display this action.</summary>
    [JsonPropertyName("duration")]
    [XmlElement("Duration")]
    public int? Duration { get; set; }

    /// <summary>
    /// Validates this action and returns a list of validation results.
    /// </summary>
    public virtual List<ValidationResult> Validate()
    {
        var results = new List<ValidationResult>();

        if (Duration is null or <= 0)
        {
            results.Add(new ValidationResult
            {
                Identifier = $"{Name} - Duration",
                IsValid = false,
                Message = "Duration must be greater than 0.",
                Guidance = "Set the Duration property to a positive integer (seconds)."
            });
        }
        else
        {
            results.Add(new ValidationResult
            {
                Identifier = $"{Name} - Duration",
                IsValid = true,
                Message = "Duration is valid."
            });
        }

        return results;
    }
}

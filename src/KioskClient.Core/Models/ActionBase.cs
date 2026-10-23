using System.Text.Json.Serialization;
using System.Xml.Serialization;
using KioskClient.Core.Services;

namespace KioskClient.Core.Models;

/// <summary>
/// Base class for all orchestration actions. Each action represents a single
/// content display step in an orchestration.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ImageAction), "KioskLibrary.Actions.ImageAction, KioskLibrary")]
[JsonDerivedType(typeof(WebsiteAction), "KioskLibrary.Actions.WebsiteAction, KioskLibrary")]
[JsonDerivedType(typeof(VideoAction), "KioskLibrary.Actions.VideoAction, KioskLibrary")]
[XmlInclude(typeof(ImageAction))]
[XmlInclude(typeof(WebsiteAction))]
[XmlInclude(typeof(VideoAction))]
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

    /// <summary>True when the owning orchestration was loaded from the network (set during validation).</summary>
    [JsonIgnore]
    [XmlIgnore]
    public bool IsRemoteSource { get; set; }

    /// <summary>Validates a path/URL against the security policy.</summary>
    protected ValidationResult ValidatePath(string? path, string emptyGuidance)
    {
        if (string.IsNullOrWhiteSpace(path))
            return new ValidationResult { Identifier = $"{Name} - Path", IsValid = false, Message = "Path is required.", Guidance = emptyGuidance };

        var error = SecurityPolicy.CheckPath(path, IsRemoteSource);
        return error is null
            ? new ValidationResult { Identifier = $"{Name} - Path", IsValid = true, Message = "Path is valid." }
            : new ValidationResult { Identifier = $"{Name} - Path", IsValid = false, Message = error, Guidance = "Use an https:// URL or, for local orchestrations, an absolute local path." };
    }

    /// <summary>Names of deprecated fields that were present when this action was loaded.</summary>
    public virtual IReadOnlyList<string> GetDeprecatedFields() => [];

    /// <summary>Removes deprecated fields and returns a description of each conversion that was made.</summary>
    public virtual List<string> RemoveDeprecatedFields() => [];

    /// <summary>Validates that a seconds value is within 0..MaxSeconds.</summary>
    protected ValidationResult ValidateSecondsRange(string field, int value, int min)
    {
        var ok = value >= min && value <= SecurityPolicy.MaxSeconds;
        return new ValidationResult
        {
            Identifier = $"{Name} - {field}",
            IsValid = ok,
            Message = ok ? $"{field} is valid." : $"{field} must be between {min} and {SecurityPolicy.MaxSeconds}.",
            Guidance = ok ? string.Empty : $"Set {field} between {min} and {SecurityPolicy.MaxSeconds} seconds."
        };
    }

    /// <summary>
    /// Validates this action and returns a list of validation results.
    /// </summary>
    public virtual List<ValidationResult> Validate()
    {
        var results = new List<ValidationResult>();

        if (!RequiresDuration)
        {
            return results;
        }

        if (Duration > SecurityPolicy.MaxSeconds)
        {
            results.Add(ValidateSecondsRange("Duration", Duration.Value, 1));
        }
        else if (Duration is null or <= 0)
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

    /// <summary>Whether this action needs a Duration to determine when it ends.</summary>
    protected virtual bool RequiresDuration => true;
}

using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace KioskClient.Core.Models;

/// <summary>Determines how a video action ends.</summary>
public enum VideoPlayMode
{
    /// <summary>Loop the video until the action's Duration expires.</summary>
    DurationLoop,

    /// <summary>Play the video once and end the action when it finishes.</summary>
    PlayOnce
}

/// <summary>
/// An action that plays a video from a URL or file path (for example an mp4).
/// </summary>
public class VideoAction : ActionBase
{
    /// <summary>URL or file path to the video.</summary>
    [JsonPropertyName("path")]
    [XmlElement("Path")]
    public string Path { get; set; } = string.Empty;

    /// <summary>How to stretch the video within the display area.</summary>
    [JsonPropertyName("stretch")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [XmlElement("Stretch")]
    public ImageStretch Stretch { get; set; } = ImageStretch.Uniform;

    /// <summary>Whether the video audio is muted.</summary>
    [JsonPropertyName("isMuted")]
    [XmlElement("IsMuted")]
    public bool IsMuted { get; set; }

    /// <summary>
    /// How the action ends. DurationLoop loops the video until Duration expires;
    /// PlayOnce plays the video a single time and ends when it finishes (Duration is ignored).
    /// </summary>
    [JsonPropertyName("playMode")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [XmlElement("PlayMode")]
    public VideoPlayMode PlayMode { get; set; } = VideoPlayMode.DurationLoop;

    /// <inheritdoc/>
    protected override bool RequiresDuration => PlayMode == VideoPlayMode.DurationLoop;

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
                Guidance = "Set the Path property to a valid URL or file path to a video (for example an .mp4 file)."
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

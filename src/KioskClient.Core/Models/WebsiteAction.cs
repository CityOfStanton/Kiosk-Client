using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace KioskClient.Core.Models;

/// <summary>
/// An action that displays a website using an embedded WebView2 browser.
/// </summary>
public class WebsiteAction : ActionBase
{
    /// <summary>URL of the website to display.</summary>
    [JsonPropertyName("path")]
    [XmlElement("Path")]
    public string Path { get; set; } = string.Empty;

    /// <summary>Whether auto-scrolling is enabled.</summary>
    [JsonPropertyName("autoScroll")]
    [XmlElement("AutoScroll")]
    public bool AutoScroll { get; set; }

    /// <summary>Seconds to scroll through the page content.</summary>
    [JsonPropertyName("scrollingTime")]
    [XmlElement("ScrollingTime")]
    public int ScrollingTime { get; set; }

    /// <summary>Seconds to wait after reaching the bottom before resetting to the top.</summary>
    [JsonPropertyName("scrollingResetDelay")]
    [XmlElement("ScrollingResetDelay")]
    public int ScrollingResetDelay { get; set; }

    /// <summary>Seconds to show the settings/exit button overlay.</summary>
    [JsonPropertyName("settingsDisplayTime")]
    [XmlElement("SettingsDisplayTime")]
    public int SettingsDisplayTime { get; set; } = 5;

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
                Guidance = "Set the Path property to a valid URL."
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

        if (AutoScroll && ScrollingTime <= 0)
        {
            results.Add(new ValidationResult
            {
                Identifier = $"{Name} - ScrollingTime",
                IsValid = false,
                Message = "ScrollingTime must be greater than 0 when AutoScroll is enabled.",
                Guidance = "Set ScrollingTime to a positive integer (seconds)."
            });
        }

        if (AutoScroll && ScrollingResetDelay < 0)
        {
            results.Add(new ValidationResult
            {
                Identifier = $"{Name} - ScrollingResetDelay",
                IsValid = false,
                Message = "ScrollingResetDelay must be 0 or greater.",
                Guidance = "Set ScrollingResetDelay to 0 or a positive integer (seconds)."
            });
        }

        if (SettingsDisplayTime < 1)
        {
            results.Add(new ValidationResult
            {
                Identifier = $"{Name} - SettingsDisplayTime",
                IsValid = false,
                Message = "SettingsDisplayTime must be at least 1 second.",
                Guidance = "Set SettingsDisplayTime to 1 or greater."
            });
        }

        return results;
    }
}

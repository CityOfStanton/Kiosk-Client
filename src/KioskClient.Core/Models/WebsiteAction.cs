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

        var pathResult = ValidatePath(Path, "Set the Path property to a valid URL.");
        if (pathResult.IsValid == true && !Uri.TryCreate(Path, UriKind.Absolute, out _))
        {
            pathResult.IsValid = false;
            pathResult.Message = "Website path must be an absolute URL.";
            pathResult.Guidance = "Use an https:// URL.";
        }
        results.Add(pathResult);

        if (AutoScroll)
        {
            var scrolling = ValidateSecondsRange("ScrollingTime", ScrollingTime, 1);
            if (scrolling.IsValid == false) results.Add(scrolling);

            var resetDelay = ValidateSecondsRange("ScrollingResetDelay", ScrollingResetDelay, 0);
            if (resetDelay.IsValid == false) results.Add(resetDelay);
        }

        var settingsTime = ValidateSecondsRange("SettingsDisplayTime", SettingsDisplayTime, 1);
        if (settingsTime.IsValid == false) results.Add(settingsTime);

        return results;
    }
}

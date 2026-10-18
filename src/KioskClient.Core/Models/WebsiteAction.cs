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

    /// <summary>Multiplier applied to the default scroll speed. 1 = default, 2 = twice as fast, 0.5 = half speed.</summary>
    [JsonPropertyName("scrollSpeed")]
    [XmlElement("ScrollSpeed")]
    public double ScrollSpeed { get; set; } = 1.0;

    /// <summary>Seconds to wait after the page loads before auto-scrolling begins.</summary>
    [JsonPropertyName("scrollingStartDelay")]
    [XmlElement("ScrollingStartDelay")]
    public int ScrollingStartDelay { get; set; }

    /// <summary>Deprecated and ignored. Auto-scroll now runs at a fixed speed for the whole action.</summary>
    [JsonPropertyName("scrollingTime")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [XmlElement("ScrollingTime")]
    public int? ScrollingTime { get; set; }

    /// <summary>Deprecated and ignored.</summary>
    [JsonPropertyName("scrollingResetDelay")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [XmlElement("ScrollingResetDelay")]
    public int? ScrollingResetDelay { get; set; }

    /// <inheritdoc/>
    public override IReadOnlyList<string> GetDeprecatedFields()
    {
        var fields = new List<string>();
        if (ScrollingTime is not null) fields.Add("scrollingTime");
        if (ScrollingResetDelay is not null) fields.Add("scrollingResetDelay");
        return fields;
    }

    /// <inheritdoc/>
    public override List<string> RemoveDeprecatedFields()
    {
        var changes = new List<string>();
        if (ScrollingTime is not null)
        {
            changes.Add($"'{Name}': removed deprecated 'scrollingTime'. Auto-scroll now runs continuously at a fixed speed; use 'scrollSpeed' to adjust it.");
            ScrollingTime = null;
        }
        if (ScrollingResetDelay is not null)
        {
            changes.Add($"'{Name}': removed deprecated 'scrollingResetDelay'. Scrolling no longer resets; use 'scrollingStartDelay' to delay the start.");
            ScrollingResetDelay = null;
        }
        return changes;
    }

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
            var startDelay = ValidateSecondsRange("ScrollingStartDelay", ScrollingStartDelay, 0);
            if (startDelay.IsValid == false) results.Add(startDelay);

            if (double.IsNaN(ScrollSpeed) || ScrollSpeed < 0.1 || ScrollSpeed > 10)
            {
                results.Add(new ValidationResult
                {
                    Identifier = $"{Name} - ScrollSpeed",
                    IsValid = false,
                    Message = "ScrollSpeed must be between 0.1 and 10.",
                    Guidance = "Use 1 for the default speed, 2 for twice as fast, or 0.5 for half speed."
                });
            }
        }

        var settingsTime = ValidateSecondsRange("SettingsDisplayTime", SettingsDisplayTime, 1);
        if (settingsTime.IsValid == false) results.Add(settingsTime);

        return results;
    }
}

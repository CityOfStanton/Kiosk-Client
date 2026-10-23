using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace KioskClient.Core.Models;

/// <summary>
/// Represents a complete orchestration configuration that defines what content
/// to display, in what order, and how to cycle through it.
/// </summary>
[XmlRoot("Orchestration")]
public class Orchestration
{
    /// <summary>Display name of the orchestration.</summary>
    [JsonPropertyName("name")]
    [XmlElement("Name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Version of the orchestration format.</summary>
    [JsonPropertyName("version")]
    [XmlElement("Version")]
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Read-only stub for the deprecated <c>pollingInterval</c> field.
    /// This field is no longer used; polling timing is derived from action durations.
    /// Kept for deserialization compatibility with existing orchestration files.
    /// </summary>
    [JsonPropertyName("pollingInterval")]
    [XmlElement("PollingInterval")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public int PollingInterval { get; set; }

    /// <summary>
    /// Read-only stub for the deprecated <c>pollingIntervalMinutes</c> field.
    /// This field is no longer used; polling timing is derived from action durations.
    /// Kept for deserialization compatibility with existing orchestration files.
    /// </summary>
    [Obsolete("pollingIntervalMinutes is no longer used. Polling is derived from action durations.")]
    [JsonIgnore]
    [XmlElement("PollingIntervalMinutes")]
    public int PollingIntervalMinutes { get; set; }

    /// <summary>
    /// Controls whether <c>PollingIntervalMinutes</c> is emitted during XML serialization.
    /// Always returns <see langword="false"/> so the deprecated field is never written.
    /// </summary>
    public bool ShouldSerializePollingIntervalMinutes() => false;

    /// <summary>
    /// Controls whether <c>PollingInterval</c> is emitted during XML serialization.
    /// Always returns <see langword="false"/> so the deprecated field is never written.
    /// </summary>
    public bool ShouldSerializePollingInterval() => false;

    /// <summary>
    /// Indicates whether the loaded orchestration file contained a deprecated polling field
    /// (<c>pollingInterval</c> or <c>pollingIntervalMinutes</c>). These fields are ignored at runtime.
    /// </summary>
    [JsonIgnore]
    [XmlIgnore]
    public bool UsedDeprecatedPollingField { get; set; }

    /// <summary>
    /// Defines behavior after all actions have been displayed.
    /// </summary>
    [JsonPropertyName("lifecycle")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [XmlElement("Lifecycle")]
    public LifecycleBehavior Lifecycle { get; set; } = LifecycleBehavior.ContinuousLoop;

    /// <summary>
    /// Defines the order in which actions are displayed.
    /// </summary>
    [JsonPropertyName("order")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [XmlElement("Order")]
    public Ordering Order { get; set; } = Ordering.Sequential;

    /// <summary>
    /// The list of actions to display as part of this orchestration.
    /// </summary>
    [JsonPropertyName("actions")]
    [XmlArray("Actions")]
    [XmlArrayItem("Action")]
    public List<ActionBase> Actions { get; set; } = [];

    /// <summary>
    /// Source from which this orchestration was loaded (not serialized in file).
    /// </summary>
    [JsonIgnore]
    [XmlIgnore]
    public OrchestrationSource Source { get; set; }

    /// <summary>
    /// The URI or file path from which this orchestration was loaded (not serialized in file).
    /// </summary>
    [JsonIgnore]
    [XmlIgnore]
    public string SourcePath { get; set; } = string.Empty;

    /// <summary>
    /// SHA256 hash of the raw content this orchestration was deserialized from.
    /// Used to detect whether the remote file has changed between polling intervals.
    /// </summary>
    [JsonIgnore]
    [XmlIgnore]
    public string? ContentHash { get; set; }

    /// <summary>
    /// Total runtime of all actions combined.
    /// </summary>
    [JsonIgnore]
    [XmlIgnore]
    public TimeSpan TotalRuntime =>
        TimeSpan.FromSeconds(Actions.Where(a => a.Duration.HasValue).Sum(a => a.Duration!.Value));

    /// <summary>
    /// Validates the orchestration and all its actions.
    /// </summary>
    public ValidationResult Validate()
    {
        var root = new ValidationResult
        {
            Identifier = Name,
            IsValid = true,
            Message = "Orchestration validation"
        };

        // Validate actions exist
        if (Actions.Count == 0)
        {
            root.Children.Add(new ValidationResult
            {
                Identifier = "Actions",
                IsValid = false,
                Message = "No actions defined.",
                Guidance = "Add at least one action to the orchestration."
            });
            root.IsValid = false;
        }

        // Validate each action
        var isRemote = Source == OrchestrationSource.URL && !string.IsNullOrEmpty(SourcePath);
        foreach (var action in Actions)
        {
            action.IsRemoteSource = isRemote;
            var actionResult = new ValidationResult
            {
                Identifier = action.Name,
                IsValid = true,
                Message = $"Action: {action.Name}"
            };

            var actionValidations = action.Validate();
            actionResult.Children.AddRange(actionValidations);

            if (actionValidations.Any(v => v.IsValid == false))
            {
                actionResult.IsValid = false;
                root.IsValid = false;
            }

            root.Children.Add(actionResult);
        }

        return root;
    }
}

using KioskClient.Core.Models;
using KioskClient.Core.Serialization;

namespace KioskClient.Core.Services;

/// <summary>
/// Generates example orchestration files in JSON and XML formats.
/// </summary>
public static class ExampleGenerator
{
    /// <summary>
    /// Creates a sample orchestration with typical actions.
    /// </summary>
    public static Orchestration CreateSampleOrchestration(string format)
    {
        return new Orchestration
        {
            Name = $"Example demo project for {format}",
            Version = "1.0",
            PollingInterval = 900,
            Lifecycle = LifecycleBehavior.ContinuousLoop,
            Order = Ordering.Sequential,
            Actions =
            [
                new ImageAction
                {
                    Name = "Show the Kiosk Client Social Share image from GitHub",
                    Version = "1.0",
                    Duration = 5,
                    Path = "https://raw.githubusercontent.com/CityOfStanton/Kiosk-Client/main/logo/Kiosk-Client_GitHub%20Social%20Preview.png",
                    Stretch = ImageStretch.Uniform
                },
                new WebsiteAction
                {
                    Name = "Display the Kiosk Client GitHub page",
                    Version = "1.0",
                    Duration = 20,
                    Path = "https://github.com/CityOfStanton/Kiosk-Client",
                    AutoScroll = true,
                    ScrollingTime = 15,
                    ScrollingResetDelay = 5,
                    SettingsDisplayTime = 5
                }
            ]
        };
    }

    /// <summary>
    /// Generates a JSON example orchestration string.
    /// </summary>
    public static string GenerateJsonExample()
    {
        var orchestration = CreateSampleOrchestration("JSON");
        return OrchestrationSerializer.SerializeJson(orchestration);
    }

    /// <summary>
    /// Generates an XML example orchestration string.
    /// </summary>
    public static string GenerateXmlExample()
    {
        var orchestration = CreateSampleOrchestration("XML");
        return OrchestrationSerializer.SerializeXml(orchestration);
    }
}

using KioskClient.Core.Models;
using KioskClient.Core.Serialization;
using Xunit;

namespace KioskClient.Tests;

public class OrchestrationSerializerTests
{
    private static readonly string SampleJson = """
        {
          "name": "Example demo project for JSON",
          "version": "1.0",
          "pollingIntervalMinutes": 15,
          "lifecycle": "ContinuousLoop",
          "order": "Sequential",
          "actions": [
            {
              "$type": "KioskLibrary.Actions.ImageAction, KioskLibrary",
              "path": "https://example.com/image.png",
              "stretch": "Uniform",
              "name": "Image Action",
              "version": "1.0",
              "duration": 5
            },
            {
              "$type": "KioskLibrary.Actions.WebsiteAction, KioskLibrary",
              "path": "https://example.com",
              "autoScroll": true,
              "scrollingTime": 15,
              "scrollingResetDelay": 5,
              "settingsDisplayTime": 5,
              "name": "Website Action",
              "version": "1.0",
              "duration": 20
            }
          ]
        }
        """;

    [Fact]
    public void DeserializeJson_ValidJson_ReturnsOrchestration()
    {
        var result = OrchestrationSerializer.DeserializeJson(SampleJson);

        Assert.NotNull(result);
        Assert.Equal("Example demo project for JSON", result.Name);
        Assert.Equal("1.0", result.Version);
        Assert.Equal(15, result.PollingIntervalMinutes);
        Assert.Equal(LifecycleBehavior.ContinuousLoop, result.Lifecycle);
        Assert.Equal(Ordering.Sequential, result.Order);
        Assert.Equal(2, result.Actions.Count);
    }

    [Fact]
    public void DeserializeJson_PreservesPolymorphicActions()
    {
        var result = OrchestrationSerializer.DeserializeJson(SampleJson);

        Assert.NotNull(result);
        Assert.IsType<ImageAction>(result.Actions[0]);
        Assert.IsType<WebsiteAction>(result.Actions[1]);

        var image = (ImageAction)result.Actions[0];
        Assert.Equal("https://example.com/image.png", image.Path);
        Assert.Equal(ImageStretch.Uniform, image.Stretch);
        Assert.Equal(5, image.Duration);

        var website = (WebsiteAction)result.Actions[1];
        Assert.Equal("https://example.com", website.Path);
        Assert.True(website.AutoScroll);
        Assert.Equal(15, website.ScrollingTime);
        Assert.Equal(5, website.ScrollingResetDelay);
        Assert.Equal(5, website.SettingsDisplayTime);
    }

    [Fact]
    public void SerializeJson_RoundTrip_PreservesData()
    {
        var original = CreateSampleOrchestration();

        var json = OrchestrationSerializer.SerializeJson(original);
        var deserialized = OrchestrationSerializer.DeserializeJson(json);

        Assert.NotNull(deserialized);
        Assert.Equal(original.Name, deserialized.Name);
        Assert.Equal(original.Actions.Count, deserialized.Actions.Count);
        Assert.IsType<ImageAction>(deserialized.Actions[0]);
        Assert.IsType<WebsiteAction>(deserialized.Actions[1]);
    }

    [Fact]
    public void SerializeJson_ContainsTypeDiscriminator()
    {
        var orchestration = CreateSampleOrchestration();
        var json = OrchestrationSerializer.SerializeJson(orchestration);

        Assert.Contains("$type", json);
        Assert.Contains("KioskLibrary.Actions.ImageAction, KioskLibrary", json);
        Assert.Contains("KioskLibrary.Actions.WebsiteAction, KioskLibrary", json);
    }

    [Fact]
    public void DeserializeXml_ValidXml_ReturnsOrchestration()
    {
        var xml = OrchestrationSerializer.SerializeXml(CreateSampleOrchestration());
        var result = OrchestrationSerializer.DeserializeXml(xml);

        Assert.NotNull(result);
        Assert.Equal("Test Orchestration", result.Name);
        Assert.Equal(2, result.Actions.Count);
        Assert.IsType<ImageAction>(result.Actions[0]);
        Assert.IsType<WebsiteAction>(result.Actions[1]);
    }

    [Fact]
    public void SerializeXml_RoundTrip_PreservesData()
    {
        var original = CreateSampleOrchestration();

        var xml = OrchestrationSerializer.SerializeXml(original);
        var deserialized = OrchestrationSerializer.DeserializeXml(xml);

        Assert.NotNull(deserialized);
        Assert.Equal(original.Name, deserialized.Name);
        Assert.Equal(original.PollingIntervalMinutes, deserialized.PollingIntervalMinutes);
        Assert.Equal(original.Lifecycle, deserialized.Lifecycle);
        Assert.Equal(original.Order, deserialized.Order);
        Assert.Equal(original.Actions.Count, deserialized.Actions.Count);
    }

    [Fact]
    public void Deserialize_AutoDetectsJson()
    {
        var result = OrchestrationSerializer.Deserialize(SampleJson);

        Assert.NotNull(result);
        Assert.Equal("Example demo project for JSON", result.Name);
    }

    [Fact]
    public void Deserialize_AutoDetectsXml()
    {
        var xml = OrchestrationSerializer.SerializeXml(CreateSampleOrchestration());
        var result = OrchestrationSerializer.Deserialize(xml);

        Assert.NotNull(result);
        Assert.Equal("Test Orchestration", result.Name);
    }

    [Fact]
    public void Deserialize_InvalidContent_ReturnsNull()
    {
        var result = OrchestrationSerializer.Deserialize("not valid content");
        Assert.Null(result);
    }

    [Fact]
    public void Deserialize_EmptyString_ReturnsNull()
    {
        Assert.Null(OrchestrationSerializer.Deserialize(""));
        Assert.Null(OrchestrationSerializer.Deserialize("  "));
    }

    private static Orchestration CreateSampleOrchestration()
    {
        return new Orchestration
        {
            Name = "Test Orchestration",
            Version = "1.0",
            PollingIntervalMinutes = 15,
            Lifecycle = LifecycleBehavior.ContinuousLoop,
            Order = Ordering.Sequential,
            Actions =
            [
                new ImageAction
                {
                    Name = "Test Image",
                    Duration = 5,
                    Path = "https://example.com/image.png",
                    Stretch = ImageStretch.Uniform
                },
                new WebsiteAction
                {
                    Name = "Test Website",
                    Duration = 20,
                    Path = "https://example.com",
                    AutoScroll = true,
                    ScrollingTime = 15,
                    ScrollingResetDelay = 5,
                    SettingsDisplayTime = 5
                }
            ]
        };
    }
}

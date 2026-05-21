using KioskClient.Core.Models;
using KioskClient.Core.Serialization;
using KioskClient.Core.Services;
using Xunit;

namespace KioskClient.Tests;

public class ExampleGeneratorTests
{
    [Fact]
    public void CreateSampleOrchestration_ReturnsValidOrchestration()
    {
        var orchestration = ExampleGenerator.CreateSampleOrchestration("Test");

        Assert.NotNull(orchestration);
        Assert.Contains("Test", orchestration.Name);
        Assert.Equal(900, orchestration.PollingInterval);
        Assert.Equal(LifecycleBehavior.ContinuousLoop, orchestration.Lifecycle);
        Assert.Equal(Ordering.Sequential, orchestration.Order);
        Assert.Equal(2, orchestration.Actions.Count);
    }

    [Fact]
    public void CreateSampleOrchestration_ContainsBothActionTypes()
    {
        var orchestration = ExampleGenerator.CreateSampleOrchestration("Test");

        Assert.IsType<ImageAction>(orchestration.Actions[0]);
        Assert.IsType<WebsiteAction>(orchestration.Actions[1]);
    }

    [Fact]
    public void CreateSampleOrchestration_PassesValidation()
    {
        var orchestration = ExampleGenerator.CreateSampleOrchestration("Test");
        var result = orchestration.Validate();

        Assert.True(result.IsFullyValid);
    }

    [Fact]
    public void GenerateJsonExample_ReturnsValidJson()
    {
        var json = ExampleGenerator.GenerateJsonExample();

        Assert.NotNull(json);
        Assert.StartsWith("{", json.TrimStart());

        var deserialized = OrchestrationSerializer.DeserializeJson(json);
        Assert.NotNull(deserialized);
        Assert.Equal(2, deserialized.Actions.Count);
    }

    [Fact]
    public void GenerateXmlExample_ReturnsValidXml()
    {
        var xml = ExampleGenerator.GenerateXmlExample();

        Assert.NotNull(xml);
        Assert.Contains("<Orchestration", xml);

        var deserialized = OrchestrationSerializer.DeserializeXml(xml);
        Assert.NotNull(deserialized);
        Assert.Equal(2, deserialized.Actions.Count);
    }

    [Fact]
    public void GenerateJsonExample_MatchesLegacyFormat()
    {
        var json = ExampleGenerator.GenerateJsonExample();

        // Must contain legacy $type discriminators for backward compatibility
        Assert.Contains("$type", json);
        Assert.Contains("KioskLibrary.Actions.ImageAction, KioskLibrary", json);
        Assert.Contains("KioskLibrary.Actions.WebsiteAction, KioskLibrary", json);
    }
}

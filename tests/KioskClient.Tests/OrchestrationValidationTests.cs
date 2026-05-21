using KioskClient.Core.Models;
using Xunit;

namespace KioskClient.Tests;

public class OrchestrationValidationTests
{
    [Fact]
    public void Validate_ValidOrchestration_ReturnsValid()
    {
        var orchestration = new Orchestration
        {
            Name = "Test",
            PollingInterval = 900,
            Actions =
            [
                new ImageAction { Name = "Image", Duration = 5, Path = "https://example.com/img.png" }
            ]
        };

        var result = orchestration.Validate();

        Assert.True(result.IsFullyValid);
    }

    [Fact]
    public void Validate_PollingIntervalTooLow_ReturnsInvalid()
    {
        var orchestration = new Orchestration
        {
            Name = "Test",
            PollingInterval = 30,
            Actions =
            [
                new ImageAction { Name = "Image", Duration = 5, Path = "https://example.com/img.png" }
            ]
        };

        var result = orchestration.Validate();

        Assert.False(result.IsFullyValid);
    }

    [Fact]
    public void Validate_NoActions_ReturnsInvalid()
    {
        var orchestration = new Orchestration
        {
            Name = "Empty",
            PollingInterval = 900,
            Actions = []
        };

        var result = orchestration.Validate();

        Assert.False(result.IsFullyValid);
    }

    [Fact]
    public void TotalRuntime_SumsActionDurations()
    {
        var orchestration = new Orchestration
        {
            Actions =
            [
                new ImageAction { Duration = 5 },
                new WebsiteAction { Duration = 20 },
                new ImageAction { Duration = 10 }
            ]
        };

        Assert.Equal(TimeSpan.FromSeconds(35), orchestration.TotalRuntime);
    }

    [Fact]
    public void TotalRuntime_SkipsNullDurations()
    {
        var orchestration = new Orchestration
        {
            Actions =
            [
                new ImageAction { Duration = 5 },
                new ImageAction { Duration = null },
                new ImageAction { Duration = 10 }
            ]
        };

        Assert.Equal(TimeSpan.FromSeconds(15), orchestration.TotalRuntime);
    }
}

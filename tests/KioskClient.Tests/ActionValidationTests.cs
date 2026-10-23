using KioskClient.Core.Models;
using Xunit;

namespace KioskClient.Tests;

public class ActionValidationTests
{
    [Fact]
    public void ImageAction_ValidAction_ReturnsAllValid()
    {
        var action = new ImageAction
        {
            Name = "Test Image",
            Duration = 5,
            Path = "https://example.com/image.png"
        };

        var results = action.Validate();

        Assert.All(results, r => Assert.True(r.IsValid));
    }

    [Fact]
    public void ImageAction_MissingPath_ReturnsInvalid()
    {
        var action = new ImageAction
        {
            Name = "Test Image",
            Duration = 5,
            Path = ""
        };

        var results = action.Validate();

        Assert.Contains(results, r => r.IsValid == false && r.Identifier.Contains("Path"));
    }

    [Fact]
    public void ImageAction_NullDuration_ReturnsInvalid()
    {
        var action = new ImageAction
        {
            Name = "Test Image",
            Duration = null,
            Path = "https://example.com/image.png"
        };

        var results = action.Validate();

        Assert.Contains(results, r => r.IsValid == false && r.Identifier.Contains("Duration"));
    }

    [Fact]
    public void ImageAction_ZeroDuration_ReturnsInvalid()
    {
        var action = new ImageAction
        {
            Name = "Test Image",
            Duration = 0,
            Path = "https://example.com/image.png"
        };

        var results = action.Validate();

        Assert.Contains(results, r => r.IsValid == false && r.Identifier.Contains("Duration"));
    }

    [Fact]
    public void WebsiteAction_ValidAction_ReturnsAllValid()
    {
        var action = new WebsiteAction
        {
            Name = "Test Website",
            Duration = 20,
            Path = "https://example.com",
            AutoScroll = true,
            ScrollingTime = 15,
            ScrollingResetDelay = 5,
            SettingsDisplayTime = 5
        };

        var results = action.Validate();

        Assert.All(results, r => Assert.True(r.IsValid));
    }

    [Fact]
    public void WebsiteAction_MissingPath_ReturnsInvalid()
    {
        var action = new WebsiteAction
        {
            Name = "Test",
            Duration = 10,
            Path = ""
        };

        var results = action.Validate();

        Assert.Contains(results, r => r.IsValid == false && r.Identifier.Contains("Path"));
    }
}

using KioskClient.Core.Models;
using KioskClient.Core.Serialization;
using KioskClient.Core.Services;
using Xunit;

namespace KioskClient.Tests;

public class OrchestrationLoaderTests
{
    [Fact]
    public void LoadFromContent_ValidJson_ReturnsOrchestration()
    {
        var json = ExampleGenerator.GenerateJsonExample();
        var loader = new OrchestrationLoader(new FakeHttpService());

        var result = loader.LoadFromContent(json);

        Assert.NotNull(result);
        Assert.Equal(2, result.Actions.Count);
    }

    [Fact]
    public void LoadFromContent_ValidXml_ReturnsOrchestration()
    {
        var xml = ExampleGenerator.GenerateXmlExample();
        var loader = new OrchestrationLoader(new FakeHttpService());

        var result = loader.LoadFromContent(xml);

        Assert.NotNull(result);
        Assert.Equal(2, result.Actions.Count);
    }

    [Fact]
    public void LoadFromContent_InvalidContent_ThrowsInvalidOperation()
    {
        var loader = new OrchestrationLoader(new FakeHttpService());

        Assert.Throws<InvalidOperationException>(() => loader.LoadFromContent("garbage data"));
    }

    [Fact]
    public async Task LoadFromUrlAsync_InvalidUrl_ThrowsArgumentException()
    {
        var loader = new OrchestrationLoader(new FakeHttpService());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            loader.LoadFromUrlAsync("not a url"));
    }

    [Fact]
    public async Task LoadFromUrlAsync_ValidUrl_SetsSourceProperties()
    {
        var json = ExampleGenerator.GenerateJsonExample();
        var fakeHttp = new FakeHttpService { ResponseContent = json };
        var loader = new OrchestrationLoader(fakeHttp);

        var result = await loader.LoadFromUrlAsync("https://example.com/settings.json");

        Assert.NotNull(result);
        Assert.Equal(OrchestrationSource.URL, result.Source);
        Assert.Equal("https://example.com/settings.json", result.SourcePath);
    }

    [Fact]
    public async Task LoadFromFileAsync_NonExistentFile_ThrowsFileNotFound()
    {
        var loader = new OrchestrationLoader(new FakeHttpService());

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            loader.LoadFromFileAsync(@"C:\nonexistent\file.json"));
    }

    /// <summary>
    /// Simple fake HTTP service for testing without network calls.
    /// </summary>
    private class FakeHttpService : IHttpService
    {
        public string ResponseContent { get; set; } = "{}";

        public Task<string> GetStringAsync(Uri uri, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ResponseContent);
        }

        public Task<ValidationResult> ValidateUriAsync(string uri, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ValidationResult { Identifier = uri, IsValid = true });
        }
    }
}

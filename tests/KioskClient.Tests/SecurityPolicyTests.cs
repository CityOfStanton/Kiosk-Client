using KioskClient.Core.Models;
using KioskClient.Core.Serialization;
using KioskClient.Core.Services;
using Xunit;

namespace KioskClient.Tests;

[Collection("SecurityPolicy")]
public class SecurityPolicyTests
{
    [Fact]
    public void CheckPath_Http_BlockedByDefault_AllowedWhenDisabled()
    {
        SecurityPolicy.BlockHttp = true;
        Assert.NotNull(SecurityPolicy.CheckPath("http://example.com/a.png", false));
        try
        {
            SecurityPolicy.BlockHttp = false;
            Assert.Null(SecurityPolicy.CheckPath("http://example.com/a.png", false));
        }
        finally { SecurityPolicy.BlockHttp = true; }
    }

    [Theory]
    [InlineData(@"C:\images\a.png")]
    [InlineData(@"\\server\share\a.png")]
    [InlineData("file:///C:/a.png")]
    public void CheckPath_LocalPaths_BlockedForRemoteSource(string path)
    {
        Assert.NotNull(SecurityPolicy.CheckPath(path, true));
    }

    [Fact]
    public void CheckPath_LocalAbsolute_AllowedForLocalSource_UncBlocked()
    {
        Assert.Null(SecurityPolicy.CheckPath(@"C:\images\a.png", false));
        Assert.NotNull(SecurityPolicy.CheckPath(@"relative\a.png", false));
        Assert.NotNull(SecurityPolicy.CheckPath("ftp://example.com/a.png", false));
    }

    [Fact]
    public void RemoteOrchestration_WithLocalPath_FailsValidation()
    {
        var o = new Orchestration
        {
            Source = OrchestrationSource.URL,
            SourcePath = "https://example.com/o.json",
            Actions = [new ImageAction { Name = "a", Duration = 5, Path = @"C:\a.png" }]
        };
        Assert.False(o.Validate().IsFullyValid);
    }

    [Fact]
    public void Duration_AboveMax_FailsValidation()
    {
        var a = new ImageAction { Name = "a", Duration = int.MaxValue, Path = "https://example.com/a.png" };
        Assert.Contains(a.Validate(), r => r.IsValid == false);
    }

    [Fact]
    public void LoadFromContent_OverOneMegabyte_Throws()
    {
        var loader = new OrchestrationLoader(new NullHttp());
        var big = "{\"name\":\"" + new string('a', SecurityPolicy.MaxOrchestrationBytes) + "\"}";
        Assert.Throws<InvalidOperationException>(() => loader.LoadFromContent(big));
    }

    [Fact]
    public void Deserialize_XmlWithDtd_ReturnsNull()
    {
        var xml = "<?xml version=\"1.0\"?><!DOCTYPE o [<!ENTITY x \"y\">]><Orchestration><Name>&x;</Name></Orchestration>";
        Assert.Null(OrchestrationSerializer.Deserialize(xml));
    }

    private sealed class NullHttp : IHttpService
    {
        public Task<string> GetStringAsync(Uri uri, CancellationToken cancellationToken = default) => Task.FromResult("{}");
        public Task<ValidationResult> ValidateUriAsync(string uri, CancellationToken cancellationToken = default)
            => Task.FromResult(new ValidationResult { IsValid = true });
    }
}

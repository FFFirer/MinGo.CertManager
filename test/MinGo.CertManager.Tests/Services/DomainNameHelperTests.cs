using MinGo.CertManager.Core.Services;
using Xunit;

namespace MinGo.CertManager.Tests.Services;

public class DomainNameHelperTests
{
    [Theory]
    [InlineData("example.com", "@", "example.com")]
    [InlineData("example.com", "www", "www.example.com")]
    [InlineData("example.com", "*.api", "*.api.example.com")]
    [InlineData("Example.COM", "WWW", "www.example.com")]
    [InlineData("example.com.", "www.", "www.example.com")]
    public void ToFqdn_ShouldDeriveFromRrAndZone(string zone, string rr, string expected)
    {
        var fqdn = DomainNameHelper.ToFqdn(zone, rr);
        Assert.Equal(expected, fqdn);
    }

    [Theory]
    [InlineData("www.example.com", new[] { "example.com" }, "example.com")]
    [InlineData("example.com", new[] { "example.com" }, "example.com")]
    [InlineData("a.b.example.com", new[] { "example.com", "b.example.com" }, "b.example.com")]
    [InlineData("foo.other.org", new[] { "example.com" }, null)]
    [InlineData("example.com", new[] { "other.org" }, null)]
    public void MatchZone_ShouldPickLongestSuffix(string fqdn, string[] zones, string? expected)
    {
        var zone = DomainNameHelper.MatchZone(fqdn, zones);
        Assert.Equal(expected, zone);
    }

    [Theory]
    [InlineData("*.example.com", true)]
    [InlineData("www.example.com", false)]
    [InlineData("example.com", false)]
    public void IsWildcard_ShouldDetectPrefix(string fqdn, bool expected)
    {
        Assert.Equal(expected, DomainNameHelper.IsWildcard(fqdn));
    }
}

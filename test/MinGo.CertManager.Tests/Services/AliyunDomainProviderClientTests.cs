using MinGo.CertManager.Infrastructure.Services;

namespace MinGo.CertManager.Tests.Services;

public class AliyunDomainProviderClientTests
{
    [Theory]
    [InlineData(null, "alidns.aliyuncs.com")]
    [InlineData("", "alidns.aliyuncs.com")]
    [InlineData("   ", "alidns.aliyuncs.com")]
    [InlineData("alidns.aliyuncs.com", "alidns.aliyuncs.com")]
    [InlineData("https://alidns.aliyuncs.com/", "alidns.aliyuncs.com")]
    [InlineData("https://alidns.aliyuncs.com", "alidns.aliyuncs.com")]
    [InlineData("http://alidns.aliyuncs.com/path", "alidns.aliyuncs.com")]
    [InlineData("alidns.cn-hangzhou.aliyuncs.com", "alidns.cn-hangzhou.aliyuncs.com")]
    [InlineData("https://alidns.cn-hangzhou.aliyuncs.com/", "alidns.cn-hangzhou.aliyuncs.com")]
    public void NormalizeEndpoint_ShouldReturnHostname(string? input, string expected)
    {
        Assert.Equal(expected, AliyunDomainProviderClient.NormalizeEndpoint(input));
    }
}

using MinGo.CertManager.Application.Services;
using MinGo.CertManager.Core.Entities;
using MinGo.CertManager.Core.Services;
using MinGo.CertManager.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Moq;

namespace MinGo.CertManager.Tests.Services;

public class DnsProviderConsoleServiceTests
{
    private static DnsProviderConsoleService CreateService(
        IDomainProviderClient client,
        string accessKeyId = "",
        string accessKeySecret = "")
    {
        var settings = Options.Create(new AliyunDnsSettings
        {
            AccessKeyId = accessKeyId,
            AccessKeySecret = accessKeySecret,
            RegionId = "cn-hangzhou",
            Endpoint = "https://alidns.aliyuncs.com/"
        });

        return new DnsProviderConsoleService(new DnsProviderCatalog(), client, settings);
    }

    private static Mock<IDomainProviderClient> CreateClientMock()
    {
        var mock = new Mock<IDomainProviderClient>();
        mock.SetupGet(x => x.ProviderType).Returns(DnsProviderType.Aliyun);
        return mock;
    }

    [Fact]
    public void GetCatalog_ShouldContainBuiltInAliyunAndPlannedPlaceholders()
    {
        var catalog = new DnsProviderCatalog();
        var items = catalog.GetItems();

        Assert.Equal(3, items.Count);

        var aliyun = items.Single(i => i.ProviderType == DnsProviderType.Aliyun);
        Assert.Equal(DnsProviderImplementationStatus.BuiltIn, aliyun.ImplementationStatus);
        Assert.True(aliyun.HasClient);
        Assert.Equal("AliyunDns", aliyun.ConfigSectionName);
        Assert.Equal("阿里云 DNS", aliyun.DisplayName);

        var cloudflare = items.Single(i => i.ProviderType == DnsProviderType.Cloudflare);
        Assert.Equal(DnsProviderImplementationStatus.Planned, cloudflare.ImplementationStatus);
        Assert.False(cloudflare.HasClient);

        var dnspod = items.Single(i => i.ProviderType == DnsProviderType.Dnspod);
        Assert.Equal(DnsProviderImplementationStatus.Planned, dnspod.ImplementationStatus);
        Assert.False(dnspod.HasClient);
    }

    [Fact]
    public void GetConfigView_WhenCredentialsPresent_ShouldBeConfiguredAndMasked()
    {
        var service = CreateService(CreateClientMock().Object, "LTAI5tExampleKeyId", "super-secret-value");

        var view = service.GetConfigView(DnsProviderType.Aliyun);

        Assert.Equal(ProviderConfigState.Configured, view.State);
        Assert.Equal("LTAI5t****", view.MaskedAccessKeyId);
        Assert.True(view.HasAccessKeySecret);
        Assert.Equal("cn-hangzhou", view.RegionId);
        Assert.DoesNotContain("super-secret-value", view.MaskedAccessKeyId);
    }

    [Fact]
    public void GetConfigView_WhenSecretMissing_ShouldBeNotConfigured()
    {
        var service = CreateService(CreateClientMock().Object, "LTAI5tExampleKeyId", " ");

        var view = service.GetConfigView(DnsProviderType.Aliyun);

        Assert.Equal(ProviderConfigState.NotConfigured, view.State);
        Assert.False(view.HasAccessKeySecret);
    }

    [Fact]
    public void GetConfigView_PlannedProvider_ShouldBeNotConfiguredWithoutSecret()
    {
        var service = CreateService(CreateClientMock().Object);

        var view = service.GetConfigView(DnsProviderType.Cloudflare);

        Assert.Equal(ProviderConfigState.NotConfigured, view.State);
        Assert.False(view.HasAccessKeySecret);
        Assert.Equal(string.Empty, view.MaskedAccessKeyId);
    }

    [Fact]
    public async Task TestConnection_WhenNotConfigured_ShouldNotCallProviderApi()
    {
        var client = CreateClientMock();
        var service = CreateService(client.Object);

        var result = await service.TestConnectionAsync(DnsProviderType.Aliyun);

        Assert.Equal(ProviderHealthStatus.NotConfigured, result.Status);
        Assert.Null(result.ZoneCount);
        client.Verify(x => x.ListZonesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TestConnection_PlannedProvider_ShouldReturnNotSupported()
    {
        var client = CreateClientMock();
        var service = CreateService(client.Object, "id", "secret");

        var result = await service.TestConnectionAsync(DnsProviderType.Cloudflare);

        Assert.Equal(ProviderHealthStatus.NotSupported, result.Status);
        client.Verify(x => x.ListZonesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TestConnection_Success_ShouldReturnZoneCount()
    {
        var client = CreateClientMock();
        client.Setup(x => x.ListZonesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProviderZone>
            {
                new("example.com"),
                new("other.org")
            });
        var service = CreateService(client.Object, "LTAI5tExampleKeyId", "secret");

        var result = await service.TestConnectionAsync(DnsProviderType.Aliyun);

        Assert.Equal(ProviderHealthStatus.Success, result.Status);
        Assert.Equal(2, result.ZoneCount);
        Assert.Contains("2", result.Message);
    }

    [Fact]
    public async Task TestConnection_AuthFailure_ShouldMapToAuthFailed()
    {
        var client = CreateClientMock();
        client.Setup(x => x.ListZonesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("InvalidAccessKeyId: specified access key does not exist"));
        var service = CreateService(client.Object, "bad-id", "bad-secret");

        var result = await service.TestConnectionAsync(DnsProviderType.Aliyun);

        Assert.Equal(ProviderHealthStatus.AuthFailed, result.Status);
        Assert.Contains("鉴权失败", result.Message);
    }

    [Fact]
    public async Task TestConnection_NetworkFailure_ShouldMapToConnectionFailed()
    {
        var client = CreateClientMock();
        client.Setup(x => x.ListZonesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("connection refused"));
        var service = CreateService(client.Object, "id", "secret");

        var result = await service.TestConnectionAsync(DnsProviderType.Aliyun);

        Assert.Equal(ProviderHealthStatus.ConnectionFailed, result.Status);
        Assert.Contains("连接失败", result.Message);
    }

    [Theory]
    [InlineData("InvalidAccessKeyId", ProviderHealthStatus.AuthFailed)]
    [InlineData("SignatureDoesNotMatch", ProviderHealthStatus.AuthFailed)]
    [InlineData("AccessDenied", ProviderHealthStatus.AuthFailed)]
    [InlineData("Forbidden.RAM", ProviderHealthStatus.AuthFailed)]
    [InlineData("Unauthorized", ProviderHealthStatus.AuthFailed)]
    [InlineData("connection timed out", ProviderHealthStatus.ConnectionFailed)]
    [InlineData("something else", ProviderHealthStatus.ConnectionFailed)]
    public void ClassifyFailure_ShouldMapMessages(string message, ProviderHealthStatus expected)
    {
        var status = DnsProviderConsoleService.ClassifyFailure(new Exception(message));
        Assert.Equal(expected, status);
    }

    [Fact]
    public void ClassifyFailure_ShouldInspectInnerException()
    {
        var ex = new InvalidOperationException("wrapper", new TimeoutException("underlying timeout"));
        var status = DnsProviderConsoleService.ClassifyFailure(ex);
        Assert.Equal(ProviderHealthStatus.ConnectionFailed, status);
    }
}

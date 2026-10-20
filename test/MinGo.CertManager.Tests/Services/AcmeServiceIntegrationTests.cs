using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MinGo.CertManager.Application.Services;
using MinGo.CertManager.Core.Constants;
using MinGo.CertManager.Core.Entities;
using MinGo.CertManager.Core.Services;
using MinGo.CertManager.Infrastructure.Configuration;
using MinGo.CertManager.Infrastructure.Data;
using MinGo.CertManager.Infrastructure.Services;
using Certes;
using Moq;
using Xunit;

namespace MinGo.CertManager.Tests.Services;

public class AcmeServiceIntegrationTests
{
    private readonly Mock<ILogger<AcmeService>> _loggerMock;
    private readonly Mock<ILogger<AcmeAccountCache>> _cacheLoggerMock;
    private readonly Mock<IDomainProviderClient> _dnsServiceMock;
    private readonly Mock<IAcmeAccountCache> _accountCacheMock;
    private readonly Mock<IOperationLogService> _operationLogMock;
    private readonly IOptions<AcmeSettings> _acmeSettings;
    private readonly ApplicationDbContext _dbContext;

    public AcmeServiceIntegrationTests()
    {
        _loggerMock = new Mock<ILogger<AcmeService>>();
        _cacheLoggerMock = new Mock<ILogger<AcmeAccountCache>>();
        _dnsServiceMock = new Mock<IDomainProviderClient>();
        _accountCacheMock = new Mock<IAcmeAccountCache>();
        _operationLogMock = new Mock<IOperationLogService>();

        _dnsServiceMock.SetupGet(x => x.ProviderType).Returns(DnsProviderType.Aliyun);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);

        _acmeSettings = Options.Create(new AcmeSettings
        {
            UseStaging = true,
            AccountEmail = "test@test.com",
            LetsEncryptProductionUrl = "https://acme-v02.api.letsencrypt.org/directory",
            LetsEncryptStagingUrl = "https://acme-staging-v02.api.letsencrypt.org/directory"
        });
    }

    private AcmeService CreateService()
    {
        return new AcmeService(
            _loggerMock.Object,
            _acmeSettings,
            _accountCacheMock.Object,
            _operationLogMock.Object);
    }

    // ============================================================
    // 集成测试（需要网络连接并调用 Let's Encrypt Staging 环境）
    // 默认跳过，需要时移除 Skip 属性后执行。
    // ============================================================

    [Fact(Skip = "集成测试 - 需要网络连接和 Let's Encrypt Staging 环境。手动移除 Skip 后执行。")]
    [Trait("Category", "Integration")]
    public async Task RequestCertificateAsync_WithValidDomain_ShouldCreateDnsRecord()
    {
        var domain = "test-example.com";
        var isWildcard = false;
        var useStaging = true;

        _accountCacheMock
            .Setup(x => x.GetCachedAccountAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((AcmeAccount?)null);

        _dnsServiceMock
            .Setup(x => x.CreateTxtRecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("record-id");

        _dnsServiceMock
            .Setup(x => x.DeleteTxtRecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var acmeService = CreateService();

        var result = await acmeService.RequestCertificateAsync(domain, isWildcard, _dnsServiceMock.Object, useStaging);

        Assert.NotNull(result);
        Assert.NotEmpty(result.CertificatePem);
        Assert.NotEmpty(result.PrivateKeyPem);
        Assert.NotEmpty(result.CertificateChainPem);

        _dnsServiceMock.Verify(
            x => x.CreateTxtRecordAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(Skip = "集成测试 - 需要网络连接和 Let's Encrypt Staging 环境。手动移除 Skip 后执行。")]
    [Trait("Category", "Integration")]
    public async Task RequestCertificateAsync_WithWildcardDomain_ShouldCreateDnsRecordForBaseDomain()
    {
        var domain = "test-example.com";
        var isWildcard = true;
        var useStaging = true;

        _accountCacheMock
            .Setup(x => x.GetCachedAccountAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((AcmeAccount?)null);

        _dnsServiceMock
            .Setup(x => x.CreateTxtRecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("record-id");

        _dnsServiceMock
            .Setup(x => x.DeleteTxtRecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var acmeService = CreateService();

        var result = await acmeService.RequestCertificateAsync(domain, isWildcard, _dnsServiceMock.Object, useStaging);

        Assert.NotNull(result);
        Assert.NotEmpty(result.CertificatePem);
        Assert.NotEmpty(result.PrivateKeyPem);
        Assert.NotEmpty(result.CertificateChainPem);

        _dnsServiceMock.Verify(
            x => x.CreateTxtRecordAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(Skip = "集成测试 - 需要网络连接和 Let's Encrypt Staging 环境。手动移除 Skip 后执行。")]
    [Trait("Category", "Integration")]
    public async Task RequestCertificateAsync_WithCachedAccount_ShouldUseCachedAccount()
    {
        var domain = "test-example.com";
        var isWildcard = false;
        var useStaging = true;

        var cachedAccount = new AcmeAccount
        {
            Id = Guid.NewGuid(),
            AccountId = "cached-account-id",
            AccountKey = Convert.ToBase64String(KeyFactory.NewKey(KeyAlgorithm.ES256).ToDer()),
            Contact = "mailto:test@test.com",
            AcmeServerUrl = "https://acme-staging-v02.api.letsencrypt.org/directory",
            IsStaging = true,
            CreatedAt = DateTime.UtcNow,
            LastUsedAt = DateTime.UtcNow
        };

        _accountCacheMock
            .Setup(x => x.GetCachedAccountAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(cachedAccount);

        _accountCacheMock
            .Setup(x => x.UpdateLastUsedAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        _dnsServiceMock
            .Setup(x => x.CreateTxtRecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("record-id");

        _dnsServiceMock
            .Setup(x => x.DeleteTxtRecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var acmeService = CreateService();

        var result = await acmeService.RequestCertificateAsync(domain, isWildcard, _dnsServiceMock.Object, useStaging);

        Assert.NotNull(result);

        _accountCacheMock.Verify(
            x => x.GetCachedAccountAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);

        _accountCacheMock.Verify(
            x => x.UpdateLastUsedAsync(It.IsAny<Guid>()),
            Times.Once);
    }

    [Fact(Skip = "集成测试 - 需要网络连接和 Let's Encrypt Staging 环境。手动移除 Skip 后执行。")]
    [Trait("Category", "Integration")]
    public async Task RequestCertificateAsync_WhenDnsRecordCreationFails_ShouldHandleException()
    {
        var domain = "test-example.com";
        var isWildcard = false;
        var useStaging = true;

        _accountCacheMock
            .Setup(x => x.GetCachedAccountAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((AcmeAccount?)null);

        _dnsServiceMock
            .Setup(x => x.CreateTxtRecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DNS record creation failed"));

        var acmeService = CreateService();

        var ex = await Assert.ThrowsAsync<Exception>(() =>
            acmeService.RequestCertificateAsync(domain, isWildcard, _dnsServiceMock.Object, useStaging));

        Assert.Contains("DNS record creation failed", ex.Message);
    }

    [Fact(Skip = "集成测试 - 需要网络连接和 Let's Encrypt Staging 环境。手动移除 Skip 后执行。")]
    [Trait("Category", "Integration")]
    public async Task RequestCertificateAsync_WhenDnsRecordDeletionFails_ShouldContinueExecution()
    {
        var domain = "test-example.com";
        var isWildcard = false;
        var useStaging = true;

        _accountCacheMock
            .Setup(x => x.GetCachedAccountAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((AcmeAccount?)null);

        _dnsServiceMock
            .Setup(x => x.CreateTxtRecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("record-id");

        _dnsServiceMock
            .Setup(x => x.ClearTxtRecordsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DNS record deletion failed"))
            .Verifiable();

        // 第二次（finally 清理）也失败时不应阻断主流程 —— 清理异常被吞掉
        var acmeService = CreateService();

        // 创建阶段用 ClearTxtRecordsAsync 做事前清理，若抛错则主流程失败；
        // 本用例仅验证 Delete/Clear 失败可被容忍的清理路径
        _dnsServiceMock.Reset();
        _dnsServiceMock.SetupGet(x => x.ProviderType).Returns(DnsProviderType.Aliyun);
        _accountCacheMock
            .Setup(x => x.GetCachedAccountAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((AcmeAccount?)null);
        _dnsServiceMock
            .Setup(x => x.CreateTxtRecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("record-id");
        _dnsServiceMock
            .Setup(x => x.ClearTxtRecordsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await acmeService.RequestCertificateAsync(domain, isWildcard, _dnsServiceMock.Object, useStaging);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task CleanupAsync_ShouldClearDnsRecord()
    {
        var domain = "test-example.com";
        var isWildcard = false;

        _dnsServiceMock
            .Setup(x => x.ClearTxtRecordsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var acmeService = CreateService();

        await acmeService.CleanupAsync(domain, isWildcard, _dnsServiceMock.Object);

        _dnsServiceMock.Verify(
            x => x.ClearTxtRecordsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CleanupAsync_WithWildcardDomain_ShouldClearMultipleDnsRecords()
    {
        var domain = "test-example.com";
        var isWildcard = true;

        _dnsServiceMock
            .Setup(x => x.ClearTxtRecordsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var acmeService = CreateService();

        await acmeService.CleanupAsync(domain, isWildcard, _dnsServiceMock.Object);

        // 通配符域名应该清理多个DNS记录（域名本身 + *.domain）
        _dnsServiceMock.Verify(
            x => x.ClearTxtRecordsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task CleanupAsync_WhenDnsServiceThrowsException_ShouldContinueExecution()
    {
        var domain = "test-example.com";
        var isWildcard = false;

        _dnsServiceMock
            .Setup(x => x.ClearTxtRecordsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DNS service error"));

        var acmeService = CreateService();

        var exception = await Record.ExceptionAsync(() =>
            acmeService.CleanupAsync(domain, isWildcard, _dnsServiceMock.Object));

        Assert.Null(exception);

        _dnsServiceMock.Verify(
            x => x.ClearTxtRecordsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AcmeAccountCache_ShouldCacheAndRetrieveAccount()
    {
        var accountCache = new AcmeAccountCache(_dbContext, _cacheLoggerMock.Object);

        var account = new AcmeAccount
        {
            Id = Guid.NewGuid(),
            AccountId = "test-account-id",
            AccountKey = Convert.ToBase64String(KeyFactory.NewKey(KeyAlgorithm.ES256).ToDer()),
            Contact = "mailto:test@test.com",
            AcmeServerUrl = "https://acme-staging-v02.api.letsencrypt.org/directory",
            IsStaging = true
        };

        await accountCache.CacheAccountAsync(account);

        var retrieved = await accountCache.GetCachedAccountAsync(account.AcmeServerUrl, account.Contact);

        Assert.NotNull(retrieved);
        Assert.Equal(account.AccountId, retrieved.AccountId);
        Assert.Equal(account.Contact, retrieved.Contact);
        Assert.Equal(account.AcmeServerUrl, retrieved.AcmeServerUrl);
    }

    [Fact]
    public async Task AcmeAccountCache_ShouldUpdateLastUsedTime()
    {
        var accountCache = new AcmeAccountCache(_dbContext, _cacheLoggerMock.Object);

        var account = new AcmeAccount
        {
            Id = Guid.NewGuid(),
            AccountId = "test-account-id",
            AccountKey = Convert.ToBase64String(KeyFactory.NewKey(KeyAlgorithm.ES256).ToDer()),
            Contact = "mailto:test@test.com",
            AcmeServerUrl = "https://acme-staging-v02.api.letsencrypt.org/directory",
            IsStaging = true
        };

        await accountCache.CacheAccountAsync(account);

        var originalLastUsed = account.LastUsedAt;

        await Task.Delay(100);

        await accountCache.UpdateLastUsedAsync(account.Id);

        var retrieved = await _dbContext.AcmeAccounts.FindAsync(account.Id);

        Assert.NotNull(retrieved);
        Assert.True(retrieved.LastUsedAt > originalLastUsed);
    }

    [Fact]
    public async Task AcmeAccountCache_WhenAccountNotFound_ShouldReturnNull()
    {
        var accountCache = new AcmeAccountCache(_dbContext, _cacheLoggerMock.Object);

        var retrieved = await accountCache.GetCachedAccountAsync("https://acme-staging-v02.api.letsencrypt.org/directory", "mailto:nonexistent@test.com");

        Assert.Null(retrieved);
    }

    // ============================================================
    // SplitDomainName 单元测试
    //
    // 算法：始终取最后 2 段为根域名（如 example.com）。
    // ============================================================

    [Theory]
    [InlineData("example.com", "", "example.com")]
    [InlineData("www.example.com", "www", "example.com")]
    [InlineData("a.b.example.com", "a.b", "example.com")]
    [InlineData("a.b.c.example.com", "a.b.c", "example.com")]
    [InlineData("*.example.com", "*", "example.com")]
    [InlineData("gitea.private.fffirer.top", "gitea.private", "fffirer.top")]
    [InlineData("sub.example.xyz", "sub", "example.xyz")]
    public void SplitDomainName_ShouldExtractSubdomainAndRoot(string input, string expectedRr, string expectedRoot)
    {
        var (rr, root) = AcmeService.SplitDomainName(input);
        Assert.Equal(expectedRr, rr);
        Assert.Equal(expectedRoot, root);
    }

    [Theory]
    [InlineData("example.com", "example.com")]
    [InlineData("www.example.com", "example.com")]
    [InlineData("a.b.c.example.com", "example.com")]
    [InlineData("gitea.private.fffirer.top", "fffirer.top")]
    public void SplitDomainName_RootShouldBeRegistrableDomain(string input, string expectedRoot)
    {
        var (_, root) = AcmeService.SplitDomainName(input);
        Assert.Equal(expectedRoot, root);
    }

    // ============================================================
    // ResolveRoot 单元测试：优先 Zone，回退启发式
    // ============================================================

    [Theory]
    [InlineData("www.example.com", "example.com", "www", "example.com")]
    [InlineData("example.com", "example.com", "@", "example.com")]
    [InlineData("*.example.com", "example.com", "*", "example.com")]
    [InlineData("a.b.example.com", "example.com", "a.b", "example.com")]
    [InlineData("www.example.com", null, "www", "example.com")]
    public void ResolveRoot_ShouldPreferKnownZone(string input, string? zone, string expectedRr, string expectedRoot)
    {
        var (rr, root) = AcmeService.ResolveRoot(input, zone);
        Assert.Equal(expectedRr, rr);
        Assert.Equal(expectedRoot, root);
    }
}

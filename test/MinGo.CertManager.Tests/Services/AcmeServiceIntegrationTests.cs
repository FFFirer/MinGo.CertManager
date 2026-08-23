using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MinGo.CertManager.Core.Constants;
using MinGo.CertManager.Core.Entities;
using MinGo.CertManager.Core.Services;
using MinGo.CertManager.Infrastructure.Configuration;
using MinGo.CertManager.Infrastructure.Data;
using MinGo.CertManager.Infrastructure.Services;
using MinGo.CertManager.Application.Services;
using Certes;
using Certes.Acme;
using Moq;
using Xunit;

namespace MinGo.CertManager.Tests.Services;

public class AcmeServiceIntegrationTests
{
    private readonly Mock<ILogger<AcmeService>> _loggerMock;
    private readonly Mock<ILogger<AcmeAccountCache>> _cacheLoggerMock;
    private readonly Mock<IAliyunDnsService> _dnsServiceMock;
    private readonly Mock<IAcmeAccountCache> _accountCacheMock;
    private readonly IOptions<AcmeSettings> _acmeSettings;
    private readonly ApplicationDbContext _dbContext;

    public AcmeServiceIntegrationTests()
    {
        _loggerMock = new Mock<ILogger<AcmeService>>();
        _cacheLoggerMock = new Mock<ILogger<AcmeAccountCache>>();
        _dnsServiceMock = new Mock<IAliyunDnsService>();
        _accountCacheMock = new Mock<IAcmeAccountCache>();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);

        _acmeSettings = Options.Create(new AcmeSettings
        {
            UseStaging = true,
            AccountEmail = "test@test.com", // 使用有效的测试邮箱
            LetsEncryptProductionUrl = "https://acme-v02.api.letsencrypt.org/directory",
            LetsEncryptStagingUrl = "https://acme-staging-v02.api.letsencrypt.org/directory"
        });
    }

    // ============================================================
    // 集成测试（需要网络连接并调用 Let's Encrypt Staging 环境）
    // 默认跳过，需要时移除 Skip 属性后执行。
    // 建议在 CI 中通过 `dotnet test --filter "Category=Integration"` 单独触发。
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
            .Setup(x => x.CreateTxtRecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _dnsServiceMock
            .Setup(x => x.DeleteTxtRecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var acmeService = new AcmeService(
            _loggerMock.Object,
            _acmeSettings,
            _accountCacheMock.Object);

        var result = await acmeService.RequestCertificateAsync(domain, isWildcard, _dnsServiceMock.Object, useStaging);

        Assert.NotNull(result);
        Assert.NotEmpty(result.CertificatePem);
        Assert.NotEmpty(result.PrivateKeyPem);
        Assert.NotEmpty(result.CertificateChainPem);

        _dnsServiceMock.Verify(
            x => x.CreateTxtRecordAsync(
                It.Is<string>(d => d == domain),
                It.Is<string>(r => r == $"_acme-challenge.{domain}"),
                It.IsAny<string>()),
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
            .Setup(x => x.CreateTxtRecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _dnsServiceMock
            .Setup(x => x.DeleteTxtRecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var acmeService = new AcmeService(
            _loggerMock.Object,
            _acmeSettings,
            _accountCacheMock.Object);

        var result = await acmeService.RequestCertificateAsync(domain, isWildcard, _dnsServiceMock.Object, useStaging);

        Assert.NotNull(result);
        Assert.NotEmpty(result.CertificatePem);
        Assert.NotEmpty(result.PrivateKeyPem);
        Assert.NotEmpty(result.CertificateChainPem);

        _dnsServiceMock.Verify(
            x => x.CreateTxtRecordAsync(
                It.Is<string>(d => d == domain),
                It.Is<string>(r => r == $"_acme-challenge.{domain}"),
                It.IsAny<string>()),
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
            .Setup(x => x.CreateTxtRecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _dnsServiceMock
            .Setup(x => x.DeleteTxtRecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var acmeService = new AcmeService(
            _loggerMock.Object,
            _acmeSettings,
            _accountCacheMock.Object);

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
            .Setup(x => x.CreateTxtRecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("DNS record creation failed"));

        _dnsServiceMock
            .Setup(x => x.DeleteTxtRecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var acmeService = new AcmeService(
            _loggerMock.Object,
            _acmeSettings,
            _accountCacheMock.Object);

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
            .Setup(x => x.CreateTxtRecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _dnsServiceMock
            .Setup(x => x.DeleteTxtRecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("DNS record deletion failed"));

        var acmeService = new AcmeService(
            _loggerMock.Object,
            _acmeSettings,
            _accountCacheMock.Object);

        _dnsServiceMock.Setup(x => x.ClearTxtRecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
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
            .Setup(x => x.ClearTxtRecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var acmeService = new AcmeService(
            _loggerMock.Object,
            _acmeSettings,
            _accountCacheMock.Object);

        await acmeService.CleanupAsync(domain, isWildcard, _dnsServiceMock.Object);

        _dnsServiceMock.Verify(
            x => x.ClearTxtRecordAsync(
                It.Is<string>(d => d == domain),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task CleanupAsync_WithWildcardDomain_ShouldClearMultipleDnsRecords()
    {
        var domain = "test-example.com";
        var isWildcard = true;

        _dnsServiceMock
            .Setup(x => x.ClearTxtRecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var acmeService = new AcmeService(
            _loggerMock.Object,
            _acmeSettings,
            _accountCacheMock.Object);

        await acmeService.CleanupAsync(domain, isWildcard, _dnsServiceMock.Object);

        // 通配符域名应该清理多个DNS记录（域名本身 + *.domain）
        _dnsServiceMock.Verify(
            x => x.ClearTxtRecordAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task CleanupAsync_WhenDnsServiceThrowsException_ShouldContinueExecution()
    {
        var domain = "test-example.com";
        var isWildcard = false;

        _dnsServiceMock
            .Setup(x => x.ClearTxtRecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("DNS service error"));

        var acmeService = new AcmeService(
            _loggerMock.Object,
            _acmeSettings,
            _accountCacheMock.Object);

        // 即使DNS服务失败，清理操作也应该尝试完成（异常被内部记录，不会传播）
        var exception = await Record.ExceptionAsync(() =>
            acmeService.CleanupAsync(domain, isWildcard, _dnsServiceMock.Object));

        Assert.Null(exception);

        // 验证是否尝试调用了清理操作
        _dnsServiceMock.Verify(
            x => x.ClearTxtRecordAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
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
    // 工具方法单元测试
    // ============================================================

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
}

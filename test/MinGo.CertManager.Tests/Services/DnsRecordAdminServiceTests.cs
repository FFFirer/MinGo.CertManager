using Microsoft.EntityFrameworkCore;
using MinGo.CertManager.Application.Services;
using MinGo.CertManager.Core.Entities;
using MinGo.CertManager.Core.Services;
using MinGo.CertManager.Infrastructure.Data;
using MinGo.CertManager.Infrastructure.Repositories;
using Moq;

namespace MinGo.CertManager.Tests.Services;

public class DnsRecordAdminServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly DomainRepository _domainRepository;
    private readonly OperationLogService _logService;
    private readonly Mock<IDomainProviderClient> _providerMock;
    private readonly DnsRecordAdminService _service;

    public DnsRecordAdminServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);
        _domainRepository = new DomainRepository(_db);
        _logService = new OperationLogService(new OperationLogRepository(_db));
        _providerMock = new Mock<IDomainProviderClient>();
        _providerMock.SetupGet(x => x.ProviderType).Returns(DnsProviderType.Aliyun);
        _service = new DnsRecordAdminService(_providerMock.Object, _domainRepository, _logService);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Create_Success_WritesMirrorAndFqdn()
    {
        _providerMock
            .Setup(x => x.CreateRecordAsync("example.com", "www", "A", "1.2.3.4", 600, It.IsAny<CancellationToken>()))
            .ReturnsAsync("rid-1");

        var result = await _service.CreateAsync("example.com", "www", "A", "1.2.3.4", 600, "admin");

        Assert.Equal("rid-1", result.Record.ProviderRecordId);
        Assert.Equal(DomainAssetStatus.Active, result.Record.Status);

        var saved = await _domainRepository.GetRecordAsync(DnsProviderType.Aliyun, "example.com", "rid-1");
        Assert.NotNull(saved);
        Assert.Equal("www.example.com", saved!.Fqdn);

        var domain = await _domainRepository.GetByFqdnAsync(DnsProviderType.Aliyun, "www.example.com");
        Assert.NotNull(domain);
        Assert.Equal(DomainAssetStatus.Active, domain!.Status);
        Assert.Equal(DomainAssetSource.Synced, domain.Source);

        var logs = await _logService.QueryAsync(action: OperationLogAction.RecordCreated);
        Assert.True(logs.Total >= 1);
    }

    [Fact]
    public async Task Create_ProviderFails_DoesNotWriteLocal()
    {
        _providerMock
            .Setup(x => x.CreateRecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAsync("example.com", "www", "A", "1.2.3.4", 600, "admin"));

        var records = await _domainRepository.GetRecordsAsync(DnsProviderType.Aliyun, "example.com");
        Assert.Empty(records);

        var logs = await _logService.QueryAsync(action: OperationLogAction.RecordCreated);
        Assert.Equal(1, logs.Total);
        Assert.Equal(OperationLogResult.Failed, logs.Items[0].Result);
    }

    [Fact]
    public async Task Create_InvalidType_ThrowsWithoutCallingProvider()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateAsync("example.com", "www", "MX", "10 mail", 600, "admin"));

        _providerMock.Verify(
            x => x.CreateRecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Create_RootCname_Rejected()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateAsync("example.com", "@", "CNAME", "cdn.example.com", 600, "admin"));
    }

    [Fact]
    public async Task Create_DefaultTtlUsedWhenZero()
    {
        _providerMock
            .Setup(x => x.CreateRecordAsync("example.com", "www", "A", "1.2.3.4", 600, It.IsAny<CancellationToken>()))
            .ReturnsAsync("rid-ttl");

        await _service.CreateAsync("example.com", "www", "A", "1.2.3.4", 0, "admin");

        _providerMock.Verify(
            x => x.CreateRecordAsync("example.com", "www", "A", "1.2.3.4", 600, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Delete_Success_SoftDeletesAndAlignsFqdn()
    {
        _providerMock
            .Setup(x => x.CreateRecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("rid-del");
        _providerMock
            .Setup(x => x.DeleteRecordAsync("rid-del", "example.com", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _service.CreateAsync("example.com", "dev", "A", "1.2.3.4", 600, "admin");
        var deleted = await _service.DeleteAsync("example.com", "rid-del", "admin");

        Assert.Equal(DomainAssetStatus.Removed, deleted.Record.Status);

        var domain = await _domainRepository.GetByFqdnAsync(DnsProviderType.Aliyun, "dev.example.com");
        Assert.NotNull(domain);
        Assert.Equal(DomainAssetStatus.Removed, domain!.Status);

        var logs = await _logService.QueryAsync(action: OperationLogAction.RecordRemoved);
        Assert.Equal(1, logs.Total);
        Assert.Equal(OperationLogResult.Success, logs.Items[0].Result);
    }

    [Fact]
    public async Task Delete_KeepsFqdn_WhenOtherActiveRecordsRemain()
    {
        _providerMock
            .Setup(x => x.CreateRecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string z, string rr, string t, string v, int ttl, CancellationToken _) =>
                $"rid-{rr}-{t}");

        await _service.CreateAsync("example.com", "www", "A", "1.2.3.4", 600, "admin");
        await _service.CreateAsync("example.com", "www", "AAAA", "2001:db8::1", 600, "admin");

        await _service.DeleteAsync("example.com", "rid-www-A", "admin");

        var domain = await _domainRepository.GetByFqdnAsync(DnsProviderType.Aliyun, "www.example.com");
        Assert.Equal(DomainAssetStatus.Active, domain!.Status);
    }

    [Fact]
    public async Task Delete_ManualFqdn_NotRemoved()
    {
        await _domainRepository.UpsertDomainAsync(
            DnsProviderType.Aliyun, "keep.example.com", "example.com", DomainAssetSource.Manual);

        _providerMock
            .Setup(x => x.CreateRecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("rid-manual");
        await _service.CreateAsync("example.com", "keep", "A", "1.2.3.4", 600, "admin");

        // UpsertDomainAsync with Synced keeps Manual source
        await _service.DeleteAsync("example.com", "rid-manual", "admin");

        var domain = await _domainRepository.GetByFqdnAsync(DnsProviderType.Aliyun, "keep.example.com");
        Assert.Equal(DomainAssetSource.Manual, domain!.Source);
        Assert.Equal(DomainAssetStatus.Active, domain.Status);
    }

    [Fact]
    public async Task Delete_AcmeChallenge_RejectedWithoutProviderCall()
    {
        _providerMock
            .Setup(x => x.CreateRecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("rid-acme");

        // 绕过创建校验：直接写镜像模拟同步得到的 ACME 记录
        await _domainRepository.UpsertRecordAsync(
            DnsProviderType.Aliyun, "example.com", "rid-acme",
            "_acme-challenge.www", "TXT", "x", 600, "_acme-challenge.www.example.com");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.DeleteAsync("example.com", "rid-acme", "admin"));

        _providerMock.Verify(
            x => x.DeleteRecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Delete_ProviderFails_LocalUnchanged()
    {
        _providerMock
            .Setup(x => x.CreateRecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("rid-fail-del");
        _providerMock
            .Setup(x => x.DeleteRecordAsync("rid-fail-del", "example.com", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("api down"));

        await _service.CreateAsync("example.com", "www", "A", "1.2.3.4", 600, "admin");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.DeleteAsync("example.com", "rid-fail-del", "admin"));

        var saved = await _domainRepository.GetRecordAsync(DnsProviderType.Aliyun, "example.com", "rid-fail-del");
        Assert.Equal(DomainAssetStatus.Active, saved!.Status);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MinGo.CertManager.Application.Services;
using MinGo.CertManager.Core.Entities;
using MinGo.CertManager.Core.Services;
using MinGo.CertManager.Infrastructure.Data;
using MinGo.CertManager.Infrastructure.Repositories;
using Moq;
using Xunit;

namespace MinGo.CertManager.Tests.Services;

public class DomainSyncServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly DomainRepository _domainRepository;
    private readonly OperationLogRepository _logRepository;
    private readonly OperationLogService _logService;
    private readonly Mock<IDomainProviderClient> _providerMock;

    public DomainSyncServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);
        _domainRepository = new DomainRepository(_db);
        _logRepository = new OperationLogRepository(_db);
        _logService = new OperationLogService(_logRepository);
        _providerMock = new Mock<IDomainProviderClient>();
        _providerMock.SetupGet(x => x.ProviderType).Returns(DnsProviderType.Aliyun);
    }

    private DomainSyncService CreateService() =>
        new(_providerMock.Object, _domainRepository, _logService);

    [Fact]
    public async Task Sync_ShouldCreateZoneRecordsAndDerivedFqdns()
    {
        _providerMock.Setup(x => x.ListZonesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProviderZone> { new("example.com", "example.com") });
        _providerMock.Setup(x => x.ListRecordsAsync("example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProviderRecord>
            {
                new("r1", "www", "A", "1.2.3.4", 600),
                new("r2", "@", "A", "1.2.3.4", 600),
                new("r3", "*", "A", "1.2.3.4", 600)
            });

        var result = await CreateService().SyncFromProviderAsync("tester");

        Assert.Equal(1, result.ZonesAdded);
        Assert.Equal(0, result.ZonesRemoved);
        Assert.Equal(3, result.RecordsUpserted);
        Assert.True(result.DomainsAdded >= 3);

        var domains = await _domainRepository.GetDomainsAsync(DnsProviderType.Aliyun);
        Assert.Contains(domains, d => d.Fqdn == "example.com");
        Assert.Contains(domains, d => d.Fqdn == "www.example.com");
        Assert.Contains(domains, d => d.Fqdn == "*.example.com");
        Assert.All(domains, d => Assert.Equal(DomainAssetSource.Synced, d.Source));

        var logs = await _logRepository.QueryAsync();
        Assert.Contains(logs, l => l.Action == OperationLogAction.DomainSynced && l.Result == OperationLogResult.Success);
    }

    [Fact]
    public async Task Sync_ShouldSoftDeleteProviderSideRemovedAssets()
    {
        _providerMock.Setup(x => x.ListZonesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProviderZone> { new("example.com") });
        _providerMock.Setup(x => x.ListRecordsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProviderRecord> { new("r1", "www", "A", "1.2.3.4", 600) });

        var service = CreateService();
        await service.SyncFromProviderAsync();

        // 第二次：提供商删除 www 记录
        _providerMock.Setup(x => x.ListRecordsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProviderRecord>());

        await service.SyncFromProviderAsync();

        var www = await _domainRepository.GetByFqdnAsync(DnsProviderType.Aliyun, "www.example.com");
        Assert.NotNull(www);
        Assert.Equal(DomainAssetStatus.Removed, www!.Status);

        var zone = await _domainRepository.GetZoneAsync(DnsProviderType.Aliyun, "example.com");
        Assert.NotNull(zone);
        Assert.Equal(DomainAssetStatus.Active, zone!.Status);
    }

    [Fact]
    public async Task AddManual_ShouldAttachToMatchedZone()
    {
        _providerMock.Setup(x => x.ListZonesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProviderZone> { new("example.com") });
        _providerMock.Setup(x => x.ListRecordsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProviderRecord>());

        var service = CreateService();
        await service.SyncFromProviderAsync();

        var domain = await service.AddManualDomainAsync("dev.example.com", "测试", "tester");

        Assert.Equal(DomainAssetSource.Manual, domain.Source);
        Assert.Equal("example.com", domain.ZoneName);
        Assert.Equal(DomainAssetStatus.Active, domain.Status);
    }

    [Fact]
    public async Task AddManual_ShouldRejectWhenNoMatchedZone()
    {
        _providerMock.Setup(x => x.ListZonesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProviderZone> { new("example.com") });
        _providerMock.Setup(x => x.ListRecordsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProviderRecord>());

        var service = CreateService();
        await service.SyncFromProviderAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddManualDomainAsync("foo.other.org"));

        var logs = await _logRepository.QueryAsync(action: OperationLogAction.DomainAdded);
        Assert.Contains(logs, l => l.Result == OperationLogResult.Failed);
    }

    public void Dispose() => _db.Dispose();
}

public class OperationLogServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly OperationLogService _service;

    public OperationLogServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);
        _service = new OperationLogService(new OperationLogRepository(_db));
    }

    [Fact]
    public async Task LogAsync_ShouldPersistCoreFields()
    {
        await _service.LogAsync(
            OperationLogAction.DnsTxtCreated,
            OperationLogResult.Success,
            DnsProviderType.Aliyun,
            "DomainRecord",
            "rid-1",
            "www.example.com",
            "创建 DNS-01 TXT: _acme-challenge.www.example.com",
            actor: "alice");

        var (items, total) = await _service.QueryAsync();
        Assert.Equal(1, total);
        var log = items.Single();
        Assert.Equal("alice", log.Actor);
        Assert.Equal(OperationLogAction.DnsTxtCreated, log.Action);
        Assert.Equal(OperationLogResult.Success, log.Result);
        Assert.Equal("www.example.com", log.TargetName);
        Assert.False(string.IsNullOrEmpty(log.Message));
    }

    [Fact]
    public async Task QueryAsync_ShouldFilterByActionAndTarget()
    {
        await _service.LogAsync(OperationLogAction.DomainSynced, OperationLogResult.Success,
            DnsProviderType.Aliyun, "DomainSync", null, null, "sync");
        await _service.LogAsync(OperationLogAction.DnsTxtCreated, OperationLogResult.Success,
            DnsProviderType.Aliyun, "DomainRecord", null, "a.example.com", "txt");

        var (items, total) = await _service.QueryAsync(action: OperationLogAction.DnsTxtCreated);
        Assert.Equal(1, total);
        Assert.All(items, i => Assert.Equal(OperationLogAction.DnsTxtCreated, i.Action));

        (items, total) = await _service.QueryAsync(targetName: "a.example.com");
        Assert.Equal(1, total);
        Assert.Equal("a.example.com", items.Single().TargetName);
    }

    [Fact]
    public void Sanitize_ShouldMaskOpaqueSecrets()
    {
        var masked = OperationLogService.Sanitize("failed key ABCDEFGHIJKLMNOPQRSTUVWXYZ012345 ok");
        Assert.Contains("***", masked);
        Assert.Contains("ok", masked);
    }

    public void Dispose() => _db.Dispose();
}

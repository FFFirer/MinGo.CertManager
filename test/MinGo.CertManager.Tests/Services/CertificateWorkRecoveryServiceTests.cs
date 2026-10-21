using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MinGo.CertManager.Core.Entities;
using MinGo.CertManager.Infrastructure.Data;
using MinGo.CertManager.Infrastructure.Jobs;
using MinGo.CertManager.Infrastructure.Repositories;
using MinGo.CertManager.Infrastructure.Services;
using Moq;
using Quartz;

namespace MinGo.CertManager.Tests.Services;

public class CertificateWorkRecoveryServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly CertificateRepository _repository;
    private readonly ServiceProvider _provider;
    private readonly Mock<IScheduler> _schedulerMock;
    private readonly Mock<ISchedulerFactory> _schedulerFactoryMock;
    private readonly CertificateWorkRecoveryService _service;

    public CertificateWorkRecoveryServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);
        _repository = new CertificateRepository(_db);

        var services = new ServiceCollection();
        services.AddScoped(_ => _db);
        services.AddScoped<ICertificateRepository>(_ => _repository);
        _provider = services.BuildServiceProvider();

        _schedulerMock = new Mock<IScheduler>();
        _schedulerMock
            .Setup(x => x.CheckExists(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _schedulerMock
            .Setup(x => x.ScheduleJob(
                It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DateTimeOffset.UtcNow);

        _schedulerFactoryMock = new Mock<ISchedulerFactory>();
        _schedulerFactoryMock
            .Setup(x => x.GetScheduler(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_schedulerMock.Object);

        _service = new CertificateWorkRecoveryService(
            _provider.GetRequiredService<IServiceScopeFactory>(),
            _schedulerFactoryMock.Object,
            Mock.Of<ILogger<CertificateWorkRecoveryService>>());
    }

    public void Dispose()
    {
        _provider.Dispose();
        _db.Dispose();
    }

    private Certificate PendingCert(string domain, DateTime? updatedAt = null)
    {
        var at = updatedAt ?? DateTime.UtcNow.AddHours(-2);
        return new Certificate
        {
            Id = Guid.NewGuid(),
            Domain = domain,
            Status = CertificateStatus.Pending,
            AcmeStatus = AcmeProcessStatus.CreatingOrder,
            AcmeStatusMessage = "创建ACME订单",
            CreatedAt = at,
            UpdatedAt = at
        };
    }

    [Fact]
    public async Task Recover_Startup_ResetsAcmeStatus_AndSchedulesJob()
    {
        var cert = PendingCert("stuck.example.com");
        await _repository.AddAsync(cert);

        await _service.RecoverAsync(staleBeforeUtc: null, CancellationToken.None);

        var after = await _repository.GetByIdAsync(cert.Id);
        Assert.NotNull(after);
        Assert.Equal(AcmeProcessStatus.Initializing, after!.AcmeStatus);
        Assert.Equal(CertificateStatus.Pending, after.Status);

        _schedulerMock.Verify(x => x.ScheduleJob(
            It.Is<IJobDetail>(j => j.Key.Name == $"CertificateRequest-{cert.Id}"),
            It.IsAny<ITrigger>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Recover_SkipsNonPending()
    {
        var active = new Certificate
        {
            Id = Guid.NewGuid(),
            Domain = "active.example.com",
            Status = CertificateStatus.Active,
            AcmeStatus = AcmeProcessStatus.Completed,
            CertificateContent = "c",
            PrivateKey = "k",
            CertificateChain = "ch",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(active);

        await _service.RecoverAsync(staleBeforeUtc: null, CancellationToken.None);

        _schedulerMock.Verify(x => x.ScheduleJob(
            It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Recover_StaleFilter_IgnoresFreshPending()
    {
        var fresh = PendingCert("fresh.example.com", DateTime.UtcNow);
        var stale = PendingCert("stale.example.com", DateTime.UtcNow.AddHours(-2));
        await _repository.AddAsync(fresh);
        await _repository.AddAsync(stale);

        await _service.RecoverAsync(DateTime.UtcNow.AddMinutes(-15), CancellationToken.None);

        var freshAfter = await _repository.GetByIdAsync(fresh.Id);
        Assert.Equal(AcmeProcessStatus.CreatingOrder, freshAfter!.AcmeStatus);

        var staleAfter = await _repository.GetByIdAsync(stale.Id);
        Assert.Equal(AcmeProcessStatus.Initializing, staleAfter!.AcmeStatus);

        _schedulerMock.Verify(x => x.ScheduleJob(
            It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Recover_SkipsWhenJobAlreadyScheduled()
    {
        var cert = PendingCert("busy.example.com");
        await _repository.AddAsync(cert);

        _schedulerMock
            .Setup(x => x.CheckExists(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _service.RecoverAsync(staleBeforeUtc: null, CancellationToken.None);

        _schedulerMock.Verify(x => x.ScheduleJob(
            It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Recover_RecoversRenewalNewRecord()
    {
        var sourceId = Guid.NewGuid();
        var renewed = PendingCert("renew.example.com");
        renewed.RenewedFromId = sourceId;
        await _repository.AddAsync(renewed);

        await _service.RecoverAsync(staleBeforeUtc: null, CancellationToken.None);

        var after = await _repository.GetByIdAsync(renewed.Id);
        Assert.Equal(AcmeProcessStatus.Initializing, after!.AcmeStatus);
        Assert.Equal(sourceId, after.RenewedFromId);

        _schedulerMock.Verify(x => x.ScheduleJob(
            It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

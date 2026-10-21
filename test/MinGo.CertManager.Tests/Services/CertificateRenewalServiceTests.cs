using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MinGo.CertManager.Application.Services;
using MinGo.CertManager.Core.Entities;
using MinGo.CertManager.Core.Services;
using MinGo.CertManager.Infrastructure.Configuration;
using MinGo.CertManager.Infrastructure.Data;
using MinGo.CertManager.Infrastructure.Repositories;
using Moq;

namespace MinGo.CertManager.Tests.Services;

public class CertificateRenewalServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly CertificateRepository _certificateRepository;
    private readonly OperationLogService _logService;
    private readonly Mock<IAcmeService> _acmeMock;
    private readonly Mock<IDomainProviderClient> _providerMock;
    private readonly Mock<IDomainRepository> _domainRepositoryMock;
    private readonly CertificateService _service;

    public CertificateRenewalServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);
        _certificateRepository = new CertificateRepository(_db);
        _logService = new OperationLogService(new OperationLogRepository(_db));

        _acmeMock = new Mock<IAcmeService>();
        _providerMock = new Mock<IDomainProviderClient>();
        _providerMock.SetupGet(x => x.ProviderType).Returns(DnsProviderType.Aliyun);
        _domainRepositoryMock = new Mock<IDomainRepository>();
        _domainRepositoryMock
            .Setup(x => x.GetByFqdnAsync(It.IsAny<DnsProviderType>(), It.IsAny<string>()))
            .ReturnsAsync((Domain?)null);
        _domainRepositoryMock
            .Setup(x => x.GetActiveZoneNamesAsync(It.IsAny<DnsProviderType>()))
            .ReturnsAsync(new List<string>());

        var settings = Options.Create(new CertificateSettings
        {
            RenewalDaysBeforeExpiry = 30,
            ValidityDays = 90
        });

        _service = new CertificateService(
            _certificateRepository,
            _acmeMock.Object,
            Mock.Of<ILogger<CertificateService>>(),
            Mock.Of<ILoggerFactory>(),
            settings,
            _providerMock.Object,
            _logService,
            _domainRepositoryMock.Object);
    }

    public void Dispose() => _db.Dispose();

    private Certificate CreateActiveSource(DateTime? expiresAt = null)
    {
        return new Certificate
        {
            Id = Guid.NewGuid(),
            Domain = "example.com",
            IsWildcard = false,
            UseStaging = false,
            Status = CertificateStatus.Active,
            AcmeStatus = AcmeProcessStatus.Completed,
            IssuedAt = DateTime.UtcNow.AddDays(-60),
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(25),
            CertificateContent = "old-cert",
            PrivateKey = "old-key",
            CertificateChain = "old-chain",
            CreatedAt = DateTime.UtcNow.AddDays(-60),
            UpdatedAt = DateTime.UtcNow.AddDays(-60)
        };
    }

    private static CertificateResult SuccessResult()
    {
        return new CertificateResult
        {
            CertificatePem = "-----BEGIN CERTIFICATE-----\nNEW\n-----END CERTIFICATE-----",
            PrivateKeyPem = "-----BEGIN PRIVATE KEY-----\nNEWKEY\n-----END PRIVATE KEY-----",
            CertificateChainPem = "-----BEGIN CERTIFICATE-----\nCHAIN\n-----END CERTIFICATE-----"
        };
    }

    [Fact]
    public async Task Renew_Success_CreatesNewRecord_AndKeepsSourceUntouched()
    {
        var source = CreateActiveSource();
        await _certificateRepository.AddAsync(source);

        _acmeMock
            .Setup(x => x.RequestCertificateAsync(
                source.Domain, source.IsWildcard, It.IsAny<IDomainProviderClient>(), source.UseStaging, It.IsAny<string?>()))
            .ReturnsAsync(SuccessResult());

        var renewed = await _service.RenewCertificateAsync(source.Id);

        Assert.NotEqual(source.Id, renewed.Id);
        Assert.Equal(source.Id, renewed.RenewedFromId);
        Assert.Equal(source.Domain, renewed.Domain);
        Assert.Equal(CertificateStatus.Active, renewed.Status);
        Assert.Equal("-----BEGIN CERTIFICATE-----\nNEW\n-----END CERTIFICATE-----", renewed.CertificateContent);

        var sourceAfter = await _certificateRepository.GetByIdAsync(source.Id);
        Assert.NotNull(sourceAfter);
        Assert.Equal(CertificateStatus.Active, sourceAfter!.Status);
        Assert.Equal("old-cert", sourceAfter.CertificateContent);
        Assert.Equal("old-key", sourceAfter.PrivateKey);
        Assert.Null(sourceAfter.RenewedFromId);

        var logs = await _logService.QueryAsync(action: OperationLogAction.CertRenewed);
        Assert.Equal(1, logs.Total);
    }

    [Fact]
    public async Task Renew_Failure_KeepsFailedNewRecord_AndSourceStillExportable()
    {
        var source = CreateActiveSource();
        await _certificateRepository.AddAsync(source);

        _acmeMock
            .Setup(x => x.RequestCertificateAsync(
                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IDomainProviderClient>(), It.IsAny<bool>(), It.IsAny<string?>()))
            .ThrowsAsync(new InvalidOperationException("dns challenge failed"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.RenewCertificateAsync(source.Id));

        var sourceAfter = await _certificateRepository.GetByIdAsync(source.Id);
        Assert.NotNull(sourceAfter);
        Assert.Equal(CertificateStatus.Active, sourceAfter!.Status);
        Assert.Equal("old-cert", sourceAfter.CertificateContent);

        // 源证书仍可导出
        var export = await _service.ExportCertificateAsync(source.Id, CertificateFormat.Pem);
        Assert.NotEmpty(export);

        // 失败的新记录保留
        var all = await _certificateRepository.GetAllAsync();
        var failed = all.Single(c => c.RenewedFromId == source.Id);
        Assert.Equal(CertificateStatus.Failed, failed.Status);

        var logs = await _logService.QueryAsync(action: OperationLogAction.CertRenewFailed);
        Assert.Equal(1, logs.Total);
    }

    [Fact]
    public async Task Renew_Rejected_WhenSourceNotActive()
    {
        var source = CreateActiveSource();
        source.Status = CertificateStatus.Failed;
        await _certificateRepository.AddAsync(source);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.RenewCertificateAsync(source.Id));
    }

    [Fact]
    public async Task Renew_Rejected_WhenPendingInFlight()
    {
        var source = CreateActiveSource();
        await _certificateRepository.AddAsync(source);

        var pending = new Certificate
        {
            Id = Guid.NewGuid(),
            Domain = source.Domain,
            IsWildcard = source.IsWildcard,
            UseStaging = source.UseStaging,
            Status = CertificateStatus.Pending,
            AcmeStatus = AcmeProcessStatus.CreatingOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _certificateRepository.AddAsync(pending);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.RenewCertificateAsync(source.Id));

        // 未创建新续期记录
        var all = await _certificateRepository.GetAllAsync();
        Assert.DoesNotContain(all, c => c.RenewedFromId == source.Id);
    }
}

using Microsoft.EntityFrameworkCore;
using MinGo.CertManager.Core.Entities;
using MinGo.CertManager.Infrastructure.Data;
using MinGo.CertManager.Infrastructure.Repositories;

namespace MinGo.CertManager.Tests.Repositories;

public class CertificateRenewalCandidateTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly CertificateRepository _repository;

    public CertificateRenewalCandidateTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);
        _repository = new CertificateRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    private Certificate Cert(
        string domain,
        CertificateStatus status,
        DateTime expiresAt,
        bool useStaging = false,
        bool isWildcard = false,
        AcmeProcessStatus acme = AcmeProcessStatus.Completed)
    {
        return new Certificate
        {
            Id = Guid.NewGuid(),
            Domain = domain,
            IsWildcard = isWildcard,
            UseStaging = useStaging,
            Status = status,
            AcmeStatus = acme,
            IssuedAt = expiresAt.AddDays(-90),
            ExpiresAt = expiresAt,
            CertificateContent = "c",
            PrivateKey = "k",
            CertificateChain = "ch",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task GetRenewalCandidates_SelectsExpiringActiveWithoutSuccessorOrPending()
    {
        var soon = DateTime.UtcNow.AddDays(10);
        var source = Cert("a.com", CertificateStatus.Active, soon);
        await _repository.AddAsync(source);

        var candidates = await _repository.GetRenewalCandidatesAsync(DateTime.UtcNow.AddDays(30));

        Assert.Single(candidates);
        Assert.Equal(source.Id, candidates[0].Id);
    }

    [Fact]
    public async Task GetRenewalCandidates_SkipsWhenNewerActiveExists()
    {
        var old = Cert("a.com", CertificateStatus.Active, DateTime.UtcNow.AddDays(10));
        var newer = Cert("a.com", CertificateStatus.Active, DateTime.UtcNow.AddDays(80));
        await _repository.AddAsync(old);
        await _repository.AddAsync(newer);

        var candidates = await _repository.GetRenewalCandidatesAsync(DateTime.UtcNow.AddDays(30));

        Assert.Empty(candidates);
    }

    [Fact]
    public async Task GetRenewalCandidates_SkipsWhenPendingInFlight()
    {
        var source = Cert("a.com", CertificateStatus.Active, DateTime.UtcNow.AddDays(10));
        var pending = Cert("a.com", CertificateStatus.Pending, DateTime.UtcNow.AddDays(90), acme: AcmeProcessStatus.CreatingOrder);
        await _repository.AddAsync(source);
        await _repository.AddAsync(pending);

        var candidates = await _repository.GetRenewalCandidatesAsync(DateTime.UtcNow.AddDays(30));

        Assert.Empty(candidates);
    }

    [Fact]
    public async Task GetRenewalCandidates_IgnoresNonActiveAndOutOfRange()
    {
        await _repository.AddAsync(Cert("b.com", CertificateStatus.Failed, DateTime.UtcNow.AddDays(5)));
        await _repository.AddAsync(Cert("c.com", CertificateStatus.Active, DateTime.UtcNow.AddDays(60)));
        await _repository.AddAsync(Cert("d.com", CertificateStatus.Expired, DateTime.UtcNow.AddDays(5)));

        var candidates = await _repository.GetRenewalCandidatesAsync(DateTime.UtcNow.AddDays(30));

        Assert.Empty(candidates);
    }

    [Fact]
    public async Task GetRenewalCandidates_DoesNotMixStagingAndProduction()
    {
        var prod = Cert("a.com", CertificateStatus.Active, DateTime.UtcNow.AddDays(10));
        var stagingNewer = Cert("a.com", CertificateStatus.Active, DateTime.UtcNow.AddDays(80), useStaging: true);
        await _repository.AddAsync(prod);
        await _repository.AddAsync(stagingNewer);

        var candidates = await _repository.GetRenewalCandidatesAsync(DateTime.UtcNow.AddDays(30));

        Assert.Single(candidates);
        Assert.Equal(prod.Id, candidates[0].Id);
    }

    [Fact]
    public async Task GetLatestValid_ReturnsActiveWithLatestExpiry()
    {
        var older = Cert("a.com", CertificateStatus.Active, DateTime.UtcNow.AddDays(25));
        var newer = Cert("a.com", CertificateStatus.Active, DateTime.UtcNow.AddDays(80));
        await _repository.AddAsync(older);
        await _repository.AddAsync(newer);

        var latest = await _repository.GetLatestValidCertificateByDomainAsync("a.com");

        Assert.NotNull(latest);
        Assert.Equal(newer.Id, latest!.Id);
    }
}

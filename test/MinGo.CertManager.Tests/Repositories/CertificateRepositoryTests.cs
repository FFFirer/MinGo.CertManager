using System;
using System.Threading.Tasks;
using MinGo.CertManager.Core.Entities;
using MinGo.CertManager.Infrastructure.Data;
using MinGo.CertManager.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MinGo.CertManager.Tests.Repositories;

public class CertificateRepositoryTests
{
    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task AddAsync_ShouldAddCertificate()
    {
        var context = CreateInMemoryContext();
        var repository = new CertificateRepository(context);

        var certificate = new Certificate
        {
            Id = Guid.NewGuid(),
            Domain = "example.com",
            IsWildcard = false,
            Status = CertificateStatus.Active,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(90),
            CertificateContent = "cert-content",
            PrivateKey = "private-key",
            CertificateChain = "cert-chain",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await repository.AddAsync(certificate);

        Assert.NotNull(result);
        Assert.Equal(certificate.Domain, result.Domain);
        Assert.Equal(CertificateStatus.Active, result.Status);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnCertificate()
    {
        var context = CreateInMemoryContext();
        var repository = new CertificateRepository(context);

        var certificate = new Certificate
        {
            Id = Guid.NewGuid(),
            Domain = "example.com",
            IsWildcard = false,
            Status = CertificateStatus.Active,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(90),
            CertificateContent = "cert-content",
            PrivateKey = "private-key",
            CertificateChain = "cert-chain",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await repository.AddAsync(certificate);

        var result = await repository.GetByIdAsync(certificate.Id);

        Assert.NotNull(result);
        Assert.Equal(certificate.Id, result.Id);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllCertificates()
    {
        var context = CreateInMemoryContext();
        var repository = new CertificateRepository(context);

        var certificate1 = new Certificate
        {
            Id = Guid.NewGuid(),
            Domain = "example1.com",
            IsWildcard = false,
            Status = CertificateStatus.Active,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(90),
            CertificateContent = "cert-content-1",
            PrivateKey = "private-key-1",
            CertificateChain = "cert-chain-1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var certificate2 = new Certificate
        {
            Id = Guid.NewGuid(),
            Domain = "example2.com",
            IsWildcard = true,
            Status = CertificateStatus.Active,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(90),
            CertificateContent = "cert-content-2",
            PrivateKey = "private-key-2",
            CertificateChain = "cert-chain-2",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await repository.AddAsync(certificate1);
        await repository.AddAsync(certificate2);

        var result = await repository.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task SearchByDomainAsync_ShouldReturnMatchingCertificates()
    {
        var context = CreateInMemoryContext();
        var repository = new CertificateRepository(context);

        var certificate1 = new Certificate
        {
            Id = Guid.NewGuid(),
            Domain = "test.example.com",
            IsWildcard = false,
            Status = CertificateStatus.Active,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(90),
            CertificateContent = "cert-content-1",
            PrivateKey = "private-key-1",
            CertificateChain = "cert-chain-1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var certificate2 = new Certificate
        {
            Id = Guid.NewGuid(),
            Domain = "other.com",
            IsWildcard = false,
            Status = CertificateStatus.Active,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(90),
            CertificateContent = "cert-content-2",
            PrivateKey = "private-key-2",
            CertificateChain = "cert-chain-2",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await repository.AddAsync(certificate1);
        await repository.AddAsync(certificate2);

        var result = await repository.SearchByDomainAsync("example");

        Assert.Single(result);
        Assert.Contains("example", result[0].Domain);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateCertificate()
    {
        var context = CreateInMemoryContext();
        var repository = new CertificateRepository(context);

        var certificate = new Certificate
        {
            Id = Guid.NewGuid(),
            Domain = "example.com",
            IsWildcard = false,
            Status = CertificateStatus.Active,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(90),
            CertificateContent = "cert-content",
            PrivateKey = "private-key",
            CertificateChain = "cert-chain",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await repository.AddAsync(certificate);

        certificate.Status = CertificateStatus.Expired;
        certificate.UpdatedAt = DateTime.UtcNow;

        var result = await repository.UpdateAsync(certificate);

        Assert.Equal(CertificateStatus.Expired, result.Status);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteCertificate()
    {
        var context = CreateInMemoryContext();
        var repository = new CertificateRepository(context);

        var certificate = new Certificate
        {
            Id = Guid.NewGuid(),
            Domain = "example.com",
            IsWildcard = false,
            Status = CertificateStatus.Active,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(90),
            CertificateContent = "cert-content",
            PrivateKey = "private-key",
            CertificateChain = "cert-chain",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await repository.AddAsync(certificate);
        await repository.DeleteAsync(certificate.Id);

        var result = await repository.GetByIdAsync(certificate.Id);

        Assert.Null(result);
    }
}

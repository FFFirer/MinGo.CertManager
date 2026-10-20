using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MinGo.CertManager.Core.Entities;

namespace MinGo.CertManager.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Certificate> Certificates { get; set; } = null!;
    public DbSet<DnsProvider> DnsProviders { get; set; } = null!;
    public DbSet<AcmeAccount> AcmeAccounts { get; set; } = null!;
    public DbSet<ApiKey> ApiKeys { get; set; } = null!;
    public DbSet<DomainZone> DomainZones { get; set; } = null!;
    public DbSet<Domain> Domains { get; set; } = null!;
    public DbSet<DomainRecord> DomainRecords { get; set; } = null!;
    public DbSet<OperationLog> OperationLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Certificate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Domain).IsRequired().HasMaxLength(255);
            entity.Property(e => e.CertificateContent).IsRequired();
            entity.Property(e => e.PrivateKey).IsRequired();
            entity.Property(e => e.CertificateChain).IsRequired();
            entity.Property(e => e.Status).HasDefaultValue(CertificateStatus.Pending);
        });

        modelBuilder.Entity<DnsProvider>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AccessKeyId).IsRequired().HasMaxLength(128);
            entity.Property(e => e.AccessKeySecret).IsRequired().HasMaxLength(128);
            entity.Property(e => e.RegionId).HasMaxLength(64);
        });

        modelBuilder.Entity<AcmeAccount>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AccountId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.AccountKey).IsRequired();
            entity.Property(e => e.Contact).IsRequired().HasMaxLength(255);
            entity.Property(e => e.AcmeServerUrl).IsRequired().HasMaxLength(512);
            entity.HasIndex(e => new { e.AcmeServerUrl, e.Contact }).IsUnique();
        });

        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ApiKeyString).IsRequired().HasMaxLength(128);
            entity.Property(e => e.ApiKeyHash).IsRequired().HasMaxLength(64);
            entity.Property(e => e.ApiSecretString).IsRequired().HasMaxLength(128);
            entity.Property(e => e.ApiSecretHash).IsRequired().HasMaxLength(64);
            entity.Property(e => e.Status).HasDefaultValue(1);
            entity.Property(e => e.RateLimitQpm).HasDefaultValue(1000);
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.HasIndex(e => e.ApiKeyHash).IsUnique();
        });

        modelBuilder.Entity<DomainZone>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ZoneName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ProviderZoneId).HasMaxLength(128);
            entity.HasIndex(e => new { e.ProviderType, e.ZoneName }).IsUnique();
        });

        modelBuilder.Entity<Domain>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Fqdn).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ZoneName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Remark).HasMaxLength(512);
            entity.HasIndex(e => new { e.ProviderType, e.Fqdn }).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ZoneName);
        });

        modelBuilder.Entity<DomainRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ZoneName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ProviderRecordId).IsRequired().HasMaxLength(128);
            entity.Property(e => e.Rr).IsRequired().HasMaxLength(255);
            entity.Property(e => e.RecordType).IsRequired().HasMaxLength(32);
            entity.Property(e => e.Value).IsRequired().HasMaxLength(1024);
            entity.Property(e => e.Fqdn).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => new { e.ProviderType, e.ZoneName, e.ProviderRecordId }).IsUnique();
            entity.HasIndex(e => e.Fqdn);
        });

        modelBuilder.Entity<OperationLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Actor).IsRequired().HasMaxLength(128);
            entity.Property(e => e.TargetType).IsRequired().HasMaxLength(64);
            entity.Property(e => e.TargetId).HasMaxLength(64);
            entity.Property(e => e.TargetName).HasMaxLength(255);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.TraceId).HasMaxLength(64);
            entity.HasIndex(e => e.At);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.TargetName);
        });
    }
}

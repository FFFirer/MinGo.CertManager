using Microsoft.EntityFrameworkCore;
using MinGo.CertManager.Core.Entities;

namespace MinGo.CertManager.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Certificate> Certificates { get; set; } = null!;
    public DbSet<DnsProvider> DnsProviders { get; set; } = null!;
    public DbSet<AcmeAccount> AcmeAccounts { get; set; } = null!;

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
    }
}

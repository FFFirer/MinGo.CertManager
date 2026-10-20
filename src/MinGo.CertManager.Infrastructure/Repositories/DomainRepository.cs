using Microsoft.EntityFrameworkCore;
using MinGo.CertManager.Core.Entities;
using MinGo.CertManager.Infrastructure.Data;

namespace MinGo.CertManager.Infrastructure.Repositories;

public interface IDomainRepository
{
    Task<DomainZone?> GetZoneAsync(DnsProviderType providerType, string zoneName);
    Task<List<DomainZone>> GetZonesAsync(DnsProviderType providerType);
    Task<DomainZone> UpsertZoneAsync(DnsProviderType providerType, string zoneName, string? providerZoneId);
    Task MarkZoneRemovedAsync(Guid zoneId);

    Task<Domain?> GetByFqdnAsync(DnsProviderType providerType, string fqdn);
    Task<List<Domain>> GetDomainsAsync(DnsProviderType providerType, DomainAssetStatus? status = null);
    Task<List<Domain>> GetActiveDomainsAsync(DnsProviderType providerType);
    Task<Domain> UpsertDomainAsync(DnsProviderType providerType, string fqdn, string zoneName, DomainAssetSource source);
    Task MarkDomainRemovedAsync(Guid domainId);

    Task<List<DomainRecord>> GetRecordsAsync(DnsProviderType providerType, string zoneName, DomainAssetStatus? status = null);
    Task<DomainRecord?> GetRecordAsync(DnsProviderType providerType, string zoneName, string providerRecordId);
    Task<DomainRecord> UpsertRecordAsync(DnsProviderType providerType, string zoneName, string providerRecordId,
        string rr, string recordType, string value, int ttl, string fqdn);
    Task MarkRecordRemovedAsync(Guid recordId);
    Task<List<string>> GetActiveZoneNamesAsync(DnsProviderType providerType);
}

public class DomainRepository : IDomainRepository
{
    private readonly ApplicationDbContext _context;

    public DomainRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<DomainZone?> GetZoneAsync(DnsProviderType providerType, string zoneName)
    {
        return _context.DomainZones.FirstOrDefaultAsync(z =>
            z.ProviderType == providerType && z.ZoneName == zoneName);
    }

    public Task<List<DomainZone>> GetZonesAsync(DnsProviderType providerType)
    {
        return _context.DomainZones
            .Where(z => z.ProviderType == providerType)
            .OrderBy(z => z.ZoneName)
            .ToListAsync();
    }

    public async Task<DomainZone> UpsertZoneAsync(DnsProviderType providerType, string zoneName, string? providerZoneId)
    {
        var now = DateTime.UtcNow;
        var zone = await GetZoneAsync(providerType, zoneName);
        if (zone is null)
        {
            zone = new DomainZone
            {
                Id = Guid.NewGuid(),
                ProviderType = providerType,
                ZoneName = zoneName,
                ProviderZoneId = providerZoneId,
                Status = DomainAssetStatus.Active,
                CreatedAt = now,
                UpdatedAt = now
            };
            _context.DomainZones.Add(zone);
        }
        else
        {
            zone.ProviderZoneId = providerZoneId ?? zone.ProviderZoneId;
            zone.Status = DomainAssetStatus.Active;
            zone.RemovedAt = null;
            zone.UpdatedAt = now;
        }

        await _context.SaveChangesAsync();
        return zone;
    }

    public async Task MarkZoneRemovedAsync(Guid zoneId)
    {
        var zone = await _context.DomainZones.FindAsync(zoneId);
        if (zone is null || zone.Status == DomainAssetStatus.Removed)
        {
            return;
        }

        zone.Status = DomainAssetStatus.Removed;
        zone.RemovedAt = DateTime.UtcNow;
        zone.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public Task<Domain?> GetByFqdnAsync(DnsProviderType providerType, string fqdn)
    {
        return _context.Domains.FirstOrDefaultAsync(d =>
            d.ProviderType == providerType && d.Fqdn == fqdn);
    }

    public Task<List<Domain>> GetDomainsAsync(DnsProviderType providerType, DomainAssetStatus? status = null)
    {
        var query = _context.Domains.Where(d => d.ProviderType == providerType);
        if (status.HasValue)
        {
            query = query.Where(d => d.Status == status.Value);
        }

        return query.OrderBy(d => d.Fqdn).ToListAsync();
    }

    public Task<List<Domain>> GetActiveDomainsAsync(DnsProviderType providerType)
    {
        return _context.Domains
            .Where(d => d.ProviderType == providerType && d.Status == DomainAssetStatus.Active)
            .OrderBy(d => d.Fqdn)
            .ToListAsync();
    }

    public async Task<Domain> UpsertDomainAsync(DnsProviderType providerType, string fqdn, string zoneName, DomainAssetSource source)
    {
        var now = DateTime.UtcNow;
        var domain = await GetByFqdnAsync(providerType, fqdn);
        if (domain is null)
        {
            domain = new Domain
            {
                Id = Guid.NewGuid(),
                ProviderType = providerType,
                Fqdn = fqdn,
                ZoneName = zoneName,
                Source = source,
                Status = DomainAssetStatus.Active,
                CreatedAt = now,
                UpdatedAt = now
            };
            _context.Domains.Add(domain);
        }
        else
        {
            domain.ZoneName = zoneName;
            domain.Status = DomainAssetStatus.Active;
            domain.RemovedAt = null;
            // 同步来源不覆盖手动备注/来源：若已是 Manual 且本次是 Synced，保持 Manual 仅更新状态
            if (source == DomainAssetSource.Manual)
            {
                domain.Source = DomainAssetSource.Manual;
            }

            domain.UpdatedAt = now;
        }

        await _context.SaveChangesAsync();
        return domain;
    }

    public async Task MarkDomainRemovedAsync(Guid domainId)
    {
        var domain = await _context.Domains.FindAsync(domainId);
        if (domain is null || domain.Status == DomainAssetStatus.Removed)
        {
            return;
        }

        domain.Status = DomainAssetStatus.Removed;
        domain.RemovedAt = DateTime.UtcNow;
        domain.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public Task<List<DomainRecord>> GetRecordsAsync(DnsProviderType providerType, string zoneName, DomainAssetStatus? status = null)
    {
        var query = _context.DomainRecords.Where(r =>
            r.ProviderType == providerType && r.ZoneName == zoneName);
        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        return query.OrderBy(r => r.Rr).ThenBy(r => r.RecordType).ToListAsync();
    }

    public Task<DomainRecord?> GetRecordAsync(DnsProviderType providerType, string zoneName, string providerRecordId)
    {
        return _context.DomainRecords.FirstOrDefaultAsync(r =>
            r.ProviderType == providerType &&
            r.ZoneName == zoneName &&
            r.ProviderRecordId == providerRecordId);
    }

    public async Task<DomainRecord> UpsertRecordAsync(DnsProviderType providerType, string zoneName, string providerRecordId,
        string rr, string recordType, string value, int ttl, string fqdn)
    {
        var now = DateTime.UtcNow;
        var record = await GetRecordAsync(providerType, zoneName, providerRecordId);
        if (record is null)
        {
            record = new DomainRecord
            {
                Id = Guid.NewGuid(),
                ProviderType = providerType,
                ZoneName = zoneName,
                ProviderRecordId = providerRecordId,
                Rr = rr,
                RecordType = recordType,
                Value = value,
                Ttl = ttl,
                Fqdn = fqdn,
                Status = DomainAssetStatus.Active,
                CreatedAt = now,
                UpdatedAt = now
            };
            _context.DomainRecords.Add(record);
        }
        else
        {
            record.Rr = rr;
            record.RecordType = recordType;
            record.Value = value;
            record.Ttl = ttl;
            record.Fqdn = fqdn;
            record.Status = DomainAssetStatus.Active;
            record.RemovedAt = null;
            record.UpdatedAt = now;
        }

        await _context.SaveChangesAsync();
        return record;
    }

    public async Task MarkRecordRemovedAsync(Guid recordId)
    {
        var record = await _context.DomainRecords.FindAsync(recordId);
        if (record is null || record.Status == DomainAssetStatus.Removed)
        {
            return;
        }

        record.Status = DomainAssetStatus.Removed;
        record.RemovedAt = DateTime.UtcNow;
        record.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<List<string>> GetActiveZoneNamesAsync(DnsProviderType providerType)
    {
        return await _context.DomainZones
            .Where(z => z.ProviderType == providerType && z.Status == DomainAssetStatus.Active)
            .Select(z => z.ZoneName)
            .ToListAsync();
    }
}

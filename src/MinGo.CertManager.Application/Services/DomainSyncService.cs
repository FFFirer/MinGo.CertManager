using MinGo.CertManager.Core.Entities;
using MinGo.CertManager.Core.Services;
using MinGo.CertManager.Infrastructure.Repositories;

namespace MinGo.CertManager.Application.Services;

/// <summary>
/// 域名资产同步结果摘要
/// </summary>
public record DomainSyncResult(
    int ZonesAdded,
    int ZonesRemoved,
    int RecordsUpserted,
    int RecordsRemoved,
    int DomainsAdded,
    int DomainsRemoved);

/// <summary>
/// 从提供商同步 Zone / 解析记录 / FQDN 资产
/// </summary>
public interface IDomainSyncService
{
    Task<DomainSyncResult> SyncFromProviderAsync(string? actor = null, CancellationToken ct = default);
    Task<Domain> AddManualDomainAsync(string fqdn, string? remark = null, string? actor = null);
}

public class DomainSyncService : IDomainSyncService
{
    private readonly IDomainProviderClient _providerClient;
    private readonly IDomainRepository _domainRepository;
    private readonly IOperationLogService _operationLogService;

    public DomainSyncService(
        IDomainProviderClient providerClient,
        IDomainRepository domainRepository,
        IOperationLogService operationLogService)
    {
        _providerClient = providerClient;
        _domainRepository = domainRepository;
        _operationLogService = operationLogService;
    }

    public async Task<DomainSyncResult> SyncFromProviderAsync(string? actor = null, CancellationToken ct = default)
    {
        var providerType = _providerClient.ProviderType;
        var zonesAdded = 0;
        var zonesRemoved = 0;
        var recordsUpserted = 0;
        var recordsRemoved = 0;
        var domainsAdded = 0;
        var domainsRemoved = 0;

        try
        {
            var remoteZones = await _providerClient.ListZonesAsync(ct);
            var remoteZoneNames = new HashSet<string>(
                remoteZones.Select(z => z.ZoneName.Trim().TrimEnd('.').ToLowerInvariant()),
                StringComparer.OrdinalIgnoreCase);

            var localZones = await _domainRepository.GetZonesAsync(providerType);
            var seenZoneIds = new HashSet<Guid>();

            foreach (var remote in remoteZones)
            {
                var zoneName = remote.ZoneName.Trim().TrimEnd('.').ToLowerInvariant();
                var existing = await _domainRepository.GetZoneAsync(providerType, zoneName);
                var wasNew = existing is null || existing.Status == DomainAssetStatus.Removed;

                await _domainRepository.UpsertZoneAsync(providerType, zoneName, remote.ProviderZoneId);
                if (wasNew)
                {
                    zonesAdded++;
                }

                var zone = await _domainRepository.GetZoneAsync(providerType, zoneName);
                if (zone is not null)
                {
                    seenZoneIds.Add(zone.Id);
                }

                // Zone 本身作为 FQDN 资产
                var zoneDomainExisting = await _domainRepository.GetByFqdnAsync(providerType, zoneName);
                if (zoneDomainExisting is null || zoneDomainExisting.Status == DomainAssetStatus.Removed)
                {
                    if (zoneDomainExisting is null)
                    {
                        domainsAdded++;
                    }
                }

                await _domainRepository.UpsertDomainAsync(providerType, zoneName, zoneName, DomainAssetSource.Synced);

                var remoteRecords = await _providerClient.ListRecordsAsync(zoneName, ct);
                var seenRecordIds = new HashSet<Guid>();
                var derivedFqdns = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { zoneName };

                foreach (var record in remoteRecords)
                {
                    var fqdn = DomainNameHelper.ToFqdn(zoneName, record.Rr);
                    derivedFqdns.Add(fqdn);

                    var existingRecord = await _domainRepository.GetRecordAsync(providerType, zoneName, record.ProviderRecordId);
                    if (existingRecord is null)
                    {
                        recordsUpserted++;
                    }

                    await _domainRepository.UpsertRecordAsync(
                        providerType, zoneName, record.ProviderRecordId,
                        record.Rr, record.RecordType, record.Value, record.Ttl, fqdn);

                    var saved = await _domainRepository.GetRecordAsync(providerType, zoneName, record.ProviderRecordId);
                    if (saved is not null)
                    {
                        seenRecordIds.Add(saved.Id);
                    }
                }

                var localRecords = await _domainRepository.GetRecordsAsync(providerType, zoneName, DomainAssetStatus.Active);
                foreach (var local in localRecords.Where(r => !seenRecordIds.Contains(r.Id)))
                {
                    await _domainRepository.MarkRecordRemovedAsync(local.Id);
                    recordsRemoved++;
                }

                foreach (var fqdn in derivedFqdns)
                {
                    var existingDomain = await _domainRepository.GetByFqdnAsync(providerType, fqdn);
                    if (existingDomain is null)
                    {
                        domainsAdded++;
                    }

                    await _domainRepository.UpsertDomainAsync(providerType, fqdn, zoneName, DomainAssetSource.Synced);
                }

                var localDomains = await _domainRepository.GetDomainsAsync(providerType, DomainAssetStatus.Active);
                foreach (var local in localDomains.Where(d =>
                             string.Equals(d.ZoneName, zoneName, StringComparison.OrdinalIgnoreCase) &&
                             !derivedFqdns.Contains(d.Fqdn) &&
                             d.Source == DomainAssetSource.Synced))
                {
                    await _domainRepository.MarkDomainRemovedAsync(local.Id);
                    domainsRemoved++;
                }
            }

            foreach (var local in localZones.Where(z => !seenZoneIds.Contains(z.Id) && z.Status == DomainAssetStatus.Active))
            {
                await _domainRepository.MarkZoneRemovedAsync(local.Id);
                zonesRemoved++;

                var childDomains = await _domainRepository.GetDomainsAsync(providerType, DomainAssetStatus.Active);
                foreach (var child in childDomains.Where(d =>
                             string.Equals(d.ZoneName, local.ZoneName, StringComparison.OrdinalIgnoreCase) &&
                             d.Source == DomainAssetSource.Synced))
                {
                    await _domainRepository.MarkDomainRemovedAsync(child.Id);
                    domainsRemoved++;
                }

                var childRecords = await _domainRepository.GetRecordsAsync(providerType, local.ZoneName, DomainAssetStatus.Active);
                foreach (var record in childRecords)
                {
                    await _domainRepository.MarkRecordRemovedAsync(record.Id);
                    recordsRemoved++;
                }
            }

            var result = new DomainSyncResult(
                zonesAdded, zonesRemoved, recordsUpserted, recordsRemoved, domainsAdded, domainsRemoved);

            await _operationLogService.LogAsync(
                OperationLogAction.DomainSynced,
                OperationLogResult.Success,
                providerType,
                "DomainSync",
                null,
                null,
                $"同步完成: Zone +{zonesAdded}/-{zonesRemoved}, Record +{recordsUpserted}/-{recordsRemoved}, Domain +{domainsAdded}/-{domainsRemoved}",
                actor: actor);

            return result;
        }
        catch (Exception ex)
        {
            await _operationLogService.LogAsync(
                OperationLogAction.DomainSynced,
                OperationLogResult.Failed,
                providerType,
                "DomainSync",
                null,
                null,
                $"同步失败: {ex.Message}",
                actor: actor);
            throw;
        }
    }

    public async Task<Domain> AddManualDomainAsync(string fqdn, string? remark = null, string? actor = null)
    {
        if (string.IsNullOrWhiteSpace(fqdn))
        {
            throw new ArgumentException("FQDN 不能为空", nameof(fqdn));
        }

        var providerType = _providerClient.ProviderType;
        var normalized = fqdn.Trim().TrimEnd('.').ToLowerInvariant();
        var zoneNames = await _domainRepository.GetActiveZoneNamesAsync(providerType);
        var zone = DomainNameHelper.MatchZone(normalized, zoneNames);

        if (zone is null)
        {
            await _operationLogService.LogAsync(
                OperationLogAction.DomainAdded,
                OperationLogResult.Failed,
                providerType,
                "Domain",
                null,
                normalized,
                $"手动添加失败：不存在匹配的已购域名",
                actor: actor);
            throw new InvalidOperationException($"不存在匹配的已购域名（Zone）: {normalized}");
        }

        var domain = await _domainRepository.UpsertDomainAsync(providerType, normalized, zone, DomainAssetSource.Manual);
        if (!string.IsNullOrWhiteSpace(remark))
        {
            domain.Remark = remark.Trim();
        }

        await _operationLogService.LogAsync(
            OperationLogAction.DomainAdded,
            OperationLogResult.Success,
            providerType,
            "Domain",
            domain.Id.ToString(),
            domain.Fqdn,
            $"手动添加域名，归属 Zone={zone}",
            actor: actor);

        return domain;
    }
}

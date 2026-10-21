using MinGo.CertManager.Core.Entities;
using MinGo.CertManager.Core.Services;
using MinGo.CertManager.Infrastructure.Repositories;

namespace MinGo.CertManager.Application.Services;

/// <summary>
/// 解析记录直通写结果（本地镜像）。
/// </summary>
public record DnsRecordWriteResult(
    DomainRecord Record,
    string Message);

/// <summary>
/// 解析记录管理：创建 / 删除（无 Update）。写模型为直通写。
/// Provider 为真相源：API 成功后回写本地；失败不改本地。
/// </summary>
public interface IDnsRecordAdminService
{
    Task<DnsRecordWriteResult> CreateAsync(
        string zoneName, string rr, string recordType, string value, int ttl,
        string? actor = null, CancellationToken ct = default);

    Task<DnsRecordWriteResult> DeleteAsync(
        string zoneName, string providerRecordId,
        string? actor = null, CancellationToken ct = default);
}

public class DnsRecordAdminService : IDnsRecordAdminService
{
    private const string TargetType = "DomainRecord";

    private readonly IDomainProviderClient _providerClient;
    private readonly IDomainRepository _domainRepository;
    private readonly IOperationLogService _operationLogService;

    public DnsRecordAdminService(
        IDomainProviderClient providerClient,
        IDomainRepository domainRepository,
        IOperationLogService operationLogService)
    {
        _providerClient = providerClient;
        _domainRepository = domainRepository;
        _operationLogService = operationLogService;
    }

    public async Task<DnsRecordWriteResult> CreateAsync(
        string zoneName, string rr, string recordType, string value, int ttl,
        string? actor = null, CancellationToken ct = default)
    {
        var providerType = _providerClient.ProviderType;
        var zone = zoneName.Trim().TrimEnd('.').ToLowerInvariant();
        var hostRr = rr.Trim().TrimEnd('.');
        var type = recordType.Trim().ToUpperInvariant();
        var recordValue = value.Trim();
        var effectiveTtl = ttl <= 0 ? DnsRecordValidator.DefaultTtl : ttl;

        var validationError = DnsRecordValidator.ValidateCreate(hostRr, type, recordValue, effectiveTtl);
        if (validationError is not null)
        {
            throw new ArgumentException(validationError);
        }

        var targetName = $"{hostRr}.{zone}";
        try
        {
            // 直通写：先提供商，成功再回写本地
            var providerRecordId = await _providerClient.CreateRecordAsync(
                zone, hostRr, type, recordValue, effectiveTtl, ct);

            var fqdn = DomainNameHelper.ToFqdn(zone, hostRr);
            var record = await _domainRepository.UpsertRecordAsync(
                providerType, zone, providerRecordId,
                hostRr, type, recordValue, effectiveTtl, fqdn);

            // FQDN 台账对齐：新建/恢复 Source=Synced
            await _domainRepository.UpsertDomainAsync(providerType, fqdn, zone, DomainAssetSource.Synced);

            await _operationLogService.LogAsync(
                OperationLogAction.RecordCreated,
                OperationLogResult.Success,
                providerType,
                TargetType,
                providerRecordId,
                targetName,
                $"创建解析记录: {hostRr}.{zone} {type} {TruncateValue(recordValue)} TTL={effectiveTtl}",
                actor: actor);

            return new DnsRecordWriteResult(record, $"已创建 {type} 记录 {hostRr}.{zone}");
        }
        catch (Exception ex)
        {
            await _operationLogService.LogAsync(
                OperationLogAction.RecordCreated,
                OperationLogResult.Failed,
                providerType,
                TargetType,
                null,
                targetName,
                $"创建解析记录失败: {ex.Message}",
                actor: actor);
            throw;
        }
    }

    public async Task<DnsRecordWriteResult> DeleteAsync(
        string zoneName, string providerRecordId,
        string? actor = null, CancellationToken ct = default)
    {
        var providerType = _providerClient.ProviderType;
        var zone = zoneName.Trim().TrimEnd('.').ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(providerRecordId))
        {
            throw new ArgumentException("缺少提供商记录标识", nameof(providerRecordId));
        }

        var existing = await _domainRepository.GetRecordAsync(providerType, zone, providerRecordId)
            ?? throw new InvalidOperationException("本地不存在该解析记录，请先从提供商同步");

        var validationError = DnsRecordValidator.ValidateDelete(existing.Rr);
        if (validationError is not null)
        {
            throw new InvalidOperationException(validationError);
        }

        var targetName = $"{existing.Rr}.{zone}";
        try
        {
            // 直通写：先提供商删除
            await _providerClient.DeleteRecordAsync(providerRecordId, zone, ct);

            await _domainRepository.MarkRecordRemovedAsync(existing.Id);
            await AlignFqdnAfterDeleteAsync(providerType, zone, existing.Fqdn, ct);

            await _operationLogService.LogAsync(
                OperationLogAction.RecordRemoved,
                OperationLogResult.Success,
                providerType,
                TargetType,
                providerRecordId,
                targetName,
                $"删除解析记录: {targetName} {existing.RecordType}",
                actor: actor);

            var removed = await _domainRepository.GetRecordAsync(providerType, zone, providerRecordId);
            return new DnsRecordWriteResult(
                removed ?? existing,
                $"已删除记录 {targetName}");
        }
        catch (Exception ex)
        {
            await _operationLogService.LogAsync(
                OperationLogAction.RecordRemoved,
                OperationLogResult.Failed,
                providerType,
                TargetType,
                providerRecordId,
                targetName,
                $"删除解析记录失败: {ex.Message}",
                actor: actor);
            throw;
        }
    }

    /// <summary>
    /// 删除记录后对齐 FQDN：无其它 Active 记录且非 Manual 时软删。
    /// 与同步软删规则一致；Manual 源保留。
    /// </summary>
    private async Task AlignFqdnAfterDeleteAsync(
        DnsProviderType providerType, string zone, string fqdn, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(fqdn))
        {
            return;
        }

        var activeRecords = await _domainRepository.GetRecordsAsync(providerType, zone, DomainAssetStatus.Active);
        var stillHasRecord = activeRecords.Any(r =>
            string.Equals(r.Fqdn, fqdn, StringComparison.OrdinalIgnoreCase));

        if (stillHasRecord)
        {
            return;
        }

        var domain = await _domainRepository.GetByFqdnAsync(providerType, fqdn);
        if (domain is null || domain.Source != DomainAssetSource.Synced || domain.Status == DomainAssetStatus.Removed)
        {
            return;
        }

        await _domainRepository.MarkDomainRemovedAsync(domain.Id);
    }

    private static string TruncateValue(string value) =>
        value.Length <= 64 ? value : value[..64] + "…";
}

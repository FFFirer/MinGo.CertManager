using MinGo.CertManager.Core.Entities;

namespace MinGo.CertManager.Core.Services;

/// <summary>
/// 提供商侧已购域名
/// </summary>
public record ProviderZone(
    string ZoneName,
    string? ProviderZoneId = null);

/// <summary>
/// 提供商侧解析记录
/// </summary>
public record ProviderRecord(
    string ProviderRecordId,
    string Rr,
    string RecordType,
    string Value,
    int Ttl);

/// <summary>
/// DNS 提供商能力统一抽象。
/// 凭据由各实现从配置读取，不进入接口契约。
/// </summary>
public interface IDomainProviderClient
{
    /// <summary>提供商类型标识</summary>
    DnsProviderType ProviderType { get; }

    /// <summary>列出已购域名（Zone）</summary>
    Task<IReadOnlyList<ProviderZone>> ListZonesAsync(CancellationToken ct = default);

    /// <summary>列出指定 Zone 的解析记录</summary>
    Task<IReadOnlyList<ProviderRecord>> ListRecordsAsync(string zoneName, CancellationToken ct = default);

    /// <summary>
    /// 创建 ACME DNS-01 TXT 记录
    /// </summary>
    /// <returns>提供商记录标识</returns>
    Task<string> CreateTxtRecordAsync(string zoneName, string rr, string value, CancellationToken ct = default);

    /// <summary>
    /// 删除 ACME DNS-01 TXT 记录（按 RR+Value 精确匹配）
    /// </summary>
    Task DeleteTxtRecordAsync(string zoneName, string rr, string value, CancellationToken ct = default);

    /// <summary>
    /// 清理同名 TXT 残留记录（事前/事后清理）
    /// </summary>
    Task ClearTxtRecordsAsync(string zoneName, string rr, CancellationToken ct = default);

    /// <summary>
    /// 创建通用解析记录（A/AAAA/CNAME 等，类型由调用方约束）。
    /// 供域名解析管理直通写使用；不做 Update 契约。
    /// </summary>
    /// <returns>提供商记录标识</returns>
    Task<string> CreateRecordAsync(
        string zoneName, string rr, string recordType, string value, int ttl,
        CancellationToken ct = default);

    /// <summary>
    /// 按提供商记录标识删除解析记录。
    /// </summary>
    Task DeleteRecordAsync(string providerRecordId, string zoneName, CancellationToken ct = default);
}

using MinGo.CertManager.Core.Entities;

namespace MinGo.CertManager.Core.Services;

/// <summary>
/// 供应商实现状态：内置可用 / 规划中占位
/// </summary>
public enum DnsProviderImplementationStatus
{
    BuiltIn = 0,
    Planned = 1
}

/// <summary>
/// 凭据配置态：由 AccessKeyId 与 AccessKeySecret 均非空判定
/// </summary>
public enum ProviderConfigState
{
    Configured = 0,
    NotConfigured = 1
}

/// <summary>
/// 连接体检结果状态
/// </summary>
public enum ProviderHealthStatus
{
    Success = 0,
    AuthFailed = 1,
    ConnectionFailed = 2,
    NotConfigured = 3,
    NotSupported = 4
}

/// <summary>
/// 内置 DNS 供应商目录项（代码注册表，只读）
/// </summary>
public record DnsProviderCatalogItem(
    DnsProviderType ProviderType,
    string DisplayName,
    DnsProviderImplementationStatus ImplementationStatus,
    string ConfigSectionName,
    string ConfigDocHint,
    bool HasClient);

/// <summary>
/// 凭据配置态展示模型（脱敏，永不包含 Secret 明文）
/// </summary>
public record ProviderConfigView(
    DnsProviderType ProviderType,
    ProviderConfigState State,
    string MaskedAccessKeyId,
    bool HasAccessKeySecret,
    string RegionId,
    string Endpoint,
    string ConfigSectionName,
    string ConfigDocHint);

/// <summary>
/// 连接体检结果
/// </summary>
public record ProviderHealthResult(
    DnsProviderType ProviderType,
    ProviderHealthStatus Status,
    int? ZoneCount,
    string Message,
    DateTime CheckedAt);

/// <summary>
/// DNS 供应商只读目录
/// </summary>
public interface IDnsProviderCatalog
{
    IReadOnlyList<DnsProviderCatalogItem> GetItems();

    DnsProviderCatalogItem? GetItem(DnsProviderType providerType);
}

/// <summary>
/// DNS 供应商控制台：目录、配置态、连接体检
/// </summary>
public interface IDnsProviderConsoleService
{
    IReadOnlyList<DnsProviderCatalogItem> GetCatalog();

    ProviderConfigView GetConfigView(DnsProviderType providerType);

    Task<ProviderHealthResult> TestConnectionAsync(DnsProviderType providerType, CancellationToken ct = default);
}

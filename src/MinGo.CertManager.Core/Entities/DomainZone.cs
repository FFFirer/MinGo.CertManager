namespace MinGo.CertManager.Core.Entities;

/// <summary>
/// 提供商侧已购域名（DNS Zone），如 example.com
/// </summary>
public class DomainZone
{
    public Guid Id { get; set; }
    public DnsProviderType ProviderType { get; set; }
    /// <summary>Zone 域名，如 example.com</summary>
    public string ZoneName { get; set; } = string.Empty;
    public DomainAssetStatus Status { get; set; } = DomainAssetStatus.Active;
    /// <summary>提供商侧域名标识（阿里云 DomainName）</summary>
    public string? ProviderZoneId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? RemovedAt { get; set; }
}

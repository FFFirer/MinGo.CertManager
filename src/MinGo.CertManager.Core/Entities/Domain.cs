namespace MinGo.CertManager.Core.Entities;

/// <summary>
/// FQDN 域名管理单元，如 www.example.com、*.example.com
/// </summary>
public class Domain
{
    public Guid Id { get; set; }
    public DnsProviderType ProviderType { get; set; }
    /// <summary>完整 FQDN，如 www.example.com</summary>
    public string Fqdn { get; set; } = string.Empty;
    /// <summary>所属已购域名（Zone），如 example.com</summary>
    public string ZoneName { get; set; } = string.Empty;
    public DomainAssetSource Source { get; set; } = DomainAssetSource.Synced;
    public DomainAssetStatus Status { get; set; } = DomainAssetStatus.Active;
    /// <summary>备注</summary>
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? RemovedAt { get; set; }
}

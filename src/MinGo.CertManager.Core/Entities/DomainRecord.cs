namespace MinGo.CertManager.Core.Entities;

/// <summary>
/// 提供商解析记录镜像
/// </summary>
public class DomainRecord
{
    public Guid Id { get; set; }
    public DnsProviderType ProviderType { get; set; }
    /// <summary>所属 Zone，如 example.com</summary>
    public string ZoneName { get; set; } = string.Empty;
    /// <summary>提供商记录标识（阿里云 RecordId）</summary>
    public string ProviderRecordId { get; set; } = string.Empty;
    /// <summary>主机记录 RR，如 www、@、*.api</summary>
    public string Rr { get; set; } = string.Empty;
    /// <summary>记录类型：A、CNAME、TXT、MX 等</summary>
    public string RecordType { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int Ttl { get; set; }
    /// <summary>由 RR+Zone 推导的 FQDN</summary>
    public string Fqdn { get; set; } = string.Empty;
    public DomainAssetStatus Status { get; set; } = DomainAssetStatus.Active;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? RemovedAt { get; set; }
}

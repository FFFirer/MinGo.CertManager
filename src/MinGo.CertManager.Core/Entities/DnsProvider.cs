using System;

namespace MinGo.CertManager.Core.Entities;

public enum DnsProviderType
{
    Aliyun = 0
}

public class DnsProvider
{
    public Guid Id { get; set; }
    public DnsProviderType ProviderType { get; set; }
    public string AccessKeyId { get; set; } = string.Empty;
    public string AccessKeySecret { get; set; } = string.Empty;
    public string RegionId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

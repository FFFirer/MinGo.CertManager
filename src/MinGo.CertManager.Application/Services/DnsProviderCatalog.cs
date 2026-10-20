using MinGo.CertManager.Core.Entities;
using MinGo.CertManager.Core.Services;

namespace MinGo.CertManager.Application.Services;

/// <summary>
/// 内置 DNS 供应商目录（代码注册表）。不依赖数据库。
/// </summary>
public class DnsProviderCatalog : IDnsProviderCatalog
{
    private static readonly IReadOnlyList<DnsProviderCatalogItem> Items =
    [
        new(
            DnsProviderType.Aliyun,
            "阿里云 DNS",
            DnsProviderImplementationStatus.BuiltIn,
            "AliyunDns",
            """
            # 用户密钥（开发推荐）
            dotnet user-secrets set "AliyunDns:AccessKeyId" "<your-access-key-id>"
            dotnet user-secrets set "AliyunDns:AccessKeySecret" "<your-access-key-secret>"

            # 或环境变量
            # Linux/macOS
            export AliyunDns__AccessKeyId="<your-access-key-id>"
            export AliyunDns__AccessKeySecret="<your-access-key-secret>"
            # Windows PowerShell
            $env:AliyunDns__AccessKeyId="<your-access-key-id>"
            $env:AliyunDns__AccessKeySecret="<your-access-key-secret>"
            """,
            HasClient: true),
        new(
            DnsProviderType.Cloudflare,
            "Cloudflare",
            DnsProviderImplementationStatus.Planned,
            string.Empty,
            "暂未内置实现，敬请期待。",
            HasClient: false),
        new(
            DnsProviderType.Dnspod,
            "DNSPod",
            DnsProviderImplementationStatus.Planned,
            string.Empty,
            "暂未内置实现，敬请期待。",
            HasClient: false)
    ];

    public IReadOnlyList<DnsProviderCatalogItem> GetItems() => Items;

    public DnsProviderCatalogItem? GetItem(DnsProviderType providerType) =>
        Items.FirstOrDefault(i => i.ProviderType == providerType);
}

using AlibabaCloud.SDK.Alidns20150109;
using Mapster;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MinGo.CertManager.Core.Entities;
using MinGo.CertManager.Core.Services;
using MinGo.CertManager.Infrastructure.Configuration;

namespace MinGo.CertManager.Infrastructure.Services;

/// <summary>
/// 阿里云 DNS 提供商客户端（partial：ctor / Zones / Records / AcmeTxt）。
/// 凭据来自 appsettings.AliyunDns（沿用现有配置方案）。
/// 参考: https://help.aliyun.com/zh/dns/developer-reference/api-alidns-2015-01-09
/// </summary>
public partial class AliyunDomainProviderClient : IDomainProviderClient
{
    private readonly Client _client;
    private readonly ILogger<AliyunDomainProviderClient> _logger;

    public AliyunDomainProviderClient(
        ILogger<AliyunDomainProviderClient> logger,
        IOptions<AliyunDnsSettings> settings)
    {
        _logger = logger;
        var dnsSettings = settings.Value;
        var config = dnsSettings.Adapt<AlibabaCloud.OpenApiClient.Models.Config>();
        // SDK 会把 Protocol + "://" + Endpoint 拼成请求地址；Endpoint 必须是主机名。
        // 兼容 "https://alidns.aliyuncs.com/" 这类带协议写法，避免主机名被解析成 "https"。
        config.Endpoint = NormalizeEndpoint(dnsSettings.Endpoint);
        if (string.IsNullOrWhiteSpace(config.Protocol))
        {
            config.Protocol = "https";
        }

        _client = new Client(config);
    }

    /// <summary>
    /// 将 Endpoint 归一化为主机名（去掉协议与路径）。
    /// </summary>
    public static string NormalizeEndpoint(string? endpoint)
    {
        const string defaultHost = "alidns.aliyuncs.com";
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return defaultHost;
        }

        var value = endpoint.Trim();
        if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            value = value["http://".Length..];
        }
        else if (value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            value = value["https://".Length..];
        }

        // 去掉可能的 path / query，只保留 host[:port]
        var slash = value.IndexOfAny(['/', '?', '#']);
        if (slash >= 0)
        {
            value = value[..slash];
        }

        return string.IsNullOrWhiteSpace(value) ? defaultHost : value;
    }

    public DnsProviderType ProviderType => DnsProviderType.Aliyun;

    private async Task DeleteByRecordIdAsync(string? recordId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(recordId))
        {
            return;
        }

        var response = await _client.DeleteDomainRecordAsync(new AlibabaCloud.SDK.Alidns20150109.Models.DeleteDomainRecordRequest
        {
            RecordId = recordId
        });

        if (response.StatusCode != 200)
        {
            throw new InvalidOperationException($"阿里云DNS记录删除失败: RecordId={recordId}, StatusCode={response.StatusCode}");
        }

        _logger.LogInformation("阿里云DNS记录删除成功: RecordId={RecordId}", recordId);
    }
}

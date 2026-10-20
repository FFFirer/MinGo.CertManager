using AlibabaCloud.SDK.Alidns20150109;
using AlibabaCloud.SDK.Alidns20150109.Models;
using Mapster;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MinGo.CertManager.Core.Entities;
using MinGo.CertManager.Core.Services;
using MinGo.CertManager.Infrastructure.Configuration;

namespace MinGo.CertManager.Infrastructure.Services;

/// <summary>
/// 阿里云 DNS 提供商客户端。
/// 凭据来自 appsettings.AliyunDns（沿用现有配置方案）。
/// 参考: https://help.aliyun.com/zh/dns/developer-reference/api-alidns-2015-01-09
/// </summary>
public class AliyunDomainProviderClient : IDomainProviderClient
{
    private readonly Client _client;
    private readonly ILogger<AliyunDomainProviderClient> _logger;

    public AliyunDomainProviderClient(
        ILogger<AliyunDomainProviderClient> logger,
        IOptions<AliyunDnsSettings> settings)
    {
        _logger = logger;
        var config = settings.Value.Adapt<AlibabaCloud.OpenApiClient.Models.Config>();
        _client = new Client(config);
    }

    public DnsProviderType ProviderType => DnsProviderType.Aliyun;

    public async Task<IReadOnlyList<ProviderZone>> ListZonesAsync(CancellationToken ct = default)
    {
        var zones = new List<ProviderZone>();
        var pageNumber = 1L;
        const long pageSize = 100;

        while (true)
        {
            var request = new DescribeDomainsRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var response = await _client.DescribeDomainsAsync(request);
            var list = response.Body?.Domains?.Domain;
            if (list is null || list.Count == 0)
            {
                break;
            }

            foreach (var domain in list)
            {
                if (!string.IsNullOrWhiteSpace(domain.DomainName))
                {
                    zones.Add(new ProviderZone(domain.DomainName, domain.DomainName));
                }
            }

            var total = response.Body?.TotalCount ?? 0;
            if (pageNumber * pageSize >= total || list.Count < pageSize)
            {
                break;
            }

            pageNumber++;
        }

        _logger.LogInformation("阿里云已购域名列表: Count={Count}", zones.Count);
        return zones;
    }

    public async Task<IReadOnlyList<ProviderRecord>> ListRecordsAsync(string zoneName, CancellationToken ct = default)
    {
        var records = new List<ProviderRecord>();
        var pageNumber = 1L;
        const long pageSize = 100;

        while (true)
        {
            var request = new DescribeDomainRecordsRequest
            {
                DomainName = zoneName,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var response = await _client.DescribeDomainRecordsAsync(request);
            var list = response.Body?.DomainRecords?.Record;
            if (list is null || list.Count == 0)
            {
                break;
            }

            foreach (var record in list)
            {
                if (string.IsNullOrWhiteSpace(record.RecordId))
                {
                    continue;
                }

                records.Add(new ProviderRecord(
                    record.RecordId,
                    record.RR ?? string.Empty,
                    record.Type ?? string.Empty,
                    record.Value ?? string.Empty,
                    (int)(record.TTL ?? 600)));
            }

            var total = response.Body?.TotalCount ?? 0;
            if (pageNumber * pageSize >= total || list.Count < pageSize)
            {
                break;
            }

            pageNumber++;
        }

        _logger.LogInformation("阿里云解析记录列表: Zone={Zone}, Count={Count}", zoneName, records.Count);
        return records;
    }

    public async Task<string> CreateTxtRecordAsync(string zoneName, string rr, string value, CancellationToken ct = default)
    {
        _logger.LogInformation("创建阿里云DNS TXT记录: Zone={Zone}, RR={RR}", zoneName, rr);

        var request = new AddDomainRecordRequest
        {
            DomainName = zoneName,
            RR = rr,
            Type = "TXT",
            Value = value,
            TTL = 600
        };

        var response = await _client.AddDomainRecordAsync(request);
        if (response.StatusCode != 200 || string.IsNullOrWhiteSpace(response.Body?.RecordId))
        {
            throw new InvalidOperationException(
                $"阿里云DNS TXT记录创建失败: Zone={zoneName}, RR={rr}, StatusCode={response.StatusCode}");
        }

        _logger.LogInformation("阿里云DNS TXT记录创建成功: Zone={Zone}, RR={RR}, RecordId={RecordId}",
            zoneName, rr, response.Body.RecordId);
        return response.Body.RecordId;
    }

    public async Task DeleteTxtRecordAsync(string zoneName, string rr, string value, CancellationToken ct = default)
    {
        var records = await ListRecordsAsync(zoneName, ct);
        var match = records.FirstOrDefault(r =>
            string.Equals(r.Rr, rr, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(r.RecordType, "TXT", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(r.Value, value, StringComparison.Ordinal));

        if (match is null)
        {
            _logger.LogWarning("未找到待删除DNS TXT记录: Zone={Zone}, RR={RR}", zoneName, rr);
            return;
        }

        await DeleteByRecordIdAsync(match.ProviderRecordId, ct);
    }

    /// <summary>
    /// 清理同名 TXT 残留（精确匹配 RR，避免模糊匹配误删）。
    /// 用于 ACME DNS-01 事前/事后清理。
    /// </summary>
    public async Task ClearTxtRecordsAsync(string zoneName, string rr, CancellationToken ct = default)
    {
        var request = new DescribeDomainRecordsRequest
        {
            DomainName = zoneName,
            Type = "TXT",
            RRKeyWord = rr
        };

        var response = await _client.DescribeDomainRecordsAsync(request);
        var list = response.Body?.DomainRecords?.Record;
        if (list is null)
        {
            return;
        }

        foreach (var record in list.Where(r => r.RR == rr).ToList())
        {
            _logger.LogInformation("清理DNS-01 TXT记录: Zone={Zone}, RR={RR}, RecordId={RecordId}",
                zoneName, rr, record.RecordId);
            await DeleteByRecordIdAsync(record.RecordId, ct);
        }
    }

    private async Task DeleteByRecordIdAsync(string? recordId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(recordId))
        {
            return;
        }

        var response = await _client.DeleteDomainRecordAsync(new DeleteDomainRecordRequest
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

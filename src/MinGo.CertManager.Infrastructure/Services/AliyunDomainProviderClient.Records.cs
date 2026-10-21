using AlibabaCloud.SDK.Alidns20150109.Models;
using Microsoft.Extensions.Logging;
using MinGo.CertManager.Core.Services;

namespace MinGo.CertManager.Infrastructure.Services;

/// <summary>
/// 阿里云解析记录列表与通用创建/删除（A/AAAA/CNAME 等由调用方约束类型）。
/// 不提供 Update 契约。
/// </summary>
public partial class AliyunDomainProviderClient
{
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

    public async Task<string> CreateRecordAsync(
        string zoneName, string rr, string recordType, string value, int ttl,
        CancellationToken ct = default)
    {
        var type = recordType.Trim().ToUpperInvariant();
        _logger.LogInformation("创建阿里云DNS记录: Zone={Zone}, RR={RR}, Type={Type}", zoneName, rr, type);

        var request = new AddDomainRecordRequest
        {
            DomainName = zoneName,
            RR = rr,
            Type = type,
            Value = value,
            TTL = ttl
        };

        var response = await _client.AddDomainRecordAsync(request);
        if (response.StatusCode != 200 || string.IsNullOrWhiteSpace(response.Body?.RecordId))
        {
            throw new InvalidOperationException(
                $"阿里云DNS记录创建失败: Zone={zoneName}, RR={rr}, Type={type}, StatusCode={response.StatusCode}");
        }

        _logger.LogInformation("阿里云DNS记录创建成功: Zone={Zone}, RR={RR}, Type={Type}, RecordId={RecordId}",
            zoneName, rr, type, response.Body.RecordId);
        return response.Body.RecordId;
    }

    public Task DeleteRecordAsync(string providerRecordId, string zoneName, CancellationToken ct = default)
    {
        _logger.LogInformation("删除阿里云DNS记录: Zone={Zone}, RecordId={RecordId}", zoneName, providerRecordId);
        return DeleteByRecordIdAsync(providerRecordId, ct);
    }
}

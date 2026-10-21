using AlibabaCloud.SDK.Alidns20150109.Models;
using Microsoft.Extensions.Logging;
using MinGo.CertManager.Core.Services;

namespace MinGo.CertManager.Infrastructure.Services;

/// <summary>ACME DNS-01 专用 TXT 生命周期（契约保持不变）。</summary>
public partial class AliyunDomainProviderClient
{
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
}

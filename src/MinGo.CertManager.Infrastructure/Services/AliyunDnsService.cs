using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MinGo.CertManager.Infrastructure.Configuration;
using AlibabaCloud.SDK.Alidns20150109;
using AlibabaCloud.SDK.Alidns20150109.Models;
using AlibabaCloud.OpenApiClient.Models;

namespace MinGo.CertManager.Infrastructure.Services;

/// <summary>
/// 阿里云 DNS 服务接口
/// 参考资料: https://help.aliyun.com/zh/dns/developer-reference/api-alidns-2015-01-09
/// </summary>
public interface IAliyunDnsService
{
    Task CreateTxtRecordAsync(string domain, string recordName, string value);
    Task DeleteTxtRecordAsync(string domain, string recordName, string value);
}

/// <summary>
/// 阿里云 DNS 服务实现类
/// 参考资料: https://help.aliyun.com/zh/dns/developer-reference/api-alidns-2015-01-09
/// 使用 AlibabaCloud.SDK.Alidns20150109 SDK
/// </summary>
public class AliyunDnsService : IAliyunDnsService
{
    private readonly AlibabaCloud.SDK.Alidns20150109.Client _client;
    private readonly ILogger<AliyunDnsService> _logger;
    private readonly AliyunDnsSettings _settings;

    public AliyunDnsService(
        ILogger<AliyunDnsService> logger,
        IOptions<AliyunDnsSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;

        var config = new Config
        {
            AccessKeyId = _settings.AccessKeyId,
            AccessKeySecret = _settings.AccessKeySecret,
            RegionId = _settings.RegionId,
            Endpoint = "alidns.cn-hangzhou.aliyuncs.com"
        };

        _client = new AlibabaCloud.SDK.Alidns20150109.Client(config);
    }

    public async Task CreateTxtRecordAsync(string domain, string recordName, string value)
    {
        _logger.LogInformation("创建阿里云DNS TXT记录: Domain={Domain}, Record={Record}, Value={Value}", domain, recordName, value);

        try
        {
            var request = new AddDomainRecordRequest
            {
                DomainName = domain,
                RR = recordName,
                Type = "TXT",
                Value = value,
                TTL = 600
            };

            var response = await _client.AddDomainRecordAsync(request);

            if (response.StatusCode == 200)
            {
                _logger.LogInformation("阿里云DNS TXT记录创建成功: Domain={Domain}, Record={Record}, RecordId={RecordId}",
                    domain, recordName, response.Body?.RecordId);
            }
            else
            {
                _logger.LogError("阿里云DNS TXT记录创建失败: Domain={Domain}, Record={Record}, StatusCode={StatusCode}",
                    domain, recordName, response.StatusCode);
                throw new Exception($"阿里云DNS TXT记录创建失败: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "阿里云DNS TXT记录创建失败: Domain={Domain}, Record={Record}", domain, recordName);
            throw;
        }
    }

    public async Task DeleteTxtRecordAsync(string domain, string recordName, string value)
    {
        _logger.LogInformation("删除阿里云DNS TXT记录: Domain={Domain}, Record={Record}", domain, recordName);

        try
        {
            var recordId = await GetRecordIdAsync(domain, recordName, value);
            if (string.IsNullOrEmpty(recordId))
            {
                _logger.LogWarning("未找到DNS记录: Domain={Domain}, Record={Record}", domain, recordName);
                return;
            }

            var request = new DeleteDomainRecordRequest
            {
                RecordId = recordId
            };

            var response = await _client.DeleteDomainRecordAsync(request);

            if (response.StatusCode == 200)
            {
                _logger.LogInformation("阿里云DNS TXT记录删除成功: Domain={Domain}, Record={Record}, RecordId={RecordId}",
                    domain, recordName, recordId);
            }
            else
            {
                _logger.LogError("阿里云DNS TXT记录删除失败: Domain={Domain}, Record={Record}, StatusCode={StatusCode}",
                    domain, recordName, response.StatusCode);
                throw new Exception($"阿里云DNS TXT记录删除失败: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "阿里云DNS TXT记录删除失败: Domain={Domain}, Record={Record}", domain, recordName);
            throw;
        }
    }

    private async Task<string?> GetRecordIdAsync(string domain, string recordName, string value)
    {
        try
        {
            var request = new DescribeDomainRecordsRequest
            {
                DomainName = domain,
                RRKeyWord = recordName,
                Type = "TXT",
                ValueKeyWord = value
            };

            var response = await _client.DescribeDomainRecordsAsync(request);

            if (response.StatusCode == 200 && response.Body?.DomainRecords?.Record != null)
            {
                var record = response.Body.DomainRecords.Record
                    .FirstOrDefault(r => r.RR == recordName && r.Value == value);

                if (record != null)
                {
                    _logger.LogInformation("找到DNS记录: Domain={Domain}, Record={Record}, RecordId={RecordId}",
                        domain, recordName, record.RecordId);
                    return record.RecordId;
                }
            }

            _logger.LogWarning("未找到DNS记录: Domain={Domain}, Record={Record}, Value={Value}", domain, recordName, value);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询DNS记录失败: Domain={Domain}, Record={Record}", domain, recordName);
            return null;
        }
    }
}

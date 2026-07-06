using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MinGo.CertManager.Infrastructure.Configuration;
using MinGo.CertManager.Core.Services;
using AlibabaCloud.SDK.Alidns20150109;
using AlibabaCloud.SDK.Alidns20150109.Models;
using AlibabaCloud.OpenApiClient.Models;
using Mapster;

namespace MinGo.CertManager.Infrastructure.Services;

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

        var config = _settings.Adapt<AlibabaCloud.OpenApiClient.Models.Config>();

        _client = new AlibabaCloud.SDK.Alidns20150109.Client(config);
    }

    /// <summary>
    /// 清理指定域名的 ACME DNS-01 challenge TXT 记录。
    ///
    /// == 清理方案说明 ==
    /// DNS-01 challenge 在证书申请流程中需要在 DNS 添加 _acme-challenge.{sub} TXT 记录，
    /// 申请完成后需要清除。但可能因进程崩溃、网络中断等原因导致记录残留。
    /// 残留的 _acme-challenge 记录会导致后续的 DNS-01 challenge 验证失败（因为 Let's Encrypt
    /// 可能读到旧值）。整体清理策略如下：
    ///
    /// 1. 【事前清理】每次创建新的 challenge 记录前，先调用本方法清理同名的历史残留记录。
    ///    - 由 AcmeService.HandleDnsChallengeAsync() 在 CreateTxtRecordAsync() 之前调用。
    /// 2. 【事后清理】challenge 验证完成后（无论成功/失败），在 finally 块中再次调用本方法清理。
    ///    - 由 AcmeService.HandleDnsChallengeAsync() 的 finally 块调用。
    /// 3. 【异常兜底】RequestCertificateAsync 整体 catch 块中调用 CleanupAsync，触发清理。
    /// 4. 【进程崩溃恢复】若 ACME 进程在清理前崩溃，下次申请同域名证书时，#1 的事前清理会处理残留。
    ///
    /// 本方法只操作 _acme-challenge.{recordName} 的记录（通过 RRKeyWord 筛选 + 精确匹配），
    /// 不会误删用户的其他 TXT 记录。
    /// </summary>
    /// <param name="rootDomain">根域名（如 example.com）</param>
    /// <param name="recordName">TXT 记录名（如 _acme-challenge.www）</param>
    /// <param name="dnsTxt">TXT 记录值（仅用于日志）</param>
    public async Task ClearTxtRecordAsync(string rootDomain, string recordName, string dnsTxt)
    {
        try
        {
            var response = await _client.DescribeDomainRecordsAsync(new()
            {
                DomainName = rootDomain,
                Type = "TXT",
                RRKeyWord = recordName
            });

            if (response.Body?.DomainRecords?.Record != null)
            {
                // 只删除精确匹配 RR 的记录，防止 RRKeyWord 模糊匹配误删
                var matchingRecords = response.Body.DomainRecords.Record
                    .Where(r => r.RR == recordName)
                    .ToList();

                foreach (var record in matchingRecords)
                {
                    _logger.LogInformation("清理DNS-01 TXT记录: Domain={RootDomain}, Record={Record}, RecordId={RecordId}",
                        rootDomain, recordName, record.RecordId);

                    await _client.DeleteDomainRecordAsync(new()
                    {
                        RecordId = record.RecordId
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理DNS-01 TXT记录失败: {RR} of {RootDomain}", recordName, rootDomain);
        }
    }

    public async Task CreateTxtRecordAsync(string rootDomain, string recordName, string dnsTxt)
    {
        _logger.LogInformation("创建阿里云DNS TXT记录: Domain={RootDomain}, Record={Record}, Value={Value}", rootDomain, recordName, dnsTxt);

        try
        {
            var request = new AddDomainRecordRequest
            {
                DomainName = rootDomain,
                RR = recordName,
                Type = "TXT",
                Value = dnsTxt,
                TTL = 600
            };

            var response = await _client.AddDomainRecordAsync(request);

            if (response.StatusCode == 200)
            {
                _logger.LogInformation("阿里云DNS TXT记录创建成功: Domain={RootDomain}, Record={Record}, RecordId={RecordId}",
                    rootDomain, recordName, response.Body?.RecordId);
            }
            else
            {
                _logger.LogError("阿里云DNS TXT记录创建失败: Domain={RootDomain}, Record={Record}, StatusCode={StatusCode}",
                    rootDomain, recordName, response.StatusCode);
                throw new Exception($"阿里云DNS TXT记录创建失败: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "阿里云DNS TXT记录创建失败: Domain={RootDomain}, Record={Record}", rootDomain, recordName);
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

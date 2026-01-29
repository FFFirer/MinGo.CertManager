using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MinGo.CertManager.Core.Constants;
using System.Globalization;

namespace MinGo.CertManager.Infrastructure.Services;

public interface IAliyunDnsService
{
    Task CreateTxtRecordAsync(string domain, string recordName, string value);
    Task DeleteTxtRecordAsync(string domain, string recordName, string value);
}

public class AliyunDnsService : IAliyunDnsService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly string _accessKeyId;
    private readonly string _accessKeySecret;
    private readonly string _regionId;

    public AliyunDnsService(
        HttpClient httpClient,
        ILogger logger,
        string accessKeyId,
        string accessKeySecret,
        string regionId = "cn-hangzhou")
    {
        _httpClient = httpClient;
        _logger = logger;
        _accessKeyId = accessKeyId;
        _accessKeySecret = accessKeySecret;
        _regionId = regionId;
    }

    public async Task CreateTxtRecordAsync(string domain, string recordName, string value)
    {
        _logger.LogInformation("创建阿里云DNS TXT记录: Domain={Domain}, Record={Record}, Value={Value}", domain, recordName, value);

        var parameters = new Dictionary<string, string>
        {
            { "Action", "AddDomainRecord" },
            { "DomainName", domain },
            { "RR", recordName },
            { "Type", "TXT" },
            { "Value", value },
            { "TTL", "600" }
        };

        try
        {
            var response = await SendRequestAsync(parameters);
            _logger.LogInformation("阿里云DNS TXT记录创建成功: Domain={Domain}, Record={Record}", domain, recordName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "阿里云DNS TXT记录创建失败: Domain={Domain}, Record={Record}", domain, recordName);
            throw;
        }
    }

    public async Task DeleteTxtRecordAsync(string domain, string recordName, string value)
    {
        _logger.LogInformation("删除阿里云DNS TXT记录: Domain={Domain}, Record={Record}, Value={Value}", domain, recordName, value);

        try
        {
            var recordId = await GetRecordIdAsync(domain, recordName, value);
            if (recordId == null)
            {
                _logger.LogWarning("未找到DNS记录: Domain={Domain}, Record={Record}", domain, recordName);
                return;
            }

            var parameters = new Dictionary<string, string>
            {
                { "Action", "DeleteDomainRecord" },
                { "RecordId", recordId }
            };

            await SendRequestAsync(parameters);
            _logger.LogInformation("阿里云DNS TXT记录删除成功: Domain={Domain}, Record={Record}", domain, recordName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "阿里云DNS TXT记录删除失败: Domain={Domain}, Record={Record}", domain, recordName);
            throw;
        }
    }

    private async Task<string?> GetRecordIdAsync(string domain, string recordName, string value)
    {
        var parameters = new Dictionary<string, string>
        {
            { "Action", "DescribeDomainRecords" },
            { "DomainName", domain },
            { "Type", "TXT" },
            { "RRKeyWord", recordName }
        };

        var response = await SendRequestAsync(parameters);
        var jsonDoc = JsonDocument.Parse(response);
        var records = jsonDoc.RootElement.GetProperty("DomainRecords").GetProperty("Record");

        foreach (var record in records.EnumerateArray())
        {
            if (record.GetProperty("Value").GetString() == value)
            {
                return record.GetProperty("RecordId").GetString();
            }
        }

        return null;
    }

    private async Task<string> SendRequestAsync(Dictionary<string, string> parameters)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        var nonce = Guid.NewGuid().ToString("N");

        parameters["Format"] = "JSON";
        parameters["Version"] = AliyunDnsConstants.ApiVersion;
        parameters["AccessKeyId"] = _accessKeyId;
        parameters["SignatureMethod"] = "HMAC-SHA1";
        parameters["SignatureVersion"] = "1.0";
        parameters["SignatureNonce"] = nonce;
        parameters["Timestamp"] = timestamp;

        var signature = GenerateSignature(parameters, "GET");

        var url = BuildUrl(parameters, signature);
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    private string GenerateSignature(Dictionary<string, string> parameters, string method)
    {
        var sortedParams = parameters.OrderBy(p => p.Key).ToDictionary(p => p.Key, p => p.Value);
        var canonicalizedQueryString = string.Join("&", sortedParams.Select(p => $"{PercentEncode(p.Key)}={PercentEncode(p.Value)}"));
        var stringToSign = $"{method.ToUpperInvariant()}&{PercentEncode("/")}&{PercentEncode(canonicalizedQueryString)}";

        var key = Encoding.UTF8.GetBytes(_accessKeySecret + "&");
        var hmac = new HMACSHA1(key);
        var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
        return Convert.ToBase64String(signatureBytes);
    }

    private string BuildUrl(Dictionary<string, string> parameters, string signature)
    {
        var sortedParams = parameters.OrderBy(p => p.Key).ToDictionary(p => p.Key, p => p.Value);
        sortedParams["Signature"] = signature;
        var queryString = string.Join("&", sortedParams.Select(p => $"{PercentEncode(p.Key)}={PercentEncode(p.Value)}"));
        return $"{AliyunDnsConstants.Endpoint}?{queryString}";
    }

    private string PercentEncode(string value)
    {
        return Uri.EscapeDataString(value)
            .Replace("+", "%20")
            .Replace("*", "%2A")
            .Replace("%7E", "~");
    }
}

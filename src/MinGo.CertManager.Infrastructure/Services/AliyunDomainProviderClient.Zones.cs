using AlibabaCloud.SDK.Alidns20150109.Models;
using Microsoft.Extensions.Logging;
using MinGo.CertManager.Core.Services;

namespace MinGo.CertManager.Infrastructure.Services;

/// <summary>阿里云已购域名（Zone）查询。</summary>
public partial class AliyunDomainProviderClient
{
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
}

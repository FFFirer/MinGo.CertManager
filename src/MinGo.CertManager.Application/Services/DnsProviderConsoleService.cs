using System.Net.Http;
using System.Net.Sockets;
using Microsoft.Extensions.Options;
using MinGo.CertManager.Core.Entities;
using MinGo.CertManager.Core.Services;
using MinGo.CertManager.Infrastructure.Configuration;

namespace MinGo.CertManager.Application.Services;

/// <summary>
/// DNS 供应商控制台服务：目录、配置态（脱敏）、只读连接体检。
/// 体检仅调用 ListZones，不写操作流水，不做 TXT 探测。
/// </summary>
public class DnsProviderConsoleService : IDnsProviderConsoleService
{
    private readonly IDnsProviderCatalog _catalog;
    private readonly IDomainProviderClient _providerClient;
    private readonly AliyunDnsSettings _aliyunSettings;

    public DnsProviderConsoleService(
        IDnsProviderCatalog catalog,
        IDomainProviderClient providerClient,
        IOptions<AliyunDnsSettings> aliyunSettings)
    {
        _catalog = catalog;
        _providerClient = providerClient;
        _aliyunSettings = aliyunSettings.Value;
    }

    public IReadOnlyList<DnsProviderCatalogItem> GetCatalog() => _catalog.GetItems();

    public ProviderConfigView GetConfigView(DnsProviderType providerType)
    {
        var item = _catalog.GetItem(providerType)
            ?? throw new ArgumentOutOfRangeException(nameof(providerType), providerType, "未知的 DNS 提供商");

        if (providerType != DnsProviderType.Aliyun)
        {
            // 当前仅阿里云可读配置；规划中供应商无凭据模型
            return new ProviderConfigView(
                providerType,
                ProviderConfigState.NotConfigured,
                string.Empty,
                false,
                string.Empty,
                string.Empty,
                item.ConfigSectionName,
                item.ConfigDocHint);
        }

        var accessKeyId = _aliyunSettings.AccessKeyId;
        var accessKeySecret = _aliyunSettings.AccessKeySecret;
        var configured = CredentialMaskHelper.IsProvided(accessKeyId) && CredentialMaskHelper.IsProvided(accessKeySecret);

        return new ProviderConfigView(
            providerType,
            configured ? ProviderConfigState.Configured : ProviderConfigState.NotConfigured,
            CredentialMaskHelper.MaskAccessKeyId(accessKeyId),
            CredentialMaskHelper.IsProvided(accessKeySecret),
            _aliyunSettings.RegionId ?? string.Empty,
            _aliyunSettings.Endpoint ?? string.Empty,
            item.ConfigSectionName,
            item.ConfigDocHint);
    }

    public async Task<ProviderHealthResult> TestConnectionAsync(DnsProviderType providerType, CancellationToken ct = default)
    {
        var item = _catalog.GetItem(providerType)
            ?? throw new ArgumentOutOfRangeException(nameof(providerType), providerType, "未知的 DNS 提供商");

        if (!item.HasClient || item.ImplementationStatus != DnsProviderImplementationStatus.BuiltIn)
        {
            return new ProviderHealthResult(
                providerType,
                ProviderHealthStatus.NotSupported,
                null,
                "该供应商暂未内置实现，无法执行连接体检。",
                DateTime.UtcNow);
        }

        var config = GetConfigView(providerType);
        if (config.State != ProviderConfigState.Configured)
        {
            return new ProviderHealthResult(
                providerType,
                ProviderHealthStatus.NotConfigured,
                null,
                "凭据未配置，请先完成 AccessKeyId / AccessKeySecret 配置。",
                DateTime.UtcNow);
        }

        try
        {
            // 只读冒烟：验证密钥有效性与网络连通
            var zones = await _providerClient.ListZonesAsync(ct);
            var count = zones.Count;
            return new ProviderHealthResult(
                providerType,
                ProviderHealthStatus.Success,
                count,
                $"连接正常 · 检出 {count} 个已购域名",
                DateTime.UtcNow);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            var status = ClassifyFailure(ex);
            var message = status == ProviderHealthStatus.AuthFailed
                ? "鉴权失败 · 请检查 AccessKeyId / AccessKeySecret"
                : $"连接失败 · {ex.Message}";

            return new ProviderHealthResult(providerType, status, null, message, DateTime.UtcNow);
        }
    }

    /// <summary>
    /// 将供应商异常映射为体检状态。优先网络类，其次鉴权类。
    /// </summary>
    public static ProviderHealthStatus ClassifyFailure(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is HttpRequestException
                or TaskCanceledException
                or TimeoutException
                or SocketException)
            {
                return ProviderHealthStatus.ConnectionFailed;
            }

            var message = current.Message ?? string.Empty;
            if (ContainsAny(message,
                    "InvalidAccessKeyId",
                    "SignatureDoesNotMatch",
                    "AccessDenied",
                    "Forbidden",
                    "Unauthorized",
                    "InvalidSecurityToken",
                    "401",
                    "403"))
            {
                return ProviderHealthStatus.AuthFailed;
            }
        }

        return ProviderHealthStatus.ConnectionFailed;
    }

    private static bool ContainsAny(string haystack, params string[] needles) =>
        needles.Any(n => haystack.Contains(n, StringComparison.OrdinalIgnoreCase));
}

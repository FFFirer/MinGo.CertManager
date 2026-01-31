using Certes;
using Certes.Acme;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MinGo.CertManager.Core.Constants;
using MinGo.CertManager.Infrastructure.Configuration;

namespace MinGo.CertManager.Infrastructure.Services;

/// <summary>
/// ACME 服务实现
/// 参考资料: https://github.com/fszlin/certes/blob/main/docs/APIv2.md
/// </summary>
public interface IAcmeService
{
    Task<CertificateResult> RequestCertificateAsync(string domain, bool isWildcard, IAliyunDnsService dnsService, bool useStaging = false);
    Task CleanupAsync(string domain, bool isWildcard, IAliyunDnsService dnsService);
}

public class CertificateResult
{
    public string CertificatePem { get; set; } = string.Empty;
    public string PrivateKeyPem { get; set; } = string.Empty;
    public string CertificateChainPem { get; set; } = string.Empty;
}

public enum AcmeEnvironment
{
    Production,
    Staging
}

/// <summary>
/// ACME 服务实现类
/// 参考资料: https://github.com/fszlin/certes/blob/main/docs/APIv2.md
/// </summary>
public class AcmeService : IAcmeService
{
    private readonly ILogger<AcmeService> _logger;
    private readonly AcmeSettings _acmeSettings;
    private readonly IAcmeAccountCache _accountCache;

    private AcmeContext? _acmeContext;
    private IKey? _accountKey;
    private readonly Dictionary<string, IOrderContext> _orders = new();
    private readonly Dictionary<string, IAuthorizationContext> _authorizations = new();

    public AcmeService(ILogger<AcmeService> logger, IOptions<AcmeSettings> acmeSettings, IAcmeAccountCache accountCache)
    {
        _logger = logger;
        _acmeSettings = acmeSettings.Value;
        _accountCache = accountCache;
    }

    public async Task<CertificateResult> RequestCertificateAsync(string domain, bool isWildcard, IAliyunDnsService dnsService, bool useStaging = false)
    {
        _logger.LogInformation("开始ACME证书申请: Domain={Domain}, IsWildcard={IsWildcard}, Environment={Environment}",
            domain, isWildcard, useStaging ? "Staging" : "Production");

        try
        {
            var acmeUri = useStaging ? new Uri(_acmeSettings.LetsEncryptStagingUrl) : new Uri(_acmeSettings.LetsEncryptProductionUrl);
            _logger.LogInformation("连接到ACME服务器: {AcmeUri}", acmeUri);

            _acmeContext = new AcmeContext(acmeUri);

            var accountEmail = string.IsNullOrEmpty(_acmeSettings.AccountEmail) ? AcmeConstants.DefaultAccountEmail : _acmeSettings.AccountEmail;
            var contact = $"mailto:{accountEmail}";

            var cachedAccount = await _accountCache.GetCachedAccountAsync(acmeUri.ToString(), contact);

            if (cachedAccount != null)
            {
                _logger.LogInformation("使用缓存的ACME账号: AccountId={AccountId}", cachedAccount.AccountId);

                _accountKey = KeyFactory.FromDer(Convert.FromBase64String(cachedAccount.AccountKey));
                var account = await _acmeContext.NewAccount(new[] { contact }, true);

                await _accountCache.UpdateLastUsedAsync(cachedAccount.Id);
            }
            else
            {
                _logger.LogInformation("创建新的ACME账户");

                _accountKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
                var account = await _acmeContext.NewAccount(new[] { contact }, true);

                _logger.LogInformation("账户创建成功: AccountId={AccountId}", account.Location);

                var newAccount = new Core.Entities.AcmeAccount
                {
                    AccountId = account.Location.ToString(),
                    AccountKey = Convert.ToBase64String(_accountKey.ToDer()),
                    Contact = contact,
                    AcmeServerUrl = acmeUri.ToString(),
                    IsStaging = useStaging
                };

                await _accountCache.CacheAccountAsync(newAccount);
            }

            var domains = new List<string> { domain };
            if (isWildcard)
            {
                domains.Add($"*.{domain}");
            }

            _logger.LogInformation("创建订单: Domains={Domains}", string.Join(", ", domains));
            var order = await _acmeContext.NewOrder(domains);
            _orders[domain] = order;

            _logger.LogInformation("订单创建成功: OrderId={OrderId}", order.Location);

            var authorizations = await order.Authorizations();
            _logger.LogInformation("获取授权列表: Count={Count}", authorizations.Count());

            foreach (var authorization in authorizations)
            {
                var authDomain = await GetAuthorizationDomain(authorization);
                _authorizations[authDomain] = authorization;
                _logger.LogInformation("处理授权: Domain={AuthDomain}", authDomain);

                await HandleDnsChallengeAsync(authorization, dnsService, authDomain);
            }

            _logger.LogInformation("等待授权验证完成");
            await Task.Delay(CertificateConstants.AuthorizationCheckDelayMilliseconds);

            _logger.LogInformation("准备生成证书密钥对");
            var certificateKey = KeyFactory.NewKey(KeyAlgorithm.RS256);

            _logger.LogInformation("完成订单并生成证书");
            var certificate = await order.Generate(new CsrInfo
            {
                CommonName = domain
            }, certificateKey);

            _logger.LogInformation("证书生成成功: Domain={Domain}", domain);

            var result = new CertificateResult
            {
                CertificatePem = certificate.Certificate.ToPem(),
                PrivateKeyPem = certificateKey.ToPem(),
                CertificateChainPem = certificate.Certificate.ToPem()
            };

            _logger.LogInformation("证书申请完成: Domain={Domain}", domain);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ACME证书申请失败: Domain={Domain}", domain);
            throw;
        }
    }

    private async Task<string> GetAuthorizationDomain(IAuthorizationContext authorization)
    {
        var authz = await authorization.Resource();
        return authz.Identifier.Value;
    }

    private async Task HandleDnsChallengeAsync(IAuthorizationContext authorization, IAliyunDnsService dnsService, string domain)
    {
        _logger.LogInformation("处理DNS01挑战: Domain={Domain}", domain);

        var dnsChallenge = await GetDnsChallenge(authorization);
        if (dnsChallenge == null)
        {
            _logger.LogWarning("无法获取DNS挑战: Domain={Domain}", domain);
            return;
        }

        var dnsTxt = GetDnsRecord(dnsChallenge);
        if (string.IsNullOrEmpty(dnsTxt))
        {
            _logger.LogWarning("无法获取DNS记录值: Domain={Domain}", domain);
            return;
        }

        var (rr, rootDomain) = SplitDomainName(domain);
        var recordName = $"_acme-challenge.{rr}";

        _logger.LogInformation("创建DNS TXT记录: Record={Record}", recordName);

        try
        {
            await dnsService.ClearTxtRecordAsync(rootDomain, recordName, dnsTxt);
            await dnsService.CreateTxtRecordAsync(rootDomain, recordName, dnsTxt);

            _logger.LogInformation("等待DNS传播: Delay={Delay}秒", CertificateConstants.DnsPropagationDelayMilliseconds / 1000);
            await Task.Delay(CertificateConstants.DnsPropagationDelayMilliseconds);

            _logger.LogInformation("验证DNS挑战: Domain={Domain}", domain);
            await ValidateChallenge(dnsChallenge);

            _logger.LogInformation("等待授权验证完成: Domain={Domain}", domain);
            await WaitForAuthorizationAsync(authorization, domain);

            _logger.LogInformation("DNS挑战验证成功: Domain={Domain}", domain);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DNS挑战验证失败: Domain={Domain}", domain);
            throw;
        }
    }

    private async Task WaitForAuthorizationAsync(IAuthorizationContext authorization, string domain)
    {
        for (int retry = 0; retry < CertificateConstants.AuthorizationCheckMaxRetries; retry++)
        {
            var status = await GetAuthorizationStatus(authorization);
            _logger.LogInformation("授权状态检查: Domain={Domain}, Status={Status}, Retry={Retry}/{MaxRetries}",
                domain, status, retry + 1, CertificateConstants.AuthorizationCheckMaxRetries);

            if (status?.Equals("valid", StringComparison.OrdinalIgnoreCase) == true)
            {
                _logger.LogInformation("授权验证成功: Domain={Domain}", domain);
                return;
            }

            if (status?.Equals("invalid", StringComparison.OrdinalIgnoreCase) == true)
            {
                var error = await GetAuthorizationError(authorization);
                _logger.LogError("授权验证失败: Domain={Domain}, Error={Error}", domain, error);
                throw new Exception($"授权验证失败: {error}");
            }

            if (status?.Equals("pending", StringComparison.OrdinalIgnoreCase) == true
                || status?.Equals("processing", StringComparison.OrdinalIgnoreCase) == true)
            {
                if (retry < CertificateConstants.AuthorizationCheckMaxRetries - 1)
                {
                    _logger.LogInformation("等待授权验证: Domain={Domain}, Delay={Delay}秒",
                        domain, CertificateConstants.AuthorizationCheckIntervalMilliseconds / 1000);
                    await Task.Delay(CertificateConstants.AuthorizationCheckIntervalMilliseconds);
                }
                else
                {
                    _logger.LogError("授权验证超时: Domain={Domain}", domain);
                    throw new TimeoutException($"授权验证超时，已重试 {CertificateConstants.AuthorizationCheckMaxRetries} 次");
                }
            }
            else
            {
                _logger.LogWarning("未知的授权状态: Domain={Domain}, Status={Status}", domain, status);
                await Task.Delay(CertificateConstants.AuthorizationCheckIntervalMilliseconds);
            }
        }
    }

    /// <summary>
    /// 获取授权状态
    /// 参考资料: https://github.com/fszlin/certes/blob/main/docs/APIv2.md#authorizations
    /// </summary>
    /// <param name="authorization">授权上下文</param>
    /// <returns>授权状态字符串（valid/invalid/pending/processing等）</returns>
    private async Task<string?> GetAuthorizationStatus(IAuthorizationContext authorization)
    {
        var resource = await authorization.Resource();
        return resource.Status?.ToString();
    }

    /// <summary>
    /// 获取授权错误信息
    /// 参考资料: https://github.com/fszlin/certes/blob/main/docs/APIv2.md#authorizations
    /// </summary>
    /// <param name="authorization">授权上下文</param>
    /// <returns>错误详情字符串</returns>
    private async Task<string> GetAuthorizationError(IAuthorizationContext authorization)
    {
        var resource = await authorization.Resource();
        var challenges = await authorization.Challenges();

        var challengeResources = resource.Challenges;
        if (challengeResources != null)
        {
            var errorChallenge = challengeResources.FirstOrDefault(c => c.Error != null);
            if (errorChallenge != null && errorChallenge.Error != null)
            {
                return errorChallenge.Error.Detail;
            }
        }

        return "未知错误";
    }

    /// <summary>
    /// 从授权中获取DNS01类型的挑战
    /// 参考资料: https://github.com/fszlin/certes/blob/main/docs/APIv2.md#challenges
    /// </summary>
    /// <param name="authorization">授权上下文</param>
    /// <returns>DNS01挑战上下文，如果未找到则返回null</returns>
    private async Task<IChallengeContext?> GetDnsChallenge(IAuthorizationContext authorization)
    {
        return await authorization.Dns();
    }

    /// <summary>
    /// 获取DNS记录值（用于DNS TXT记录）
    /// 参考资料: https://github.com/fszlin/certes/blob/main/docs/APIv2.md#challenges
    /// 使用 AccountKey.DnsTxt() 方法计算 DNS TXT 记录值
    /// </summary>
    /// <param name="challenge">挑战上下文</param>
    /// <returns>DNS记录值字符串</returns>
    private string GetDnsRecord(IChallengeContext challenge)
    {
        return _acmeContext?.AccountKey.DnsTxt(challenge.Token) ?? string.Empty;
        // return _accountKey?.DnsTxt(challenge.Token) ?? string.Empty;
    }

    /// <summary>
    /// 向ACME服务器验证挑战
    /// 参考资料: https://github.com/fszlin/certes/blob/main/docs/APIv2.md#challenges
    /// 使用 challenge.Validate() 方法通知 ACME 服务器验证挑战
    /// </summary>
    /// <param name="challenge">挑战上下文</param>
    private async Task ValidateChallenge(IChallengeContext challenge)
    {
        await challenge.Validate();
    }

    private static (string rr, string root) SplitDomainName(string domainName)
    {
        var spans = domainName.Split('.').AsSpan<string>();

        var index = spans.Length - 2;

        var rr = string.Join(".", spans[..index]!);
        var domain = string.Join(".", spans[index..]!);

        return (rr, domain);
    }

    public async Task CleanupAsync(string domain, bool isWildcard, IAliyunDnsService dnsService)
    {
        _logger.LogInformation("清理ACME资源: Domain={Domain}, IsWildcard={IsWildcard}", domain, isWildcard);

        try
        {
            var domains = new List<string> { domain };
            if (isWildcard)
            {
                domains.Add($"*.{domain}");
            }

            foreach (var authDomain in domains)
            {
                var (rr, rootDomain) = SplitDomainName(authDomain);
                var recordName = $"_acme-challenge.{rr}";
                string? dnsKey = null;

                // 尝试从已存储的授权中获取DNS密钥
                if (_authorizations.TryGetValue(authDomain, out var authorization))
                {
                    var dnsChallenge = await GetDnsChallenge(authorization);
                    if (dnsChallenge != null)
                    {
                        dnsKey = GetDnsRecord(dnsChallenge);
                    }
                }

                // 即使没有授权信息，也尝试删除DNS记录
                _logger.LogInformation("删除DNS TXT记录: Record={Record}", recordName);
                await dnsService.ClearTxtRecordAsync(rootDomain, recordName, dnsKey ?? string.Empty);
            }

            _authorizations.Clear();
            _logger.LogInformation("ACME资源清理完成: Domain={Domain}", domain);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ACME资源清理失败: Domain={Domain}", domain);
        }
    }
}

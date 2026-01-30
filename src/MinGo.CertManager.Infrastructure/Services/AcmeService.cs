using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Certes;
using Certes.Acme;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MinGo.CertManager.Core.Constants;
using MinGo.CertManager.Core.Entities;
using MinGo.CertManager.Infrastructure.Configuration;

namespace MinGo.CertManager.Infrastructure.Services;

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
                var authDomain = GetAuthorizationDomain(authorization);
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

    private string GetAuthorizationDomain(IAuthorizationContext authorization)
    {
        try
        {
            var identifierProp = authorization.GetType().GetProperty("Identifier");
            if (identifierProp != null)
            {
                var identifier = identifierProp.GetValue(authorization);
                if (identifier != null)
                {
                    var valueProp = identifier.GetType().GetProperty("Value");
                    if (valueProp != null)
                    {
                        return valueProp.GetValue(identifier)?.ToString() ?? "unknown";
                    }
                }
            }
        }
        catch
        {
        }
        return "unknown";
    }

    private async Task HandleDnsChallengeAsync(IAuthorizationContext authorization, IAliyunDnsService dnsService, string domain)
    {
        _logger.LogInformation("处理DNS01挑战: Domain={Domain}", domain);

        var dnsChallenge = GetDnsChallenge(authorization);
        if (dnsChallenge == null)
        {
            _logger.LogWarning("无法获取DNS挑战: Domain={Domain}", domain);
            return;
        }

        var dnsKey = GetDnsRecord(dnsChallenge);
        if (string.IsNullOrEmpty(dnsKey))
        {
            _logger.LogWarning("无法获取DNS记录值: Domain={Domain}", domain);
            return;
        }

        var recordName = $"_acme-challenge.{domain}";

        _logger.LogInformation("创建DNS TXT记录: Record={Record}", recordName);

        try
        {
            await dnsService.CreateTxtRecordAsync(domain, recordName, dnsKey);

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
            var status = GetAuthorizationStatus(authorization);
            _logger.LogInformation("授权状态检查: Domain={Domain}, Status={Status}, Retry={Retry}/{MaxRetries}",
                domain, status, retry + 1, CertificateConstants.AuthorizationCheckMaxRetries);

            if (status == "valid")
            {
                _logger.LogInformation("授权验证成功: Domain={Domain}", domain);
                return;
            }

            if (status == "invalid")
            {
                var error = GetAuthorizationError(authorization);
                _logger.LogError("授权验证失败: Domain={Domain}, Error={Error}", domain, error);
                throw new Exception($"授权验证失败: {error}");
            }

            if (status == "pending" || status == "processing")
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

    private string GetAuthorizationStatus(IAuthorizationContext authorization)
    {
        try
        {
            var method = authorization.GetType().GetMethod("Status");
            if (method != null)
            {
                return method.Invoke(authorization, null)?.ToString() ?? "unknown";
            }
        }
        catch
        {
        }
        return "unknown";
    }

    private string GetAuthorizationError(IAuthorizationContext authorization)
    {
        try
        {
            var challenges = authorization.GetType().GetProperty("Challenges");
            if (challenges != null)
            {
                var challengesValue = challenges.GetValue(authorization);
                if (challengesValue != null)
                {
                    var enumerator = challengesValue.GetType().GetMethod("GetEnumerator");
                    if (enumerator != null)
                    {
                        var enumeratorObj = enumerator.Invoke(challengesValue, null);
                        if (enumeratorObj != null)
                        {
                            var moveNext = enumeratorObj.GetType().GetMethod("MoveNext");
                            if (moveNext != null && (bool)moveNext.Invoke(enumeratorObj, null))
                            {
                                var current = enumeratorObj.GetType().GetProperty("Current");
                                if (current != null)
                                {
                                    var challenge = current.GetValue(enumeratorObj);
                                    if (challenge != null)
                                    {
                                        var error = challenge.GetType().GetProperty("Error");
                                        if (error != null)
                                        {
                                            var errorValue = error.GetValue(challenge);
                                            if (errorValue != null)
                                            {
                                                var detail = errorValue.GetType().GetProperty("Detail");
                                                if (detail != null)
                                                {
                                                    return detail.GetValue(errorValue)?.ToString() ?? "未知错误";
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        catch
        {
        }
        return "未知错误";
    }

    private IChallengeContext? GetDnsChallenge(IAuthorizationContext authorization)
    {
        try
        {
            var method = authorization.GetType().GetMethod("DnsChallenge");
            if (method != null)
            {
                return method.Invoke(authorization, null) as IChallengeContext;
            }
        }
        catch
        {
        }
        return null;
    }

    private string GetDnsRecord(IChallengeContext challenge)
    {
        try
        {
            var property = challenge.GetType().GetProperty("DnsRecord");
            if (property != null)
            {
                return property.GetValue(challenge)?.ToString() ?? string.Empty;
            }
        }
        catch
        {
        }
        return string.Empty;
    }

    private async Task ValidateChallenge(IChallengeContext challenge)
    {
        try
        {
            var method = challenge.GetType().GetMethod("Validate");
            if (method != null)
            {
                var task = method.Invoke(challenge, null) as Task;
                if (task != null)
                {
                    await task;
                }
            }
        }
        catch
        {
        }
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
                if (_authorizations.TryGetValue(authDomain, out var authorization))
                {
                    var recordName = $"_acme-challenge.{authDomain}";
                    var dnsChallenge = GetDnsChallenge(authorization);

                    if (dnsChallenge != null)
                    {
                        var dnsKey = GetDnsRecord(dnsChallenge);
                        if (!string.IsNullOrEmpty(dnsKey))
                        {
                            _logger.LogInformation("删除DNS TXT记录: Record={Record}", recordName);
                            await dnsService.DeleteTxtRecordAsync(authDomain, recordName, dnsKey);
                        }
                    }
                }
            }

            _logger.LogInformation("ACME资源清理完成: Domain={Domain}", domain);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ACME资源清理失败: Domain={Domain}", domain);
        }
    }
}

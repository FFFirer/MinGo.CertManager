using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Certes;
using Certes.Acme;
using Microsoft.Extensions.Logging;
using MinGo.CertManager.Core.Constants;
using MinGo.CertManager.Core.Entities;

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

    private static readonly Uri LetEncryptProductionUri = AcmeConstants.LetsEncryptProductionUri;
    private static readonly Uri LetEncryptStagingUri = AcmeConstants.LetsEncryptStagingUri;

    private AcmeContext? _acmeContext;
    private IKey? _accountKey;
    private readonly Dictionary<string, IOrderContext> _orders = new();
    private readonly Dictionary<string, IAuthorizationContext> _authorizations = new();

    public AcmeService(ILogger<AcmeService> logger)
    {
        _logger = logger;
    }

    public async Task<CertificateResult> RequestCertificateAsync(string domain, bool isWildcard, IAliyunDnsService dnsService, bool useStaging = false)
    {
        _logger.LogInformation("开始ACME证书申请: Domain={Domain}, IsWildcard={IsWildcard}, Environment={Environment}",
            domain, isWildcard, useStaging ? "Staging" : "Production");

        try
        {
            var acmeUri = useStaging ? LetEncryptStagingUri : LetEncryptProductionUri;
            _logger.LogInformation("连接到ACME服务器: {AcmeUri}", acmeUri);

            _acmeContext = new AcmeContext(acmeUri);
            _logger.LogInformation("创建新账户");

            _accountKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
            var account = await _acmeContext.NewAccount(new[] { AcmeConstants.DefaultAccountEmail }, true);

            _logger.LogInformation("账户创建成功: AccountId={AccountId}", account.Location);

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

            _logger.LogInformation("DNS挑战验证成功: Domain={Domain}", domain);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DNS挑战验证失败: Domain={Domain}", domain);
            throw;
        }
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

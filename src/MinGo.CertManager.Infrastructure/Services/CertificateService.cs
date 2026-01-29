using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MinGo.CertManager.Core.Constants;
using MinGo.CertManager.Core.Entities;
using MinGo.CertManager.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace MinGo.CertManager.Infrastructure.Services;

public interface ICertificateService
{
    Task<Certificate> RequestCertificateAsync(string domain, bool isWildcard, DnsProvider dnsProvider, bool useStaging = false);
    Task<Certificate> RenewCertificateAsync(Guid certificateId);
    Task<byte[]> ExportCertificateAsync(Guid certificateId, CertificateFormat format, string? password = null);
}

public class CertificateService : ICertificateService
{
    private readonly ICertificateRepository _certificateRepository;
    private readonly IAcmeService _acmeService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CertificateService> _logger;

    public CertificateService(
        ICertificateRepository certificateRepository,
        IAcmeService acmeService,
        IHttpClientFactory httpClientFactory,
        ILogger<CertificateService> logger)
    {
        _certificateRepository = certificateRepository;
        _acmeService = acmeService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<Certificate> RequestCertificateAsync(string domain, bool isWildcard, DnsProvider dnsProvider, bool useStaging = false)
    {
        _logger.LogInformation("开始申请证书: Domain={Domain}, IsWildcard={IsWildcard}, Environment={Environment}",
            domain, isWildcard, useStaging ? "Staging" : "Production");

        var certificate = new Certificate
        {
            Id = Guid.NewGuid(),
            Domain = domain,
            IsWildcard = isWildcard,
            Status = CertificateStatus.Pending,
            AcmeStatus = AcmeProcessStatus.Initializing,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _certificateRepository.AddAsync(certificate);

        try
        {
            await UpdateAcmeStatusAsync(certificate.Id, AcmeProcessStatus.CreatingAccount, "创建ACME账户");

            var httpClient = _httpClientFactory.CreateClient();
            var dnsService = new AliyunDnsService(
                httpClient,
                _logger,
                dnsProvider.AccessKeyId,
                dnsProvider.AccessKeySecret,
                dnsProvider.RegionId);

            await UpdateAcmeStatusAsync(certificate.Id, AcmeProcessStatus.CreatingOrder, "创建ACME订单");

            var certificateResult = await _acmeService.RequestCertificateAsync(domain, isWildcard, dnsService, useStaging);

            await UpdateAcmeStatusAsync(certificate.Id, AcmeProcessStatus.Completed, "证书申请完成");

            certificate.CertificateContent = certificateResult.CertificatePem;
            certificate.PrivateKey = certificateResult.PrivateKeyPem;
            certificate.CertificateChain = certificateResult.CertificateChainPem;
            certificate.IssuedAt = DateTime.UtcNow;
            certificate.ExpiresAt = DateTime.UtcNow.AddDays(CertificateConstants.CertificateValidityDays);
            certificate.Status = CertificateStatus.Active;
            certificate.UpdatedAt = DateTime.UtcNow;

            await _certificateRepository.UpdateAsync(certificate);

            _logger.LogInformation("证书申请成功: CertificateId={CertificateId}, ExpiresAt={ExpiresAt}", certificate.Id, certificate.ExpiresAt);
            return certificate;
        }
        catch (Exception ex)
        {
            await UpdateAcmeStatusAsync(certificate.Id, AcmeProcessStatus.Failed, $"证书申请失败: {ex.Message}");
            _logger.LogError(ex, "证书申请失败: CertificateId={CertificateId}, Domain={Domain}", certificate.Id, domain);
            certificate.Status = CertificateStatus.Failed;
            certificate.UpdatedAt = DateTime.UtcNow;
            await _certificateRepository.UpdateAsync(certificate);
            throw;
        }
    }

    public async Task<Certificate> RenewCertificateAsync(Guid certificateId)
    {
        _logger.LogInformation("开始续签证书: CertificateId={CertificateId}", certificateId);

        var existingCertificate = await _certificateRepository.GetByIdAsync(certificateId);
        if (existingCertificate == null)
        {
            _logger.LogWarning("证书不存在: CertificateId={CertificateId}", certificateId);
            throw new ArgumentException("Certificate not found", nameof(certificateId));
        }

        _logger.LogInformation("证书信息: Domain={Domain}, IsWildcard={IsWildcard}, Status={Status}",
            existingCertificate.Domain, existingCertificate.IsWildcard, existingCertificate.Status);

        var dnsProvider = await GetDefaultDnsProvider();
        return await RequestCertificateAsync(existingCertificate.Domain, existingCertificate.IsWildcard, dnsProvider, false);
    }

    public async Task<byte[]> ExportCertificateAsync(Guid certificateId, CertificateFormat format, string? password = null)
    {
        _logger.LogInformation("导出证书: CertificateId={CertificateId}, Format={Format}", certificateId, format);

        var certificate = await _certificateRepository.GetByIdAsync(certificateId);
        if (certificate == null)
        {
            _logger.LogWarning("证书不存在: CertificateId={CertificateId}", certificateId);
            throw new ArgumentException("Certificate not found", nameof(certificateId));
        }

        try
        {
            var result = format switch
            {
                CertificateFormat.Pfx => ExportToPfx(certificate.CertificateContent, certificate.PrivateKey, password),
                CertificateFormat.Pem => ExportToPem(certificate.CertificateContent, certificate.PrivateKey),
                CertificateFormat.Crt => ExportToCrt(certificate.CertificateContent),
                _ => throw new ArgumentException("Unsupported format", nameof(format))
            };

            _logger.LogInformation("证书导出成功: CertificateId={CertificateId}, Format={Format}", certificateId, format);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "证书导出失败: CertificateId={CertificateId}, Format={Format}", certificateId, format);
            throw;
        }
    }

    private async Task UpdateAcmeStatusAsync(Guid certificateId, AcmeProcessStatus status, string message)
    {
        _logger.LogInformation("更新ACME状态: CertificateId={CertificateId}, Status={Status}, Message={Message}",
            certificateId, status, message);

        var certificate = await _certificateRepository.GetByIdAsync(certificateId);
        if (certificate != null)
        {
            certificate.AcmeStatus = status;
            certificate.AcmeStatusMessage = message;
            certificate.UpdatedAt = DateTime.UtcNow;
            await _certificateRepository.UpdateAsync(certificate);
        }
    }

    private byte[] ExportToPfx(string certContent, string privateKey, string? password)
    {
        var certParser = new X509CertificateParser();
        var cert = certParser.ReadCertificate(Encoding.ASCII.GetBytes(certContent));

        var keyPairParser = new PemReader(new StringReader(privateKey));
        var keyPair = (AsymmetricCipherKeyPair)keyPairParser.ReadObject();

        var store = new Pkcs12StoreBuilder().Build();
        store.SetKeyEntry(
            "certificate",
            new AsymmetricKeyEntry(keyPair.Private),
            new[] { new X509CertificateEntry(cert) }
        );

        using var stream = new MemoryStream();
        store.Save(stream, password?.ToCharArray(), new SecureRandom());
        return stream.ToArray();
    }

    private byte[] ExportToPem(string certContent, string privateKey)
    {
        var pem = $"{certContent}\n{privateKey}";
        return Encoding.UTF8.GetBytes(pem);
    }

    private byte[] ExportToCrt(string certContent)
    {
        return Encoding.UTF8.GetBytes(certContent);
    }

    private async Task<DnsProvider> GetDefaultDnsProvider()
    {
        return await Task.FromResult(new DnsProvider
        {
            Id = Guid.NewGuid(),
            ProviderType = DnsProviderType.Aliyun,
            AccessKeyId = string.Empty,
            AccessKeySecret = string.Empty,
            RegionId = "cn-hangzhou",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MinGo.CertManager.Core.Entities;
using MinGo.CertManager.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;

namespace MinGo.CertManager.Infrastructure.Services;

public interface ICertificateService
{
    Task<Certificate> RequestCertificateAsync(string domain, bool isWildcard, DnsProvider dnsProvider);
    Task<Certificate> RenewCertificateAsync(Guid certificateId);
    Task<byte[]> ExportCertificateAsync(Guid certificateId, CertificateFormat format, string? password = null);
}

public class CertificateService : ICertificateService
{
    private readonly ICertificateRepository _certificateRepository;
    private readonly IDnsValidationService _dnsValidationService;
    private readonly ILogger<CertificateService> _logger;

    public CertificateService(
        ICertificateRepository certificateRepository,
        IDnsValidationService dnsValidationService,
        ILogger<CertificateService> logger)
    {
        _certificateRepository = certificateRepository;
        _dnsValidationService = dnsValidationService;
        _logger = logger;
    }

    public async Task<Certificate> RequestCertificateAsync(string domain, bool isWildcard, DnsProvider dnsProvider)
    {
        _logger.LogInformation("开始申请证书: Domain={Domain}, IsWildcard={IsWildcard}", domain, isWildcard);

        var certificate = new Certificate
        {
            Id = Guid.NewGuid(),
            Domain = domain,
            IsWildcard = isWildcard,
            Status = CertificateStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("生成RSA密钥对: CertificateId={CertificateId}", certificate.Id);
            var keyPair = GenerateKeyPair();

            var acmeChallenge = $"_acme-challenge.{domain}";
            var acmeValue = GenerateAcmeChallengeValue();

            _logger.LogInformation("创建DNS TXT记录: Domain={Domain}, Record={Record}, Value={Value}", domain, acmeChallenge, acmeValue);
            await _dnsValidationService.CreateTxtRecordAsync(domain, acmeChallenge, acmeValue);

            _logger.LogInformation("等待DNS传播: Delay=5秒");
            await Task.Delay(5000);

            _logger.LogInformation("模拟ACME证书颁发: CertificateId={CertificateId}", certificate.Id);
            var (certContent, certChain) = await SimulateAcmeCertificateIssuance(domain, isWildcard, keyPair);

            certificate.CertificateContent = certContent;
            certificate.PrivateKey = ExportPrivateKey(keyPair.Private);
            certificate.CertificateChain = certChain;
            certificate.IssuedAt = DateTime.UtcNow;
            certificate.ExpiresAt = DateTime.UtcNow.AddDays(90);
            certificate.Status = CertificateStatus.Active;
            certificate.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("证书申请成功: CertificateId={CertificateId}, ExpiresAt={ExpiresAt}", certificate.Id, certificate.ExpiresAt);
            await _certificateRepository.AddAsync(certificate);
            return certificate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "证书申请失败: CertificateId={CertificateId}, Domain={Domain}", certificate.Id, domain);
            certificate.Status = CertificateStatus.Failed;
            certificate.UpdatedAt = DateTime.UtcNow;
            await _certificateRepository.AddAsync(certificate);
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
        return await RequestCertificateAsync(existingCertificate.Domain, existingCertificate.IsWildcard, dnsProvider);
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

    private AsymmetricCipherKeyPair GenerateKeyPair()
    {
        var keyGenerator = new RsaKeyPairGenerator();
        var keyParams = new KeyGenerationParameters(new SecureRandom(), 2048);
        keyGenerator.Init(keyParams);
        return keyGenerator.GenerateKeyPair();
    }

    private string ExportPrivateKey(AsymmetricKeyParameter privateKey)
    {
        using var stringWriter = new StringWriter();
        var pemWriter = new PemWriter(stringWriter);
        pemWriter.WriteObject(privateKey);
        pemWriter.Writer.Flush();
        return stringWriter.ToString();
    }

    private string GenerateAcmeChallengeValue()
    {
        return Guid.NewGuid().ToString("N");
    }

    private async Task<(string certContent, string certChain)> SimulateAcmeCertificateIssuance(string domain, bool isWildcard, AsymmetricCipherKeyPair keyPair)
    {
        await Task.Delay(1000);

        var certGenerator = new X509V3CertificateGenerator();
        var certName = new X509Name($"CN={domain}");
        var serialNumber = new BigInteger(DateTime.UtcNow.Ticks.ToString());

        certGenerator.SetSerialNumber(serialNumber);
        certGenerator.SetSubjectDN(certName);
        certGenerator.SetIssuerDN(certName);
        certGenerator.SetNotBefore(DateTime.UtcNow.Date);
        certGenerator.SetNotAfter(DateTime.UtcNow.AddDays(90));
        certGenerator.SetPublicKey(keyPair.Public);

        certGenerator.AddExtension(
            X509Extensions.BasicConstraints.Id,
            true,
            new BasicConstraints(false)
        );

        certGenerator.AddExtension(
            X509Extensions.KeyUsage.Id,
            true,
            new KeyUsage(KeyUsage.DigitalSignature | KeyUsage.KeyEncipherment)
        );

        var serverAuth = new DerObjectIdentifier("1.3.6.1.5.5.7.3.1");
        certGenerator.AddExtension(
            X509Extensions.ExtendedKeyUsage.Id,
            false,
            new ExtendedKeyUsage(new[] { serverAuth })
        );

        var signatureFactory = new Asn1SignatureFactory("SHA256WithRSA", keyPair.Private);
        var certificate = certGenerator.Generate(signatureFactory);

        using var stringWriter = new StringWriter();
        var pemWriter = new PemWriter(stringWriter);
        pemWriter.WriteObject(certificate);
        pemWriter.Writer.Flush();

        return (stringWriter.ToString(), stringWriter.ToString());
    }

    private byte[] ExportToPfx(string certContent, string privateKey, string? password)
    {
        var store = new Pkcs12StoreBuilder().Build();

        var certParser = new X509CertificateParser();
        var cert = certParser.ReadCertificate(Encoding.ASCII.GetBytes(certContent));

        var keyPairParser = new PemReader(new StringReader(privateKey));
        var keyPair = (AsymmetricCipherKeyPair)keyPairParser.ReadObject();

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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MinGo.CertManager.Core.Entities;
using MinGo.CertManager.Infrastructure.Repositories;
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

    public CertificateService(
        ICertificateRepository certificateRepository,
        IDnsValidationService dnsValidationService)
    {
        _certificateRepository = certificateRepository;
        _dnsValidationService = dnsValidationService;
    }

    public async Task<Certificate> RequestCertificateAsync(string domain, bool isWildcard, DnsProvider dnsProvider)
    {
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
            var keyPair = GenerateKeyPair();
            
            var acmeChallenge = $"_acme-challenge.{domain}";
            var acmeValue = GenerateAcmeChallengeValue();
            
            await _dnsValidationService.CreateTxtRecordAsync(domain, acmeChallenge, acmeValue);
            await Task.Delay(5000);

            var (certContent, certChain) = await SimulateAcmeCertificateIssuance(domain, isWildcard, keyPair);

            certificate.CertificateContent = certContent;
            certificate.PrivateKey = ExportPrivateKey(keyPair.Private);
            certificate.CertificateChain = certChain;
            certificate.IssuedAt = DateTime.UtcNow;
            certificate.ExpiresAt = DateTime.UtcNow.AddDays(90);
            certificate.Status = CertificateStatus.Active;
            certificate.UpdatedAt = DateTime.UtcNow;

            await _certificateRepository.AddAsync(certificate);
            return certificate;
        }
        catch (Exception)
        {
            certificate.Status = CertificateStatus.Failed;
            certificate.UpdatedAt = DateTime.UtcNow;
            await _certificateRepository.AddAsync(certificate);
            throw;
        }
    }

    public async Task<Certificate> RenewCertificateAsync(Guid certificateId)
    {
        var existingCertificate = await _certificateRepository.GetByIdAsync(certificateId);
        if (existingCertificate == null)
        {
            throw new ArgumentException("Certificate not found", nameof(certificateId));
        }

        var dnsProvider = await GetDefaultDnsProvider();
        return await RequestCertificateAsync(existingCertificate.Domain, existingCertificate.IsWildcard, dnsProvider);
    }

    public async Task<byte[]> ExportCertificateAsync(Guid certificateId, CertificateFormat format, string? password = null)
    {
        var certificate = await _certificateRepository.GetByIdAsync(certificateId);
        if (certificate == null)
        {
            throw new ArgumentException("Certificate not found", nameof(certificateId));
        }

        return format switch
        {
            CertificateFormat.Pfx => ExportToPfx(certificate.CertificateContent, certificate.PrivateKey, password),
            CertificateFormat.Pem => ExportToPem(certificate.CertificateContent, certificate.PrivateKey),
            CertificateFormat.Crt => ExportToCrt(certificate.CertificateContent),
            _ => throw new ArgumentException("Unsupported format", nameof(format))
        };
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

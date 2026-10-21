using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MinGo.CertManager.Core.Entities;
using MinGo.CertManager.Infrastructure.Repositories;
using MinGo.CertManager.Core.Services;
using MinGo.CertManager.Infrastructure.Configuration;
using Quartz;

namespace MinGo.CertManager.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public class CertificateRenewalJob : IJob
{
    private readonly ICertificateRepository _certificateRepository;
    private readonly ICertificateService _certificateService;
    private readonly ILogger<CertificateRenewalJob> _logger;
    private readonly CertificateSettings _certificateSettings;

    public CertificateRenewalJob(
        ICertificateRepository certificateRepository,
        ICertificateService certificateService,
        ILogger<CertificateRenewalJob> logger,
        IOptions<CertificateSettings> certificateSettings)
    {
        _certificateRepository = certificateRepository;
        _certificateService = certificateService;
        _logger = logger;
        _certificateSettings = certificateSettings.Value;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Starting certificate renewal job");

        try
        {
            var expiringBefore = DateTime.UtcNow.AddDays(_certificateSettings.RenewalDaysBeforeExpiry);
            var expiringCertificates = await _certificateRepository.GetRenewalCandidatesAsync(expiringBefore);

            _logger.LogInformation("Found {Count} certificates to renew", expiringCertificates.Count);

            foreach (var certificate in expiringCertificates)
            {
                try
                {
                    _logger.LogInformation("Renewing certificate for domain: {Domain} (source={SourceId})",
                        certificate.Domain, certificate.Id);

                    // 续期创建新记录，失败不影响源证书，下轮 Job 自然重试
                    var renewedCertificate = await _certificateService.RenewCertificateAsync(certificate.Id);

                    _logger.LogInformation("Successfully renewed certificate for domain: {Domain} (new={NewId})",
                        renewedCertificate.Domain, renewedCertificate.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to renew certificate for domain: {Domain}", certificate.Domain);
                }
            }

            _logger.LogInformation("Certificate renewal job completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Certificate renewal job failed");
            throw;
        }
    }
}

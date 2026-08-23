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
            var certificates = await _certificateRepository.GetAllAsync();
            var expiringCertificates = certificates
                .Where(c => c.Status == CertificateStatus.Active)
                .Where(c => c.ExpiresAt <= DateTime.UtcNow.AddDays(_certificateSettings.RenewalDaysBeforeExpiry))
                .ToList();

            _logger.LogInformation($"Found {expiringCertificates.Count} certificates to renew");

            foreach (var certificate in expiringCertificates)
            {
                try
                {
                    _logger.LogInformation($"Renewing certificate for domain: {certificate.Domain}");

                    var renewedCertificate = await _certificateService.RenewCertificateAsync(certificate.Id);

                    _logger.LogInformation($"Successfully renewed certificate for domain: {renewedCertificate.Domain}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to renew certificate for domain: {certificate.Domain}");
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

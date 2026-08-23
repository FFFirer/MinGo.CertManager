using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MinGo.CertManager.Core.Entities;
using MinGo.CertManager.Infrastructure.Repositories;
using MinGo.CertManager.Core.Services;
using Quartz;

namespace MinGo.CertManager.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public class CertificateRequestJob : IJob
{
    private readonly ICertificateRepository _certificateRepository;
    private readonly ICertificateService _certificateService;
    private readonly ILogger<CertificateRequestJob> _logger;

    public CertificateRequestJob(
        ICertificateRepository certificateRepository,
        ICertificateService certificateService,
        ILogger<CertificateRequestJob> logger)
    {
        _certificateRepository = certificateRepository;
        _certificateService = certificateService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var certificateId = context.MergedJobDataMap.GetGuid("CertificateId");
        _logger.LogInformation("Starting certificate request job for CertificateId: {CertificateId}", certificateId);

        var certificate = await _certificateRepository.GetByIdAsync(certificateId);
        if (certificate == null)
        {
            _logger.LogError("Certificate not found: {CertificateId}", certificateId);
            return;
        }

        // 检查证书是否已经在申请中（防止重复申请）
        if (certificate.AcmeStatus != AcmeProcessStatus.Initializing)
        {
            _logger.LogWarning("Certificate is already being processed: {CertificateId}, Status: {Status}",
                certificateId, certificate.AcmeStatus);
            return;
        }

        // 设置超时时间为10分钟
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        var timeoutTask = Task.Delay(Timeout.Infinite, cts.Token);

        try
        {
            // 处理已存在的证书申请（更新现有记录，不创建新记录）
            var certificateRequestTask = _certificateService.ProcessCertificateAsync(certificateId);

            // 等待证书申请任务或超时
            var completedTask = await Task.WhenAny(certificateRequestTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                // 超时处理
                _logger.LogError("Certificate request job timed out for CertificateId: {CertificateId}", certificateId);

                // 更新证书状态为失败
                certificate.Status = CertificateStatus.Failed;
                certificate.AcmeStatus = AcmeProcessStatus.Failed;
                certificate.AcmeStatusMessage = "证书申请超时（超过10分钟）";
                certificate.UpdatedAt = DateTime.UtcNow;
                await _certificateRepository.UpdateAsync(certificate);

                return;
            }

            // 证书申请完成
            var renewedCertificate = await certificateRequestTask;
            _logger.LogInformation("Certificate request completed successfully for Domain: {Domain}", certificate.Domain);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Certificate request job failed for CertificateId: {CertificateId}", certificateId);

            // 更新证书状态为失败
            certificate.Status = CertificateStatus.Failed;
            certificate.AcmeStatus = AcmeProcessStatus.Failed;
            certificate.AcmeStatusMessage = $"证书申请失败: {ex.Message}";
            certificate.UpdatedAt = DateTime.UtcNow;
            await _certificateRepository.UpdateAsync(certificate);

            throw;
        }
    }
}
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MinGo.CertManager.Core.Entities;
using MinGo.CertManager.Core.Messaging;
using MinGo.CertManager.Core.Services;
using MinGo.CertManager.Infrastructure.Repositories;
using MinGo.Messaging;
using MinGo.Messaging.Subscriptions;

namespace MinGo.CertManager.Application.Consumers;

/// <summary>
/// 证书申请命令消费者
/// </summary>
[MessageConsumer(Contract = typeof(RequestCertificateCommand), Group = "certmanager", DeliveryMode = DeliveryMode.Competing)]
public class CertificateRequestConsumer : IConsumer<RequestCertificateCommand>
{
    private readonly ICertificateService _certificateService;
    private readonly ICertificateRepository _certificateRepository;
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<CertificateRequestConsumer> _logger;

    public CertificateRequestConsumer(
        ICertificateService certificateService,
        ICertificateRepository certificateRepository,
        IMessagePublisher publisher,
        ILogger<CertificateRequestConsumer> logger)
    {
        _certificateService = certificateService;
        _certificateRepository = certificateRepository;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task ConsumeAsync(ConsumeContext<RequestCertificateCommand> context, CancellationToken cancellationToken)
    {
        var command = context.Message;
        _logger.LogInformation("收到证书申请命令: CertificateId={CertificateId}", command.CertificateId);

        try
        {
            var certificate = await _certificateService.ProcessCertificateAsync(command.CertificateId);

            await _publisher.PublishAsync(new CertificateCompletedEvent
            {
                CertificateId = certificate.Id,
                Domain = certificate.Domain,
                ExpiresAt = certificate.ExpiresAt
            }, cancellationToken);

            _logger.LogInformation("证书申请完成: CertificateId={CertificateId}, Domain={Domain}",
                certificate.Id, certificate.Domain);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "证书申请失败: CertificateId={CertificateId}", command.CertificateId);

            var certificate = await _certificateRepository.GetByIdAsync(command.CertificateId);
            if (certificate != null)
            {
                await _publisher.PublishAsync(new CertificateFailedEvent
                {
                    CertificateId = certificate.Id,
                    Domain = certificate.Domain,
                    ErrorMessage = ex.Message
                }, cancellationToken);
            }
        }
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MinGo.CertManager.Core.Entities;
using MinGo.CertManager.Infrastructure.Jobs;
using MinGo.CertManager.Infrastructure.Repositories;
using Quartz;

namespace MinGo.CertManager.Infrastructure.Services;

/// <summary>
/// 证书申请/续期任务恢复。
/// Quartz 使用内存存储，进程重启后动态创建的 CertificateRequestJob 会丢失，
/// 留下 Status=Pending 的孤儿记录。本服务在启动时（以及周期性兜底）将其重新入队。
/// </summary>
public class CertificateWorkRecoveryService : BackgroundService
{
    /// <summary>周期性兜底扫描间隔</summary>
    private static readonly TimeSpan SweepInterval = TimeSpan.FromMinutes(5);

    /// <summary>周期性扫描的陈旧阈值：UpdatedAt 早于此时长视为可能已成孤儿</summary>
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromMinutes(15);

    /// <summary>启动后延迟，等待 Quartz 调度器就绪</summary>
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(3);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly ILogger<CertificateWorkRecoveryService> _logger;

    public CertificateWorkRecoveryService(
        IServiceScopeFactory scopeFactory,
        ISchedulerFactory schedulerFactory,
        ILogger<CertificateWorkRecoveryService> logger)
    {
        _scopeFactory = scopeFactory;
        _schedulerFactory = schedulerFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartupDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        // 启动恢复：进程刚起来，Quartz 内存作业已清空，所有 Pending 都是孤儿
        await RecoverAsync(staleBeforeUtc: null, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(SweepInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            // 周期兜底：只捞长时间未更新的 Pending，避免与在途作业并发
            await RecoverAsync(DateTime.UtcNow - StaleThreshold, stoppingToken);
        }
    }

    /// <summary>
    /// 扫描可恢复的 Pending 记录并重新入队。
    /// <paramref name="staleBeforeUtc"/> 为 null 时不限定陈旧度（启动恢复）；
    /// 非空时只处理 UpdatedAt 早于该时刻的记录（周期兜底）。
    /// </summary>
    public async Task RecoverAsync(DateTime? staleBeforeUtc, CancellationToken cancellationToken)
    {
        List<Certificate> pending;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<ICertificateRepository>();
            pending = await repository.GetRecoverablePendingAsync(staleBeforeUtc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load recoverable pending certificates");
            return;
        }

        if (pending.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Recovering {Count} pending certificate task(s)", pending.Count);

        foreach (var certificate in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await RecoverOneAsync(certificate, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to recover certificate {CertificateId} ({Domain})",
                    certificate.Id, certificate.Domain);
            }
        }
    }

    private async Task RecoverOneAsync(Certificate certificate, CancellationToken cancellationToken)
    {
        // ACME 订单状态在内存中，无法从中间阶段续跑；重置后整单重来
        using (var scope = _scopeFactory.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<ICertificateRepository>();
            var existing = await repository.GetByIdAsync(certificate.Id);
            if (existing == null || existing.Status != CertificateStatus.Pending)
            {
                return;
            }

            existing.AcmeStatus = AcmeProcessStatus.Initializing;
            existing.AcmeStatusMessage = "应用重启后恢复处理";
            existing.UpdatedAt = DateTime.UtcNow;
            await repository.UpdateAsync(existing);
        }

        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        var jobKey = new JobKey($"CertificateRequest-{certificate.Id}", "CertificateRequests");
        var triggerKey = new TriggerKey($"CertificateRequestTrigger-{certificate.Id}", "CertificateRequests");

        if (await scheduler.CheckExists(jobKey, cancellationToken))
        {
            _logger.LogDebug("Job already scheduled for {CertificateId}, skip", certificate.Id);
            return;
        }

        var jobDetail = JobBuilder.Create<CertificateRequestJob>()
            .WithIdentity(jobKey)
            .WithDescription($"恢复处理证书: {certificate.Domain}")
            .UsingJobData("CertificateId", certificate.Id)
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity(triggerKey)
            .WithDescription("证书任务恢复触发")
            .StartNow()
            .Build();

        await scheduler.ScheduleJob(jobDetail, trigger, cancellationToken);

        _logger.LogInformation("Re-queued certificate {CertificateId} ({Domain})",
            certificate.Id, certificate.Domain);
    }
}

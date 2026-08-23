using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MinGo.CertManager.Core.Entities;
using MinGo.CertManager.Infrastructure.Repositories;
using MinGo.CertManager.Core.Services;
using MinGo.CertManager.Infrastructure.Services;
using MinGo.CertManager.Infrastructure.Jobs;
using Quartz;

namespace MinGo.CertManager.Web.Controllers;

[ApiController]
[Route("api/external")]
public class ExternalApiController : ControllerBase
{
    private readonly ICertificateService _certificateService;
    private readonly IAcmeService _acmeService;
    private readonly IAliyunDnsService _aliyunDnsService;
    private readonly ICertificateRepository _certificateRepository;
    private readonly ISchedulerFactory _schedulerFactory;

    public ExternalApiController(
        ICertificateService certificateService,
        IAcmeService acmeService,
        IAliyunDnsService aliyunDnsService,
        ICertificateRepository certificateRepository,
        ISchedulerFactory schedulerFactory)
    {
        _certificateService = certificateService;
        _acmeService = acmeService;
        _aliyunDnsService = aliyunDnsService;
        _certificateRepository = certificateRepository;
        _schedulerFactory = schedulerFactory;
    }

    /// <summary>
    /// 申请新证书
    /// </summary>
    /// <param name="request">证书申请请求</param>
    /// <returns>申请结果</returns>
    [HttpPost("certificates")]
    public async Task<IActionResult> RequestCertificate([FromBody] CertificateRequest request)
    {
        try
        {
            // 检查同一域名是否已有证书正在申请中
            var hasPending = await _certificateRepository.HasPendingCertificateAsync(request.Domain, request.IsWildcard);
            if (hasPending)
            {
                return BadRequest(new {
                    Success = false,
                    Error = $"域名 {request.Domain} 已有证书正在申请中，请稍后再试"
                });
            }

            // 创建证书记录
            var certificate = new Certificate
            {
                Id = Guid.NewGuid(),
                Domain = request.Domain,
                IsWildcard = request.IsWildcard,
                UseStaging = request.UseStaging,
                Status = CertificateStatus.Pending,
                AcmeStatus = AcmeProcessStatus.Initializing,
                AcmeStatusMessage = "等待处理",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _certificateRepository.AddAsync(certificate);

            // 触发一次性作业
            var scheduler = await _schedulerFactory.GetScheduler();
            var jobDetail = JobBuilder.Create<CertificateRequestJob>()
                .WithIdentity($"CertificateRequest-{certificate.Id}", "CertificateRequests")
                .WithDescription($"申请证书: {certificate.Domain}")
                .UsingJobData("CertificateId", certificate.Id)
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"CertificateRequestTrigger-{certificate.Id}", "CertificateRequests")
                .StartNow()
                .Build();

            await scheduler.ScheduleJob(jobDetail, trigger);

            return Ok(new {
                Success = true,
                CertificateId = certificate.Id,
                Domain = certificate.Domain,
                Status = certificate.Status.ToString(),
                AcmeStatus = certificate.AcmeStatus.ToString(),
                Message = "证书申请已提交，请稍后查询状态"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new {
                Success = false,
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// 查询证书申请状态
    /// </summary>
    /// <param name="certificateId">证书ID</param>
    /// <returns>证书状态信息</returns>
    [HttpGet("certificates/{certificateId}/status")]
    public async Task<IActionResult> GetCertificateStatus(Guid certificateId)
    {
        try
        {
            var certificate = await _certificateRepository.GetByIdAsync(certificateId);
            if (certificate == null)
            {
                return NotFound(new {
                    Success = false,
                    Error = "证书不存在"
                });
            }

            return Ok(new {
                Success = true,
                CertificateId = certificate.Id,
                Domain = certificate.Domain,
                Status = certificate.Status.ToString(),
                AcmeStatus = certificate.AcmeStatus.ToString(),
                AcmeStatusMessage = certificate.AcmeStatusMessage,
                CreatedAt = certificate.CreatedAt,
                UpdatedAt = certificate.UpdatedAt,
                ExpiresAt = certificate.ExpiresAt
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new {
                Success = false,
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// 下载指定域名的最新有效证书
    /// </summary>
    /// <param name="domain">域名</param>
    /// <param name="format">证书格式</param>
    /// <param name="password">证书密码（仅PFX格式需要）</param>
    /// <returns>证书文件</returns>
    [HttpGet("certificates/{domain}/download")]
    public async Task<IActionResult> DownloadLatestCertificate(string domain, [FromQuery] CertificateFormat format, [FromQuery] string? password = null)
    {
        try
        {
            var certificate = await _certificateRepository.GetLatestValidCertificateByDomainAsync(domain);
            if (certificate == null)
            {
                return NotFound(new {
                    Success = false,
                    Error = "Certificate not found for the specified domain"
                });
            }

            var certificateData = await _certificateService.ExportCertificateAsync(certificate.Id, format, password);
            
            var contentType = format switch
            {
                CertificateFormat.Pfx => "application/x-pkcs12",
                CertificateFormat.Pem => "application/x-pem-file",
                CertificateFormat.Crt => "application/zip",
                _ => "application/octet-stream"
            };

            var fileExtension = format == CertificateFormat.Crt ? "zip" : format.ToString().ToLower();
            var fileName = $"{domain}.{fileExtension}";

            return File(certificateData, contentType, fileName);
        }
        catch (Exception ex)
        {
            return BadRequest(new {
                Success = false,
                Error = ex.Message
            });
        }
    }
}

public class CertificateRequest
{
    public string Domain { get; set; } = string.Empty;
    public bool IsWildcard { get; set; }
    public bool UseStaging { get; set; }
}

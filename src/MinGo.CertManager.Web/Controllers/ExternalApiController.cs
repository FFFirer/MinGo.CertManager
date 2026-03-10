using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MinGo.CertManager.Core.Entities;
using MinGo.CertManager.Infrastructure.Repositories;
using MinGo.CertManager.Core.Services;
using MinGo.CertManager.Infrastructure.Services;

namespace MinGo.CertManager.Web.Controllers;

[ApiController]
[Route("api/external")]
public class ExternalApiController : ControllerBase
{
    private readonly ICertificateService _certificateService;
    private readonly IAcmeService _acmeService;
    private readonly IAliyunDnsService _aliyunDnsService;
    private readonly ICertificateRepository _certificateRepository;

    public ExternalApiController(
        ICertificateService certificateService,
        IAcmeService acmeService,
        IAliyunDnsService aliyunDnsService,
        ICertificateRepository certificateRepository)
    {
        _certificateService = certificateService;
        _acmeService = acmeService;
        _aliyunDnsService = aliyunDnsService;
        _certificateRepository = certificateRepository;
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
            var dnsProvider = new DnsProvider
            {
                Id = Guid.NewGuid(),
                ProviderType = DnsProviderType.Aliyun,
                AccessKeyId = string.Empty, // 使用配置中的默认值
                AccessKeySecret = string.Empty, // 使用配置中的默认值
                RegionId = "cn-hangzhou",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var certificate = await _certificateService.RequestCertificateAsync(
                request.Domain,
                request.IsWildcard,
                dnsProvider,
                request.UseStaging);

            return Ok(new {
                Success = true,
                CertificateId = certificate.Id,
                Domain = certificate.Domain,
                Status = certificate.Status.ToString(),
                CreatedAt = certificate.CreatedAt,
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
                CertificateFormat.Crt => "application/x-x509-ca-cert",
                _ => "application/octet-stream"
            };

            var fileExtension = format.ToString().ToLower();
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
    public string Domain { get; set; }
    public bool IsWildcard { get; set; }
    public bool UseStaging { get; set; }
}

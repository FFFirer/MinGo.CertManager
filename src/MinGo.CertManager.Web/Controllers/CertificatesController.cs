using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MinGo.CertManager.Core.Entities;
using MinGo.CertManager.Core.Services;

namespace MinGo.CertManager.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CertificatesController : ControllerBase
{
    private readonly ICertificateService _certificateService;

    public CertificatesController(ICertificateService certificateService)
    {
        _certificateService = certificateService;
    }

    [HttpGet("download/{id}")]
    public async Task<IActionResult> DownloadCertificate(Guid id, [FromQuery] CertificateFormat format, [FromQuery] string? password = null)
    {
        try
        {
            var certificateData = await _certificateService.ExportCertificateAsync(id, format, password);
            
            var contentType = format switch
            {
                CertificateFormat.Pfx => "application/x-pkcs12",
                CertificateFormat.Pem => "application/x-pem-file",
                CertificateFormat.Crt => "application/x-x509-ca-cert",
                _ => "application/octet-stream"
            };

            var fileExtension = format.ToString().ToLower();
            var fileName = $"certificate.{fileExtension}";

            return File(certificateData, contentType, fileName);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

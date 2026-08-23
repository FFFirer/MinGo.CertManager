using MinGo.CertManager.Core.Entities;

namespace MinGo.CertManager.Core.Services;

/// <summary>
/// 证书服务接口
/// </summary>
public interface ICertificateService
{
    /// <summary>
    /// 申请证书（创建新记录）
    /// </summary>
    /// <param name="domain">域名</param>
    /// <param name="isWildcard">是否通配符证书</param>
    /// <param name="dnsProvider">DNS提供商</param>
    /// <param name="useStaging">是否使用测试环境</param>
    /// <returns>证书实体</returns>
    Task<Certificate> RequestCertificateAsync(string domain, bool isWildcard, DnsProvider dnsProvider, bool useStaging = false);

    /// <summary>
    /// 处理已存在的证书申请（更新现有记录，不创建新记录）
    /// </summary>
    /// <param name="certificateId">证书ID</param>
    /// <returns>更新后的证书实体</returns>
    Task<Certificate> ProcessCertificateAsync(Guid certificateId);

    /// <summary>
    /// 续签证书
    /// </summary>
    /// <param name="certificateId">证书ID</param>
    /// <returns>更新后的证书实体</returns>
    Task<Certificate> RenewCertificateAsync(Guid certificateId);
    
    /// <summary>
    /// 导出证书
    /// </summary>
    /// <param name="certificateId">证书ID</param>
    /// <param name="format">证书格式</param>
    /// <param name="password">密码（仅PFX格式需要）</param>
    /// <returns>证书字节数组</returns>
    Task<byte[]> ExportCertificateAsync(Guid certificateId, CertificateFormat format, string? password = null);
}

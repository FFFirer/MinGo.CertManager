namespace MinGo.CertManager.Core.Services;

/// <summary>
/// ACME 服务接口
/// </summary>
public interface IAcmeService
{
    /// <summary>
    /// 申请证书
    /// </summary>
    /// <param name="domain">域名</param>
    /// <param name="isWildcard">是否通配符证书</param>
    /// <param name="dnsService">DNS服务</param>
    /// <param name="useStaging">是否使用测试环境</param>
    /// <returns>证书结果</returns>
    Task<CertificateResult> RequestCertificateAsync(string domain, bool isWildcard, IAliyunDnsService dnsService, bool useStaging = false);
    
    /// <summary>
    /// 清理资源
    /// </summary>
    /// <param name="domain">域名</param>
    /// <param name="isWildcard">是否通配符证书</param>
    /// <param name="dnsService">DNS服务</param>
    /// <returns>任务</returns>
    Task CleanupAsync(string domain, bool isWildcard, IAliyunDnsService dnsService);
}

/// <summary>
/// 证书结果
/// </summary>
public class CertificateResult
{
    /// <summary>
    /// 证书PEM
    /// </summary>
    public string CertificatePem { get; set; } = string.Empty;
    
    /// <summary>
    /// 私钥PEM
    /// </summary>
    public string PrivateKeyPem { get; set; } = string.Empty;
    
    /// <summary>
    /// 证书链PEM
    /// </summary>
    public string CertificateChainPem { get; set; } = string.Empty;
}

/// <summary>
/// ACME环境
/// </summary>
public enum AcmeEnvironment
{
    /// <summary>
    /// 生产环境
    /// </summary>
    Production,
    
    /// <summary>
    /// 测试环境
    /// </summary>
    Staging
}

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
    /// <param name="dnsService">DNS 提供商客户端</param>
    /// <param name="useStaging">是否使用测试环境</param>
    /// <param name="zoneName">已购域名（Zone）；提供时优先用于 DNS 记录定位</param>
    Task<CertificateResult> RequestCertificateAsync(
        string domain,
        bool isWildcard,
        IDomainProviderClient dnsService,
        bool useStaging = false,
        string? zoneName = null);

    /// <summary>
    /// 清理资源
    /// </summary>
    Task CleanupAsync(
        string domain,
        bool isWildcard,
        IDomainProviderClient dnsService,
        string? zoneName = null);
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

using System;

namespace MinGo.CertManager.Core.Entities;

public class Certificate
{
    public Guid Id { get; set; }
    public string Domain { get; set; } = string.Empty;
    public bool IsWildcard { get; set; }
    public bool UseStaging { get; set; }
    /// <summary>ACME 账户邮箱标识；为空时使用部署配置默认值</summary>
    public string? AccountEmail { get; set; }
    public CertificateStatus Status { get; set; }
    public AcmeProcessStatus AcmeStatus { get; set; }
    public string? AcmeStatusMessage { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string CertificateContent { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
    public string CertificateChain { get; set; } = string.Empty;
    /// <summary>续期来源证书 Id；非空表示本记录由续期创建</summary>
    public Guid? RenewedFromId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

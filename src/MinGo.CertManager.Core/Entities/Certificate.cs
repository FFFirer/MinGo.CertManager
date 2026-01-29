using System;

namespace MinGo.CertManager.Core.Entities;

public class Certificate
{
    public Guid Id { get; set; }
    public string Domain { get; set; } = string.Empty;
    public bool IsWildcard { get; set; }
    public CertificateStatus Status { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string CertificateContent { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
    public string CertificateChain { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

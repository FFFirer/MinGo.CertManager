using System;

namespace MinGo.CertManager.Core.Entities;

public class AcmeAccount
{
    public Guid Id { get; set; }
    public string AccountId { get; set; } = string.Empty;
    public string AccountKey { get; set; } = string.Empty;
    public string Contact { get; set; } = string.Empty;
    public string AcmeServerUrl { get; set; } = string.Empty;
    public bool IsStaging { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastUsedAt { get; set; }
}

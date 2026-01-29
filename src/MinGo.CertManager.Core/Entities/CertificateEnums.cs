namespace MinGo.CertManager.Core.Entities;

public enum CertificateStatus
{
    Pending = 0,
    Active = 1,
    Expiring = 2,
    Expired = 3,
    Failed = 4
}

public enum CertificateFormat
{
    Pfx = 0,
    Pem = 1,
    Crt = 2
}

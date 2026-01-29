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

public enum AcmeProcessStatus
{
    Initializing = 0,
    CreatingAccount = 1,
    CreatingOrder = 2,
    ProcessingAuthorization = 3,
    CreatingDnsRecord = 4,
    WaitingDnsPropagation = 5,
    ValidatingChallenge = 6,
    GeneratingCertificate = 7,
    Completed = 8,
    Failed = 9
}

namespace MinGo.CertManager.Core.Constants;

public static class CertificateConstants
{
    public const int CertificateValidityDays = 90;
    public const int DnsPropagationDelayMilliseconds = 10000;
    public const int AuthorizationCheckDelayMilliseconds = 10000;
    public const int AuthorizationCheckMaxRetries = 30;
    public const int AuthorizationCheckIntervalMilliseconds = 2000;
}

public static class AcmeConstants
{
    public static readonly Uri LetsEncryptProductionUri = new Uri("https://acme-v02.api.letsencrypt.org/directory");
    public static readonly Uri LetsEncryptStagingUri = new Uri("https://acme-staging-v02.api.letsencrypt.org/directory");
    public static readonly string DefaultAccountEmail = "admin@example.com";
}

public static class AliyunDnsConstants
{
    public const string ApiVersion = "2015-01-09";
    public const string Service = "Alidns";
    public const string Endpoint = "https://alidns.aliyuncs.com/";
    public const string DefaultRegionId = "cn-hangzhou";
}

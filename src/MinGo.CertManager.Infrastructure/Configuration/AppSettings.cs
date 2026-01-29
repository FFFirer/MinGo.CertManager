namespace MinGo.CertManager.Infrastructure.Configuration;

public class AcmeSettings
{
    public const string SectionName = "Acme";

    public bool UseStaging { get; set; }
    public string AccountEmail { get; set; } = string.Empty;
    public string LetsEncryptProductionUrl { get; set; } = string.Empty;
    public string LetsEncryptStagingUrl { get; set; } = string.Empty;
}

public class CertificateSettings
{
    public const string SectionName = "Certificate";

    public int ValidityDays { get; set; } = 90;
    public int RenewalDaysBeforeExpiry { get; set; } = 30;
}

public class AliyunDnsSettings
{
    public const string SectionName = "AliyunDns";

    public string AccessKeyId { get; set; } = string.Empty;
    public string AccessKeySecret { get; set; } = string.Empty;
    public string RegionId { get; set; } = "cn-hangzhou";
    public string ApiVersion { get; set; } = "2015-01-09";
    public string Endpoint { get; set; } = "https://alidns.aliyuncs.com/";
}

public class QuartzSettings
{
    public const string SectionName = "Quartz";

    public string SchedulerName { get; set; } = "MinGo CertManager Scheduler";
    public string SchedulerInstanceId { get; set; } = "MinGo-CertManager-Scheduler";
}

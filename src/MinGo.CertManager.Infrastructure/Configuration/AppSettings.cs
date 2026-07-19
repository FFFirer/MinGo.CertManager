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
}

public class QuartzSettings
{
    public const string SectionName = "Quartz";

    public string SchedulerName { get; set; } = "MinGo CertManager Scheduler";
    public string SchedulerInstanceId { get; set; } = "MinGo-CertManager-Scheduler";
}

public class ApiKeySettings
{
    public const string SectionName = "ApiKeys";

    public List<ApiKeyItem> Keys { get; set; } = new List<ApiKeyItem>();
}

public class ApiKeyItem
{
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiredAt { get; set; }
}

public class ForwardedHeadersSettings
{
    public const string SectionName = "ForwardedHeaders";

    public bool Enabled { get; set; } = false;
}

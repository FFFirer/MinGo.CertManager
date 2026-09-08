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

/// <summary>
/// MinGo.Quartz.Agent SDK 集成开关与暴露设置。
/// Agent 自身的心跳/平台地址等参数由 SDK 从顶级 <c>Agent</c> / <c>Platform</c> 配置节绑定，
/// 详见 https://www.nuget.org/packages/MinGo.Quartz.Agent/ 。
/// </summary>
public class QuartzAgentSettings
{
    public const string SectionName = "QuartzAgent";

    /// <summary>是否启用 MinGo.Quartz.Agent SDK（关闭时不注册 Agent 服务与 API 端点）。</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>Agent Minimal API 路由前缀，默认 <c>/api/agent</c>。</summary>
    public string ApiPrefix { get; set; } = "/api/agent";
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

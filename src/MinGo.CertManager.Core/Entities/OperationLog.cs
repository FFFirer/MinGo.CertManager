namespace MinGo.CertManager.Core.Entities;

/// <summary>
/// 操作流水动作类型
/// </summary>
public enum OperationLogAction
{
    ZoneSynced = 0,
    RecordSynced = 1,
    DomainSynced = 2,
    DomainAdded = 3,
    DomainRemoved = 4,
    DnsTxtCreated = 5,
    DnsTxtDeleted = 6,
    CertRequested = 7
}

/// <summary>
/// 操作结果
/// </summary>
public enum OperationLogResult
{
    Success = 0,
    Failed = 1
}

/// <summary>
/// 域名 / DNS 相关操作的可追溯流水
/// </summary>
public class OperationLog
{
    public Guid Id { get; set; }
    public DateTime At { get; set; }
    /// <summary>操作者：用户名或 system</summary>
    public string Actor { get; set; } = "system";
    public OperationLogAction Action { get; set; }
    /// <summary>目标类型：Domain / DomainZone / DomainRecord / Certificate</summary>
    public string TargetType { get; set; } = string.Empty;
    public string? TargetId { get; set; }
    public string? TargetName { get; set; }
    public DnsProviderType ProviderType { get; set; }
    public OperationLogResult Result { get; set; }
    /// <summary>可读摘要，禁止包含 AccessKey/Secret</summary>
    public string Message { get; set; } = string.Empty;
    /// <summary>OpenTelemetry TraceId（存在时写入）</summary>
    public string? TraceId { get; set; }
    /// <summary>可选变更摘要 JSON</summary>
    public string? Payload { get; set; }
}

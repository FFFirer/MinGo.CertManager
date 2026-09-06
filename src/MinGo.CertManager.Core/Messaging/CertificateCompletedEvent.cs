using MinGo.Messaging;

namespace MinGo.CertManager.Core.Messaging;

/// <summary>
/// 证书申请完成事件
/// </summary>
[MessageContract(Id = "certmanager.certificate.completed", Version = "1", Kind = MessageKind.Event)]
public class CertificateCompletedEvent : IEvent
{
    /// <summary>
    /// 证书ID
    /// </summary>
    public Guid CertificateId { get; set; }

    /// <summary>
    /// 域名
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}

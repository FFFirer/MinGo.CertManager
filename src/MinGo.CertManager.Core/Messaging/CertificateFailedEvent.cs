using MinGo.Messaging;

namespace MinGo.CertManager.Core.Messaging;

/// <summary>
/// 证书申请失败事件
/// </summary>
[MessageContract(Id = "certmanager.certificate.failed", Version = "1", Kind = MessageKind.Event)]
public class CertificateFailedEvent : IEvent
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
    /// 错误信息
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
}

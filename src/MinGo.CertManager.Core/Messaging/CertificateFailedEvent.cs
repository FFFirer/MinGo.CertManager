using MinGo.Messaging;

namespace MinGo.CertManager.Core.Messaging;

/// <summary>
/// 证书申请失败事件
/// </summary>
[MessageContract(Id = "a1b2c3d4-0003-0001-0001-000000000001", Version = "1.0", Kind = MessageKind.Event)]
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

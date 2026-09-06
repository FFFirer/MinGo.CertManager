using MinGo.Messaging;

namespace MinGo.CertManager.Core.Messaging;

/// <summary>
/// 证书申请命令
/// </summary>
[MessageContract(Id = "a1b2c3d4-0001-0001-0001-000000000001", Version = "1.0", Kind = MessageKind.Command)]
public class RequestCertificateCommand : ICommand
{
    /// <summary>
    /// 证书ID
    /// </summary>
    public Guid CertificateId { get; set; }
}

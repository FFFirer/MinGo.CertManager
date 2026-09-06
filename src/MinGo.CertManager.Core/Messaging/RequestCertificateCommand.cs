using MinGo.Messaging;

namespace MinGo.CertManager.Core.Messaging;

/// <summary>
/// 证书申请命令
/// </summary>
[MessageContract(Id = "certmanager.certificate.request", Version = "1", Kind = MessageKind.Command)]
public class RequestCertificateCommand : ICommand
{
    /// <summary>
    /// 证书ID
    /// </summary>
    public Guid CertificateId { get; set; }
}

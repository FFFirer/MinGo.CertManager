using MinGo.Messaging;

namespace MinGo.CertManager.Core.Messaging;

[MessageContract(Id = "certmanager.custom.event", Version = "1", Kind = MessageKind.Event)]
public class CustomEvent : IEvent
{
    /// <summary>
    /// id
    /// </summary>
    public string EventId { get; init;} = "";
}

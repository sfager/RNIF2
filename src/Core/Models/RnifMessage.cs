namespace RNIF2.Core.Models;

public sealed class RnifMessage
{
    public required PreambleHeader Preamble { get; init; }
    public required DeliveryHeader Delivery { get; init; }
    public required ServiceHeader Service { get; init; }
    public required ServiceContent Content { get; init; }
    public bool IsSigned { get; init; }
    public bool IsEncrypted { get; init; }
    public MessagePattern Pattern { get; init; } = MessagePattern.Async;
}

namespace RNIF2.Core.Models;

public sealed class DeliveryHeader
{
    public required TradingPartnerIdentity FromPartner { get; init; }
    public required TradingPartnerIdentity ToPartner { get; init; }
    public required string MessageTrackingId { get; init; }
    public required DateTimeOffset MessageDateTime { get; init; }
}

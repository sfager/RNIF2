namespace RNIF2.Core.Models;

public sealed class TradingPartnerIdentity
{
    public required string GlobalBusinessIdentifier { get; init; }
    public string? BusinessName { get; init; }
    public string? GlobalPartnerRoleClassification { get; init; }
}

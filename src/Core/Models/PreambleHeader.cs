namespace RNIF2.Core.Models;

public sealed class PreambleHeader
{
    public required string RnifVersionIdentifier { get; init; }
    public required string GlobalUsageCode { get; init; }
    public required DateTimeOffset TimeDelivered { get; init; }
}

namespace RNIF2.Core.Models;

public sealed class ServiceHeader
{
    public required string PipCode { get; init; }
    public required string PipVersion { get; init; }
    public required string PipInstanceId { get; init; }
    public string? GlobalProcessCode { get; init; }
    public string? InitiatorRole { get; init; }
    public string? ResponderRole { get; init; }
    public bool IsSignalMessage { get; init; }
    public string? SignalType { get; init; }
    public bool IsEncrypted { get; init; }
}

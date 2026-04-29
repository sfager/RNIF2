namespace RNIF2.Core.Models;

public sealed class ParsedMimePart
{
    public required string ContentType { get; init; }
    public required byte[] Data { get; init; }
    public string? ContentId { get; init; }
    public bool IsSmime { get; init; }
}

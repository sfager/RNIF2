using System.Xml.Linq;

namespace RNIF2.Core.Models;

public sealed class ServiceContent
{
    public required XDocument PayloadXml { get; init; }
    public IReadOnlyList<ServiceAttachment> Attachments { get; init; } = [];
    public bool IsEncrypted { get; init; }
}

public sealed class ServiceAttachment
{
    public required string ContentType { get; init; }
    public required byte[] Data { get; init; }
    public string? ContentId { get; init; }
    public string? FileName { get; init; }
}

using RNIF2.Core.Models;

namespace RNIF2.Core.Interfaces;

public interface IMimeParser
{
    Task<IReadOnlyList<ParsedMimePart>> ParseAsync(Stream body, string contentType, CancellationToken ct);
}

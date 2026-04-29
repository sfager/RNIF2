using RNIF2.Core.Models;

namespace RNIF2.Core.Interfaces;

public interface ISecurityService
{
    Task<ParsedMimePart> VerifySignatureAsync(ParsedMimePart part, CancellationToken ct);
    Task<ParsedMimePart> DecryptAsync(ParsedMimePart part, CancellationToken ct);
    bool IsEnabled { get; }
}

using RNIF2.Core.Exceptions;
using RNIF2.Core.Models;

namespace RNIF2.Parsing;

public sealed class RnifMessageExtractor
{
    public Task<RnifMessage> ExtractAsync(IReadOnlyList<ParsedMimePart> parts, CancellationToken ct)
    {
        if (parts.Count < 4)
            throw new RnifParsingException($"Expected at least 4 MIME parts; got {parts.Count}.");

        var preamble = PreambleHeaderParser.Parse(parts[0]);
        var delivery = DeliveryHeaderParser.Parse(parts[1]);
        var service = ServiceHeaderParser.Parse(parts[2]);
        var content = ServiceContentParser.Parse(parts[3]);

        var message = new RnifMessage
        {
            Preamble = preamble,
            Delivery = delivery,
            Service = service,
            Content = content,
            IsEncrypted = service.IsEncrypted || content.IsEncrypted
        };

        return Task.FromResult(message);
    }
}

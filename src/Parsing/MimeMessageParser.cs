using MimeKit;
using MimeKit.Cryptography;
using RNIF2.Core.Exceptions;
using RNIF2.Core.Interfaces;
using RNIF2.Core.Models;

namespace RNIF2.Parsing;

public sealed class MimeMessageParser : RNIF2.Core.Interfaces.IMimeParser
{
    public async Task<IReadOnlyList<ParsedMimePart>> ParseAsync(Stream body, string contentType, CancellationToken ct)
    {
        // HttpListener strips the outer Content-Type header; reconstruct a minimal MIME message
        // so MimeKit can parse the multipart boundary correctly.
        using var ms = new MemoryStream();
        var header = $"Content-Type: {contentType}\r\n\r\n";
        var headerBytes = System.Text.Encoding.ASCII.GetBytes(header);
        ms.Write(headerBytes);
        await body.CopyToAsync(ms, ct);
        ms.Position = 0;

        MimeMessage mimeMessage;
        try
        {
            var parser = new MimeParser(ms, MimeFormat.Entity);
            mimeMessage = await parser.ParseMessageAsync(ct);
        }
        catch (Exception ex)
        {
            throw new RnifParsingException("Failed to parse MIME message.", ex);
        }

        if (mimeMessage.Body is not Multipart multipart)
            throw new RnifParsingException("RNIF 2.0 message body must be multipart/related.");

        if (multipart.Count < 4)
            throw new RnifParsingException(
                $"RNIF 2.0 requires at least 4 MIME parts; received {multipart.Count}.");

        var parts = new List<ParsedMimePart>(multipart.Count);
        foreach (var entity in multipart)
        {
            parts.Add(ConvertToParsedPart(entity));
        }

        return parts;
    }

    private static ParsedMimePart ConvertToParsedPart(MimeEntity entity)
    {
        var isSmime = entity is ApplicationPkcs7Mime || entity is MultipartSigned;
        byte[] data;

        using var ms = new MemoryStream();
        if (entity is MimePart part && part.Content != null)
        {
            part.Content.DecodeTo(ms);
        }
        else
        {
            // For multipart sub-entities, serialize them back so the security layer can handle them
            entity.WriteTo(ms);
        }
        data = ms.ToArray();

        return new ParsedMimePart
        {
            ContentType = entity.ContentType.MimeType,
            Data = data,
            ContentId = entity.ContentId,
            IsSmime = isSmime
        };
    }
}

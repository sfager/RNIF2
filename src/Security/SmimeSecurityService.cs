using MimeKit;
using MimeKit.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RNIF2.Core.Configuration;
using RNIF2.Core.Exceptions;
using RNIF2.Core.Interfaces;
using RNIF2.Core.Models;

namespace RNIF2.Security;

public sealed class SmimeSecurityService : ISecurityService
{
    private readonly SecurityOptions _options;
    private readonly CertificateStore _certStore;
    private readonly ILogger<SmimeSecurityService> _logger;

    public bool IsEnabled =>
        _options.EnableSignatureVerification || _options.EnableEncryption;

    public SmimeSecurityService(
        IOptions<RnifOptions> options,
        ILogger<SmimeSecurityService> logger)
    {
        _options = options.Value.Security;
        _certStore = new CertificateStore(_options.CertificateStoreName, _options.CertificateStoreLocation);
        _logger = logger;
    }

    public Task<ParsedMimePart> VerifySignatureAsync(ParsedMimePart part, CancellationToken ct)
    {
        if (!_options.EnableSignatureVerification || !part.IsSmime)
            return Task.FromResult(part);

        try
        {
            using var stream = new MemoryStream(part.Data);
            var entity = MimeEntity.Load(stream);

            if (entity is not MultipartSigned signed)
                return Task.FromResult(part);

            using var ctx = new TemporarySecureMimeContext();
            var signatures = signed.Verify(ctx);
            foreach (var sig in signatures)
            {
                if (!sig.Verify())
                    throw new RnifSecurityException("S/MIME signature verification failed: signature is invalid.");
            }

            _logger.LogDebug("S/MIME signature verified successfully.");

            // Return the signed content (inner part) as the verified data
            using var inner = new MemoryStream();
            signed[0].WriteTo(inner);
            return Task.FromResult(new ParsedMimePart
            {
                ContentType = signed[0].ContentType.MimeType,
                Data = inner.ToArray(),
                ContentId = part.ContentId,
                IsSmime = false
            });
        }
        catch (RnifSecurityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RnifSecurityException("S/MIME signature verification failed.", ex);
        }
    }

    public Task<ParsedMimePart> DecryptAsync(ParsedMimePart part, CancellationToken ct)
    {
        if (!_options.EnableEncryption || !part.IsSmime)
            return Task.FromResult(part);

        if (string.IsNullOrWhiteSpace(_options.SigningCertificateThumbprint))
            throw new RnifSecurityException("Decryption requested but no certificate thumbprint configured.");

        var cert = _certStore.FindByThumbprint(_options.SigningCertificateThumbprint, requirePrivateKey: true)
            ?? throw new RnifSecurityException(
                $"Private key certificate not found for thumbprint '{_options.SigningCertificateThumbprint}'.");

        try
        {
            using var stream = new MemoryStream(part.Data);
            var entity = MimeEntity.Load(stream);

            if (entity is not ApplicationPkcs7Mime encrypted)
                return Task.FromResult(part);

            using var ctx = new TemporarySecureMimeContext();
            var decrypted = encrypted.Decrypt(ctx);

            using var decryptedStream = new MemoryStream();
            decrypted.WriteTo(decryptedStream);
            return Task.FromResult(new ParsedMimePart
            {
                ContentType = decrypted.ContentType.MimeType,
                Data = decryptedStream.ToArray(),
                ContentId = part.ContentId,
                IsSmime = false
            });
        }
        catch (RnifSecurityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RnifSecurityException("S/MIME decryption failed.", ex);
        }
    }
}

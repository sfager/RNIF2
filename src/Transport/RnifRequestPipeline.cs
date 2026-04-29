using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RNIF2.Core.Configuration;
using RNIF2.Core.Exceptions;
using RNIF2.Core.Interfaces;
using RNIF2.Core.Models;
using RNIF2.Parsing;

namespace RNIF2.Transport;

public sealed class RnifRequestPipeline : IRnifRequestPipeline
{
    private readonly IMimeParser _mimeParser;
    private readonly RnifMessageExtractor _extractor;
    private readonly IMessageValidator _validator;
    private readonly ISecurityService _security;
    private readonly IMessageRouter _router;
    private readonly ISignalGenerator _signalGen;
    private readonly IRnifMessageSerializer _serializer;
    private readonly RnifOptions _options;
    private readonly ILogger<RnifRequestPipeline> _logger;

    public RnifRequestPipeline(
        IMimeParser mimeParser,
        RnifMessageExtractor extractor,
        IMessageValidator validator,
        ISecurityService security,
        IMessageRouter router,
        ISignalGenerator signalGen,
        IRnifMessageSerializer serializer,
        IOptions<RnifOptions> options,
        ILogger<RnifRequestPipeline> logger)
    {
        _mimeParser = mimeParser;
        _extractor = extractor;
        _validator = validator;
        _security = security;
        _router = router;
        _signalGen = signalGen;
        _serializer = serializer;
        _options = options.Value;
        _logger = logger;
    }

    public async Task HandleAsync(HttpRequestContext ctx, CancellationToken ct)
    {
        if (!ctx.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            await WriteErrorAsync(ctx.Response, HttpStatusCode.MethodNotAllowed, "Only POST is supported.", ct);
            return;
        }

        // Stage 1: MIME Parse
        IReadOnlyList<ParsedMimePart> parts;
        try
        {
            parts = await _mimeParser.ParseAsync(ctx.RequestBody, ctx.ContentType, ct);
        }
        catch (RnifParsingException ex)
        {
            _logger.LogWarning(ex, "MIME parse failure");
            await WriteErrorAsync(ctx.Response, HttpStatusCode.BadRequest, ex.Message, ct);
            return;
        }

        // Stage 2: Security — decrypt/verify S/MIME parts if enabled
        if (_security.IsEnabled)
        {
            var securedParts = new List<ParsedMimePart>(parts.Count);
            for (var i = 0; i < parts.Count; i++)
            {
                try
                {
                    var part = await _security.VerifySignatureAsync(parts[i], ct);
                    part = await _security.DecryptAsync(part, ct);
                    securedParts.Add(part);
                }
                catch (RnifSecurityException ex)
                {
                    _logger.LogWarning(ex, "Security failure on MIME part {Index}", i);
                    var exSignal = await _signalGen.CreateExceptionSignalAsync(
                        null, ex.Message, RnifFailureCode.AuthenticationFailed, ct);
                    await WriteSignalAsync(ctx.Response, exSignal, ct);
                    return;
                }
            }
            parts = securedParts;
        }

        // Stage 3: Extract RNIF message
        RnifMessage message;
        try
        {
            message = await _extractor.ExtractAsync(parts, ct);
        }
        catch (RnifParsingException ex)
        {
            _logger.LogWarning(ex, "RNIF extraction failure");
            var exSignal = await _signalGen.CreateExceptionSignalAsync(
                null, ex.Message, RnifFailureCode.ParseError, ct);
            await WriteSignalAsync(ctx.Response, exSignal, ct);
            return;
        }

        _logger.LogInformation(
            "Processing RNIF message PIP={Pip} TrackingId={Id} From={From}",
            message.Service.PipCode, message.Delivery.MessageTrackingId,
            message.Delivery.FromPartner.GlobalBusinessIdentifier);

        // Stage 4: Validate
        var validationErrors = _validator.Validate(message);
        if (validationErrors.Count > 0)
        {
            var errorText = string.Join("; ", validationErrors);
            _logger.LogWarning("Validation failed: {Errors}", errorText);
            var exSignal = await _signalGen.CreateExceptionSignalAsync(
                message, errorText, RnifFailureCode.ValidationError, ct);
            await WriteSignalAsync(ctx.Response, exSignal, ct);
            return;
        }

        // Stage 5: Route & Handle
        RnifHandlerResult result;
        try
        {
            result = await _router.RouteAsync(message, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Handler threw unexpectedly for PIP {Pip}", message.Service.PipCode);
            var exSignal = await _signalGen.CreateExceptionSignalAsync(
                message, "Internal server error.", RnifFailureCode.SystemError, ct);
            await WriteSignalAsync(ctx.Response, exSignal, ct);
            return;
        }

        // Stage 6 & 7: Generate and send signal/response
        if (!result.Success)
        {
            var exSignal = await _signalGen.CreateExceptionSignalAsync(
                message,
                result.FailureReason ?? "Handler reported failure.",
                result.FailureCode ?? RnifFailureCode.Unknown,
                ct);
            await WriteSignalAsync(ctx.Response, exSignal, ct);
            return;
        }

        // Synchronous PIP: handler provided an inline response
        if (result.SyncResponse is not null)
        {
            await WriteSignalAsync(ctx.Response, result.SyncResponse, ct);
            return;
        }

        // Asynchronous PIP: return ReceiptAcknowledgmentSignal
        var ack = await _signalGen.CreateReceiptAcknowledgmentAsync(message, ct);
        await WriteSignalAsync(ctx.Response, ack, ct);
    }

    private async Task WriteSignalAsync(HttpListenerResponse response, RnifMessage signal, CancellationToken ct)
    {
        response.StatusCode = (int)HttpStatusCode.OK;
        response.ContentType = _serializer.GetContentType(signal);
        using var ms = new MemoryStream();
        await _serializer.SerializeAsync(signal, ms, ct);
        var body = ms.ToArray();
        response.ContentLength64 = body.Length;
        await response.OutputStream.WriteAsync(body, ct);
        response.Close();
    }

    private static async Task WriteErrorAsync(
        HttpListenerResponse response,
        HttpStatusCode code,
        string message,
        CancellationToken ct)
    {
        response.StatusCode = (int)code;
        response.ContentType = "text/plain; charset=utf-8";
        var body = Encoding.UTF8.GetBytes(message);
        response.ContentLength64 = body.Length;
        await response.OutputStream.WriteAsync(body, ct);
        response.Close();
    }
}

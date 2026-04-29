using Microsoft.Extensions.Logging;
using RNIF2.Core.Interfaces;
using RNIF2.Core.Models;

namespace RNIF2.Handlers;

public sealed class DefaultReceiptHandler : IMessageHandler
{
    private readonly ILogger<DefaultReceiptHandler> _logger;

    public string PipCode => "*";

    public DefaultReceiptHandler(ILogger<DefaultReceiptHandler> logger)
    {
        _logger = logger;
    }

    public Task<RnifHandlerResult> HandleAsync(RnifMessage message, CancellationToken ct)
    {
        _logger.LogInformation(
            "Received RNIF message: PIP={PipCode} TrackingId={TrackingId} From={From}",
            message.Service.PipCode,
            message.Delivery.MessageTrackingId,
            message.Delivery.FromPartner.GlobalBusinessIdentifier);

        return Task.FromResult(RnifHandlerResult.Ok());
    }
}

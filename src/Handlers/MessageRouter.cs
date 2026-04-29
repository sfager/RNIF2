using Microsoft.Extensions.Logging;
using RNIF2.Core.Interfaces;
using RNIF2.Core.Models;

namespace RNIF2.Handlers;

public sealed class MessageRouter : IMessageRouter
{
    private readonly IEnumerable<IMessageHandler> _handlers;
    private readonly ILogger<MessageRouter> _logger;

    public MessageRouter(IEnumerable<IMessageHandler> handlers, ILogger<MessageRouter> logger)
    {
        _handlers = handlers;
        _logger = logger;
    }

    public async Task<RnifHandlerResult> RouteAsync(RnifMessage message, CancellationToken ct)
    {
        var pipCode = message.Service.PipCode;

        // Exact match first, then catch-all
        var handler = _handlers.FirstOrDefault(h =>
                          h.PipCode.Equals(pipCode, StringComparison.OrdinalIgnoreCase))
                      ?? _handlers.FirstOrDefault(h => h.PipCode == "*");

        if (handler is null)
        {
            _logger.LogWarning("No handler registered for PIP {PipCode}", pipCode);
            return RnifHandlerResult.Fail(
                $"No handler for PIP '{pipCode}'.",
                RnifFailureCode.UnknownPip);
        }

        _logger.LogDebug("Routing PIP {PipCode} to handler {Handler}", pipCode, handler.GetType().Name);
        return await handler.HandleAsync(message, ct);
    }
}

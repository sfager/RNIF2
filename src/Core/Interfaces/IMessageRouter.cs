using RNIF2.Core.Models;

namespace RNIF2.Core.Interfaces;

public interface IMessageRouter
{
    Task<RnifHandlerResult> RouteAsync(RnifMessage message, CancellationToken ct);
}

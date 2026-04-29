using RNIF2.Core.Models;

namespace RNIF2.Core.Interfaces;

public interface IOutboundClient
{
    Task<RnifMessage> SendAsync(RnifMessage message, Uri partnerEndpoint, CancellationToken ct);
}

using RNIF2.Core.Models;

namespace RNIF2.Core.Interfaces;

public interface IRnifMessageSerializer
{
    Task SerializeAsync(RnifMessage message, Stream output, CancellationToken ct);
    string GetContentType(RnifMessage message);
}

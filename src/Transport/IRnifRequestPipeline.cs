namespace RNIF2.Transport;

public interface IRnifRequestPipeline
{
    Task HandleAsync(HttpRequestContext ctx, CancellationToken ct);
}

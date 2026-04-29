using RNIF2.Core.Models;

namespace RNIF2.Core.Interfaces;

public interface IMessageHandler
{
    string PipCode { get; }
    Task<RnifHandlerResult> HandleAsync(RnifMessage message, CancellationToken ct);
}

public sealed class RnifHandlerResult
{
    public bool Success { get; init; }
    public string? FailureReason { get; init; }
    public RnifFailureCode? FailureCode { get; init; }
    public RnifMessage? SyncResponse { get; init; }

    public static RnifHandlerResult Ok() => new() { Success = true };
    public static RnifHandlerResult Fail(string reason, RnifFailureCode code) =>
        new() { Success = false, FailureReason = reason, FailureCode = code };
}

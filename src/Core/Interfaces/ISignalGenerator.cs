using RNIF2.Core.Models;

namespace RNIF2.Core.Interfaces;

public interface ISignalGenerator
{
    Task<RnifMessage> CreateReceiptAcknowledgmentAsync(RnifMessage originalMessage, CancellationToken ct);
    Task<RnifMessage> CreateExceptionSignalAsync(
        RnifMessage? originalMessage,
        string errorDescription,
        RnifFailureCode failureCode,
        CancellationToken ct);
}

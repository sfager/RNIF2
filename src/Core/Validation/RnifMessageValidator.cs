using RNIF2.Core.Interfaces;
using RNIF2.Core.Models;

namespace RNIF2.Core.Validation;

public sealed class RnifMessageValidator : IMessageValidator
{
    public IReadOnlyList<string> Validate(RnifMessage message)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(message.Preamble.RnifVersionIdentifier))
            errors.Add("Preamble: missing RnifVersionIdentifier");

        if (!message.Preamble.RnifVersionIdentifier.StartsWith("V02", StringComparison.OrdinalIgnoreCase))
            errors.Add($"Preamble: unsupported RNIF version '{message.Preamble.RnifVersionIdentifier}'; expected V02.xx");

        if (string.IsNullOrWhiteSpace(message.Delivery.MessageTrackingId))
            errors.Add("DeliveryHeader: missing MessageTrackingID");

        if (string.IsNullOrWhiteSpace(message.Delivery.FromPartner.GlobalBusinessIdentifier))
            errors.Add("DeliveryHeader: missing FromPartner.GlobalBusinessIdentifier");

        if (string.IsNullOrWhiteSpace(message.Delivery.ToPartner.GlobalBusinessIdentifier))
            errors.Add("DeliveryHeader: missing ToPartner.GlobalBusinessIdentifier");

        if (!message.Service.IsEncrypted)
        {
            if (string.IsNullOrWhiteSpace(message.Service.PipCode))
                errors.Add("ServiceHeader: missing PipCode");

            if (string.IsNullOrWhiteSpace(message.Service.PipVersion))
                errors.Add("ServiceHeader: missing PipVersion");

            if (string.IsNullOrWhiteSpace(message.Service.PipInstanceId))
                errors.Add("ServiceHeader: missing PipInstanceId");
        }

        return errors;
    }
}

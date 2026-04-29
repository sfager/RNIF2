using Microsoft.Extensions.Options;
using RNIF2.Core.Configuration;
using RNIF2.Core.Interfaces;
using RNIF2.Core.Models;

namespace RNIF2.Signals;

public sealed class SignalGenerator : ISignalGenerator
{
    private readonly RnifOptions _options;

    public SignalGenerator(IOptions<RnifOptions> options)
    {
        _options = options.Value;
    }

    public Task<RnifMessage> CreateReceiptAcknowledgmentAsync(RnifMessage originalMessage, CancellationToken ct)
    {
        var signalXml = ReceiptAcknowledgmentBuilder.Build(
            originalMessage, _options.LocalTradingPartnerId, _options.LocalBusinessName);

        var signal = BuildSignalMessage(originalMessage, signalXml, "ReceiptAcknowledgmentSignal");
        return Task.FromResult(signal);
    }

    public Task<RnifMessage> CreateExceptionSignalAsync(
        RnifMessage? originalMessage,
        string errorDescription,
        RnifFailureCode failureCode,
        CancellationToken ct)
    {
        var signalXml = ExceptionSignalBuilder.Build(
            originalMessage, failureCode, errorDescription,
            _options.LocalTradingPartnerId);

        var signal = BuildSignalMessage(originalMessage, signalXml, "ExceptionSignal");
        return Task.FromResult(signal);
    }

    private RnifMessage BuildSignalMessage(
        RnifMessage? original,
        System.Xml.Linq.XDocument signalXml,
        string signalType)
    {
        var now = DateTimeOffset.UtcNow;
        var usageCode = original?.Preamble.GlobalUsageCode ?? "Production";

        return new RnifMessage
        {
            Preamble = new PreambleHeader
            {
                RnifVersionIdentifier = "V02.00",
                GlobalUsageCode = usageCode,
                TimeDelivered = now
            },
            Delivery = new DeliveryHeader
            {
                FromPartner = new TradingPartnerIdentity
                {
                    GlobalBusinessIdentifier = _options.LocalTradingPartnerId,
                    BusinessName = _options.LocalBusinessName
                },
                ToPartner = original?.Delivery.FromPartner
                    ?? new TradingPartnerIdentity { GlobalBusinessIdentifier = string.Empty },
                MessageTrackingId = $"uuid:{Guid.NewGuid()}",
                MessageDateTime = now
            },
            Service = new ServiceHeader
            {
                PipCode = original?.Service.PipCode ?? string.Empty,
                PipVersion = original?.Service.PipVersion ?? string.Empty,
                PipInstanceId = original?.Service.PipInstanceId ?? string.Empty,
                IsSignalMessage = true,
                SignalType = signalType
            },
            Content = new ServiceContent { PayloadXml = signalXml }
        };
    }
}

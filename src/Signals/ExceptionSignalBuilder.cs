using System.Xml.Linq;
using RNIF2.Core.Constants;
using RNIF2.Core.Models;

namespace RNIF2.Signals;

public static class ExceptionSignalBuilder
{
    public static XDocument Build(
        RnifMessage? originalMessage,
        RnifFailureCode code,
        string description,
        string localPartnerId,
        string globalUsageCode = "Production")
    {
        var ns = RnifNamespaces.Rnif20Ns;

        var trackingId = originalMessage?.Delivery.MessageTrackingId ?? string.Empty;
        var usageCode = originalMessage?.Preamble.GlobalUsageCode ?? globalUsageCode;

        return new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(ns + "ExceptionSignal",
                new XAttribute(XNamespace.Xmlns + "rnif", RnifNamespaces.Rnif20),
                new XElement(ns + "GlobalUsageCode", usageCode),
                new XElement(ns + "inResponseToMessageTrackingID", trackingId),
                new XElement(ns + "ExceptionCode", code.ToString()),
                new XElement(ns + "ExceptionDescription", description),
                new XElement(ns + "PartnerRoute",
                    new XElement(ns + "GlobalBusinessIdentifier", localPartnerId))
            )
        );
    }
}

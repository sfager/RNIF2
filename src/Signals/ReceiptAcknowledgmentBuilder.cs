using System.Xml.Linq;
using RNIF2.Core.Constants;
using RNIF2.Core.Models;

namespace RNIF2.Signals;

public static class ReceiptAcknowledgmentBuilder
{
    public static XDocument Build(RnifMessage originalMessage, string localPartnerId, string localBusinessName)
    {
        var ns = RnifNamespaces.Rnif20Ns;

        return new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(ns + "ReceiptAcknowledgmentSignal",
                new XAttribute(XNamespace.Xmlns + "rnif", RnifNamespaces.Rnif20),
                new XElement(ns + "GlobalUsageCode", originalMessage.Preamble.GlobalUsageCode),
                new XElement(ns + "inResponseToMessageTrackingID",
                    originalMessage.Delivery.MessageTrackingId),
                new XElement(ns + "inResponseToGlobalBusinessActionCode",
                    originalMessage.Service.PipCode),
                new XElement(ns + "OriginalMessageDateTimeStamp",
                    originalMessage.Delivery.MessageDateTime.ToString("o")),
                new XElement(ns + "PartnerRoute",
                    new XElement(ns + "GlobalBusinessIdentifier", localPartnerId),
                    new XElement(ns + "BusinessName", localBusinessName))
            )
        );
    }
}

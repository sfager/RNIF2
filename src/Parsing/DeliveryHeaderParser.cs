using System.Xml.Linq;
using RNIF2.Core.Constants;
using RNIF2.Core.Exceptions;
using RNIF2.Core.Models;

namespace RNIF2.Parsing;

public static class DeliveryHeaderParser
{
    public static DeliveryHeader Parse(ParsedMimePart part)
    {
        XDocument xml;
        try
        {
            xml = XDocument.Parse(System.Text.Encoding.UTF8.GetString(part.Data));
        }
        catch (Exception ex)
        {
            throw new RnifParsingException("DeliveryHeader: invalid XML.", ex);
        }

        var ns = RnifNamespaces.Rnif20Ns;

        var trackingId = FindValue(xml.Root, ns, "MessageTrackingID")
            ?? throw new RnifParsingException("DeliveryHeader: missing <MessageTrackingID>.");

        var tsValue = FindValue(xml.Root, ns, "MessageDateTime") ?? DateTimeOffset.UtcNow.ToString("o");
        if (!DateTimeOffset.TryParse(tsValue, out var ts))
            ts = DateTimeOffset.UtcNow;

        var fromId = FindPartnerIdentifier(xml.Root, ns, "FromRole")
            ?? throw new RnifParsingException("DeliveryHeader: missing FromRole/GlobalBusinessIdentifier.");

        var toId = FindPartnerIdentifier(xml.Root, ns, "ToRole")
            ?? throw new RnifParsingException("DeliveryHeader: missing ToRole/GlobalBusinessIdentifier.");

        return new DeliveryHeader
        {
            MessageTrackingId = trackingId,
            MessageDateTime = ts,
            FromPartner = new TradingPartnerIdentity { GlobalBusinessIdentifier = fromId },
            ToPartner = new TradingPartnerIdentity { GlobalBusinessIdentifier = toId }
        };
    }

    private static string? FindValue(XElement? root, XNamespace ns, string localName) =>
        root?.Descendants(ns + localName).FirstOrDefault()?.Value
        ?? root?.Descendants(localName).FirstOrDefault()?.Value;

    private static string? FindPartnerIdentifier(XElement? root, XNamespace ns, string roleElement)
    {
        var role = root?.Element(ns + roleElement) ?? root?.Element(roleElement);
        return role?.Descendants(ns + "GlobalBusinessIdentifier").FirstOrDefault()?.Value
            ?? role?.Descendants("GlobalBusinessIdentifier").FirstOrDefault()?.Value;
    }
}
